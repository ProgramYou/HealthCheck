using HealthCheck.Domain.Extensions;
using HealthCheck.Domain.Models;
using System.Text.RegularExpressions;

namespace HealthCheck.Domain.Services
{
    /// <summary>
    /// Constructs the service using given settings. 
    /// </summary>
    public class HealthService(HealthSettings settings)
    {
        /// <summary>
        /// These days are avoided because they do not always exist in every month
        /// </summary>
        private readonly int[] invalidDays = [29, 30, 31];
        private const int daysBeforeLastToUpdate = 3;

        private HealthSettings _settings = settings;

        /// <summary>
        /// A collection of indexed errors. 
        /// </summary>
        public Dictionary<int, string> Errors { get; set; } = [];

        /// <summary>
        /// Returns the version of this application
        /// </summary>
        public string Version
        {
            get
            {
                var result = _settings.Version;
                return result;
            }
        }

        /// <summary>
        /// Gets the last day of the month.
        /// </summary>
        public int GetLastDayOfMonth(DateTime date)
        {
            return DateTime.DaysInMonth(date.Year, date.Month);
        }

        /// <summary>
        /// Returns the number of the period based on the date. 
        /// </summary>
        public int GetCurrentPeriod(DateTime date)
        {
            int result;
            var interval = GetInterval();

            if (interval == BaseTypes.Interval.Monthly)
            {
                var isValid = int.TryParse(GetStartDaysSetting(), out int dayOfMonth);

                if (isValid)
                {
                    //Stay on previous month if day not reached yet
                    result = date.Day >= dayOfMonth ? date.Month : date.AddMonths(-1).Month;
                }
                else
                {
                    //Use month if day is invalid
                    result = date.Month;
                }
            }
            else
            {
                result = GetPeriodInMonth(date);
            }

            return result;
        }

        /// <summary>
        /// The name describing the period that servers will be switched. 
        /// </summary>
        public string IntervalName
        {
            get
            {
                var interval = GetInterval();
                return interval == BaseTypes.Interval.DateRange ? "Range" : "Month";
            }
        }

        /// <summary>
        /// Gets the load-balanced, healthcheck url, in order to check if theload-balancer has completed switching over. 
        /// </summary>
        public string GetHealthCheckUrl()
        {
            var result = _settings.HealthCheckUrl;
            return result;
        }

        /// <summary>
        /// Gets the value of the setting that specifies the days that the servers are scheduled to switch over. 
        /// </summary>
        public string GetStartDaysSetting()
        {
            var result = _settings.StartDays;
            return result;
        }

        public bool IsValidStartDay(int dayOfMonth)
        {
            return !invalidDays.Contains(dayOfMonth);
        }

