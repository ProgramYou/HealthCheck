using HealthCheck.Domain.Models;
using System.Globalization;

namespace HealthCheck.Domain.Extensions
{
    public class Utility
    {
        public static string StatusText(BaseTypes.ServerStatus status)
        {
            switch (status)
            {
                case BaseTypes.ServerStatus.Standby:
                    return "On Standby";
                default:
                    return status.ToString();
            }
        }

        public static BaseTypes.ServerStatus GetEffectiveStatus(bool isServerMatch, BaseTypes.ServerStatus statusWhenMatched)
        {
            if (isServerMatch)
                return statusWhenMatched;
            else
            {
                //Show opposite parity for other period
                if (statusWhenMatched == BaseTypes.ServerStatus.Active)
                    return @BaseTypes.ServerStatus.Standby;
                else
                    return @BaseTypes.ServerStatus.Active;
            }
        }

        public static bool IsOdd(int number)
        {
            return number % 2 == 1;
        }

        public static string GetMonthName(int month)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
        }

        public static int GetLastDayOfMonth(DateTime date)
        {
            int result = DateTime.DaysInMonth(date.Year, date.Month);
            return result;
        }

        public static bool IsBetweenDays(DateTime date, int startDay, int endDay)
        {
            bool result = date.Day >= startDay && date.Day < endDay;
            return result;
        }

        public static string GetServername()
        {
            string result = Environment.MachineName.ToLower();
            return result;
        }

        public static string GetLastChar(string text)
        {
            return text[^1..];
        }

        public static async Task<string> GetHttpString(string url)
        {
            string result = "";
            var httpclient = new HttpClient();

            var t = "";
            try
            {
                using (var httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                    using var client = new HttpClient(httpClientHandler);

                    t = $"respond. Please check service. url: {url}";
                    HttpResponseMessage response;
                    try
                    {
                        response = await httpclient.GetAsync(url);

                        t = "ensure result";
                        response.EnsureSuccessStatusCode();

                        t = "read response";
                        result = (await response.Content.ReadAsStringAsync()).Trim('"');
                    }
                    catch (Exception ex)
                    {
                        result = $"0:{ex.Message} {ex.InnerException?.Message} Url: {url}";
                    }
                }
            }
            catch (Exception)
            {
                result = $"0:failed to {t}. Url: {url}";
            }

            return result;
        }
    }
}