        /// <summary>
        /// Parses the start days given in the settings file.
        /// </summary>
        public List<int> GetStartDays()
        {
            var result = new List<int>();

            var daysText = GetStartDaysSetting();

            if (string.IsNullOrWhiteSpace(daysText))
            {
                //Default
                result.Add(1);
            }
            else
            {
                var days = daysText.Split(',');

                bool isPreviousOdd = false;

                foreach (var day in days)
                {
                    var isFirst = day == days.First();
                    var isValid = int.TryParse(day, out int dayOfMonth);
                    var alreadyAdded = result.Contains(dayOfMonth);

                    if (isValid && IsValidStartDay(dayOfMonth) && !alreadyAdded)
                    {
                        var isOdd = Utility.IsOdd(dayOfMonth);

                        if (isFirst)
                            isPreviousOdd = Utility.IsOdd(dayOfMonth);

                        if (!isFirst)
                        {
                            //Previous day must be opposite to current
                            if (isValid && isPreviousOdd != isOdd)
                            {
                                result.Add(dayOfMonth);
                                isPreviousOdd = isOdd;
                            }
                            else
                            {
                                if (!Errors.ContainsKey(dayOfMonth))
                                    Errors.Add(dayOfMonth, $"Invalid day {dayOfMonth}. Day will be ignored because an {(isOdd ? "even" : "odd")} number was expected. ");
                            }
                        }
                        else
                        {
                            result.Add(dayOfMonth);
                        }
                    }
                    else
                    {
                        if (!Errors.ContainsKey(dayOfMonth))
                            Errors.Add(dayOfMonth, $"Ignored day(s): {dayOfMonth}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Describes the period that the load-balancer will perform the switch over of servers.
        /// </summary>
        public BaseTypes.Interval GetInterval()
        {
            BaseTypes.Interval result;
            try
            {
                var daysOfMonth = GetStartDays();
                result = daysOfMonth.Count == 1 ? BaseTypes.Interval.Monthly : BaseTypes.Interval.DateRange;

                return result;
            }
            catch (Exception)
            {
                throw new Exception($"Invalid interval value. ");
            }
        }

        /// <summary>
        /// Gets the status if overriden in settings.
        /// </summary>
        public BaseTypes.ServerStatus GetStateOverride()
        {
            BaseTypes.ServerStatus result;

            string isActive = _settings.IsActive;

            if (isActive == "")
                result = BaseTypes.ServerStatus.Automatic;
            else if (isActive == "1")
                result = BaseTypes.ServerStatus.Active;
            else if (isActive == "0")
                result = BaseTypes.ServerStatus.Standby;
            else
                result = BaseTypes.ServerStatus.Error;

            return result;
        }

        public string GetNameOverride()
        {
            string switchState = _settings.Servername;
            return switchState;
        }

        /// <summary>
        /// Gets the server name of the local server or the setting if overriden. 
        /// </summary>
        public string GetEffectiveServername()
        {
            string overrideName = GetNameOverride();
            var hasNameOverride = !string.IsNullOrWhiteSpace(overrideName);
            var servername = hasNameOverride ? overrideName : Utility.GetServername();

            return servername;
        }

        /// <summary>
        /// Determines the pending status of a given status. 
        /// </summary>
        public async Task<PendingResult> IsPending(string servername, BaseTypes.ServerStatus serverStatus, string loadBalancedUrl)
        {
            var result = new PendingResult();

            string isComplete = _settings.IsComplete;

            if (string.IsNullOrWhiteSpace(isComplete))
            {
                var liveServer = await Utility.GetHttpString($"{loadBalancedUrl}/getname");
                var hasError = liveServer.StartsWith("0:");

                if (hasError)
                {
                    result.Value = true;
                    result.Message = liveServer[2..];
                    result.HasError = true;
                }
                else
                {
                    result.Value = liveServer != servername;
                    result.Message = liveServer;
                }
            }
            else
            {
                result.Value = !(isComplete == "1");
                result.Message = $"Load-balance completion is overridden. ";
            }

            return result;
        }

        /// <summary>
        /// Determines the status of the server based on it's parity and the parity of the period. 
        /// </summary>
        public BaseTypes.ServerStatus GetServerState(DateTime date, string servername, BaseTypes.ServerStatus stateOverride)
        {
            BaseTypes.ServerStatus result;
            var serverLastChar = Utility.GetLastChar(servername);
            var hasIndex = int.TryParse(serverLastChar, out var serverNodeIndex);

            if (stateOverride == BaseTypes.ServerStatus.Automatic)
            {
                if (hasIndex)
                {
                    var isNodeOdd = Utility.IsOdd(serverNodeIndex);
                    var period = GetCurrentPeriod(date);
                    bool isActive = IsActive(period, isNodeOdd);

                    result = isActive ? BaseTypes.ServerStatus.Active : BaseTypes.ServerStatus.Standby;
                }
                else
                    result = BaseTypes.ServerStatus.Error;
            }
            else
            {
                result = stateOverride;
            }

            return result;
        }

        /// <summary>
        /// Determines whether a server is active or on standby, based on it's parity matched to the parity of the period. 
        /// </summary>
        public bool IsActive(int period, bool isNodeOdd)
        {
            var isPeriodOdd = Utility.IsOdd(period);
            var isMatch = isPeriodOdd == isNodeOdd || !isPeriodOdd == !isNodeOdd;
            var statusWhenMatched = GetParityStatusWhenMatched();
            return statusWhenMatched == BaseTypes.ServerStatus.Active ? isMatch : !isMatch;
        }

        /// <summary>
        /// Gets the name of the computer that hosts the local instance
        /// </summary>
        public static string GetServername(string baseUrl)
        {
            var re = new Regex("//([^/:\\.]+)");
            string result = re.Match(baseUrl).Groups[1].Value;
            return result;
        }

        public DateTime GetUpdateDate(DateTime first, DateTime last)
        {
            var result = last;

            for (var i = 0; i < daysBeforeLastToUpdate; i++)
            {
                if (result != first)
                    result = result.AddDays(-1);
            }

            return result;
        }

        /// <summary>
        /// This method is used to get the instance of the period in the month for switching over
        /// </summary>
        public int GetPeriodInMonth(DateTime date)
        {
            var rangesInMonth = GetStartDays();
            var rangeIndex = rangesInMonth.Count - 1;

            for (var i = 0; i < rangesInMonth.Count; i++)
            {
                var hasNext = rangesInMonth.Count - 1 > i;
                var startDay = rangesInMonth[i];
                var endDay = hasNext ? rangesInMonth[i + 1] : Utility.GetLastDayOfMonth(date);
                var isInPeriod = Utility.IsBetweenDays(date, startDay, endDay);

                if (isInPeriod)
                {
                    rangeIndex = i;
                    break;
                }
            }

            return rangeIndex + 1;
        }

        /// <summary>
        /// Determine whether the server must be active or on standby according to the parity of the period
        /// </summary>
        public BaseTypes.ServerStatus GetParityStatusWhenMatched()
        {
            var parityStatusText = _settings.ParityStatusWhenMatched;
            var isValid = int.TryParse(parityStatusText, out int parityStatus);

            var result = isValid ? Enum.Parse<BaseTypes.ServerStatus>(parityStatus.ToString()) : 0;
            return result;
        }

        /// <summary>
        /// Get start and end dates for a range
        /// </summary>
        public DateTime[] GetRangeDates(int number, int year, int month)
        {
            var startDaysInMonth = GetStartDays();

            //Find first range if monthly
            var rangeCount = startDaysInMonth.Count();
            if (rangeCount == 1)
            {
                month = number;
                number = 1;
            }

            var startDayInMonth = startDaysInMonth[number - 1];
            var dayIndex = startDaysInMonth.IndexOf(startDayInMonth);
            var firstDay = startDaysInMonth[0];

            var hasNextDay = dayIndex < startDaysInMonth.Count - 1;
            var nextDay = hasNextDay ? startDaysInMonth[dayIndex + 1] : 0;

            var endDay = nextDay == 0 ? firstDay : nextDay;
            var endMonth = month + (nextDay == 0 ? 1 : 0);

            if (endMonth == 13)
            {
                endMonth = 1;
                year++;
            }

            var startDate = new DateTime(year, month, startDayInMonth);
            var endDate = new DateTime(year, endMonth, endDay).AddDays(-1);

            DateTime[] result = [startDate, endDate];
            return result;
        }

        /// <summary>
        /// Finds the next day that the server status will change. 
        /// </summary>
        public int GetNextStartDay(DateTime date, BaseTypes.ServerStatus stateOverride)
        {
            var startDaysInMonth = GetStartDays();
            var period = GetCurrentPeriod(date);

            // If the state is overridden, return the first day of the next period
            if (stateOverride != BaseTypes.ServerStatus.Automatic)
            {
                var nextPeriod = period + 1;
                return startDaysInMonth[nextPeriod % startDaysInMonth.Count];
            }

            foreach (var day in startDaysInMonth)
            {
                // Find the next rotation day
                if (day > date.Day)
                {
                    return day;
                }
            }

            // If no future day is found in the month, return the first day of the next month
            return startDaysInMonth.First();
        }

        /// <summary>
        /// Get the range for a given start day
        /// </summary>
        public DateRange GetDateRange(DateTime currentDate, int startDay, BaseTypes.ServerStatus stateOverride)
        {
            var month = currentDate.Month;
            var result = new DateRange();

            //Value type can be cloned via instantiation
            var date = currentDate;

            //Find start date
            while (date.Day != startDay)
                date = date.AddDays(1);

            result.Start = date;
            var nextStartDay = GetNextStartDay(date, stateOverride);

            //Start at tomorrow, as not to match current period
            date = date.AddDays(1);

            //Find end date
            while (date.Day != nextStartDay)
                date = date.AddDays(1);

            result.End = date;

            return result;
        }

        /// <summary>
        /// Gets the date ranges based on the start days.
        /// </summary>
        public List<DayRange> GetDateRanges(List<int> startDays)
        {
            var dateRanges = new List<DayRange>();

            for (int i = 0; i < startDays.Count; i++)
            {
                var startDay = startDays[i];
                int endDay;
                var spansMonth = startDay != 1;
                var isLast = i == startDays.Count - 1;

                if (isLast)
                    //Up to the first day of the next month or the end of the month
                    endDay = spansMonth ? startDays[0] - 1 : 31;
                else
                    //Up to the next day
                    endDay = startDays[i + 1] - 1;

                dateRanges.Add(new DayRange(i, startDay, endDay));
            }

            return dateRanges;
        }

        /// <summary>
        /// Gets the opposite od either active or on standby
        /// </summary>
        /// </summary>
        public BaseTypes.ServerStatus OppositeStatus(BaseTypes.ServerStatus status)
        {
            return status == BaseTypes.ServerStatus.Active
                ? BaseTypes.ServerStatus.Standby
                : BaseTypes.ServerStatus.Active;
        }

        /// <summary>
        /// Gets todays date, without time, or overrides it from settings if specified. 
        /// </summary>
        public DateTime GetDate()
        {
            DateTime result;
            var hasOverride = !String.IsNullOrWhiteSpace(_settings.Date);
            if (hasOverride)
            {
                var isValid = DateTime.TryParse(_settings.Date, out result);
                if (!isValid)
                    throw new ArgumentException("Invalid date setting specified. ");
            }
            else
                result = DateTime.Now.Date;

            return result;
        }
    }

}