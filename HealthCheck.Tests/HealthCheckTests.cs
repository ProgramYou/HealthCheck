using HealthCheck.Domain.Extensions;
using HealthCheck.Domain.Models;
using HealthCheck.Domain.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HealthCheck.Tests
{
    public class HealthCheckTests
    {
        /// <summary>
        /// Ensures the expected server state for a each day of the month based on date ranges.
        /// </summary>
        [Theory]
        [InlineData("testserver01", "1,16")]
        [InlineData("testserver03", "2,15")]
        [InlineData("testserver05", "5,10,17,22")]
        [InlineData("testserver06", "1,5,12,20")]
        public void TestDateRanges(string servername, string startDays)
        {
            var settings = new HealthSettings()
            {
                Servername = servername,
                StartDays = startDays
            };

            var serverChar = Utility.GetLastChar(settings.Servername);

            if (string.IsNullOrWhiteSpace(serverChar) && int.TryParse(serverChar, out _))
            {
                Assert.Fail("Server index is invalid.");
            }
            else
            {
                var healthService = new HealthService(settings);
                var statusWhenMatched = settings.ParityStatusWhenMatched == "0" ? BaseTypes.ServerStatus.Standby : BaseTypes.ServerStatus.Active;
                var dateRanges = healthService.GetDateRanges(healthService.GetStartDays());
                var isServerIndexOdd = Utility.IsOdd(int.Parse(serverChar));

                //Simulate each day of the month
                for (int dayOfMonth = 1; dayOfMonth <= 31; dayOfMonth++)
                {
                    var date = new DateTime(2023, 1, dayOfMonth);
                    var status = healthService.GetServerState(date, settings.Servername, healthService.GetStateOverride());

                    foreach (var range in dateRanges)
                    {
                        //Check the last range for overflow into the next month
                        var isLastRange = range.Index == dateRanges.Count - 1;
                        var hasOverflowOnLastRange = isLastRange && range.Start != 1;

                        //Check up to the end of the month if the date is in range
                        var rangeEnd = hasOverflowOnLastRange ? 31 : range.End;
                        var isInRange = dayOfMonth >= range.Start && dayOfMonth <= rangeEnd;

                        //Check if the date is in the overflow in the beginning of the month
                        if (hasOverflowOnLastRange)
                        {
                            if (!isInRange)
                                isInRange = dayOfMonth < dateRanges.First().Start;
                        }

                        if (isInRange)
                        {
                            var period = range.Index + 1;
                            var isPeriodOdd = Utility.IsOdd(period);

                            //Is the server index odd or even
                            var isMatch = isServerIndexOdd == isPeriodOdd;

                            //Is the status expected
                            var expectedStatus = isMatch
                                ? statusWhenMatched
                                : healthService.OppositeStatus(statusWhenMatched);

                            var result = status == expectedStatus;
                            Assert.True(result, $"Day {dayOfMonth} in range {range.Index + 1} is expected to be {expectedStatus}. ");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensures expected server state when run monthly.
        /// </summary>
        [Theory]
        [InlineData("testserver01", 5, 1)]
        [InlineData("testserver02", 4, 1)]
        [InlineData("testserver03", 2, 4)]
        [InlineData("testserver03", 5, 4)]
        public void TestMonth(string servername, int day, int startDay)
        {
            var settings = new HealthSettings()
            {
                Servername = servername,
                StartDays = startDay.ToString(),
            };

            var serverIndex = Utility.GetLastChar(settings.Servername);

            if (string.IsNullOrWhiteSpace(serverIndex))
            {
                Assert.Fail("Server index is empty.");
            }
            else
            {
                var healthService = new HealthService(settings);
                var isServerIndexOdd = Utility.IsOdd(int.Parse(serverIndex));
                var statusWhenMatched = settings.ParityStatusWhenMatched == "0" ? BaseTypes.ServerStatus.Standby : BaseTypes.ServerStatus.Active;

                for (var month = 1; month <= 12; month++)
                {
                    var date = new DateTime(2025, month, day);
                    var status = healthService.GetServerState(date, settings.Servername, healthService.GetStateOverride());
                    var isPeriodOdd = Utility.IsOdd(month);
                    var isMatch = isServerIndexOdd == isPeriodOdd;
                    var hasStarted = day >= startDay;

                    var expectedStatus = (isMatch && hasStarted) || (!isMatch && !hasStarted)
                        ? statusWhenMatched
                        : healthService.OppositeStatus(statusWhenMatched);

                    var result = status == expectedStatus;

                    Assert.True(result, $"Date {date:yyyy-MM-dd} in month {month} is expected to be {expectedStatus}. ");
                }
            }
        }

        [Fact]
        public void TestInvalidDays()
        {
            var date = new DateTime(2025, 5, 14);
            var settings = new HealthSettings()
            {
                Servername = "testserver01",
                StartDays = "1,12,16,29"
            };

            var healthService = new HealthService(settings);
            var status = healthService.GetServerState(date, settings.Servername, healthService.GetStateOverride());

            Assert.True(healthService.Errors.Count == 2, "There should be two errors. ");
            Assert.StartsWith("Invalid", healthService.Errors[16]);
            Assert.StartsWith("Ignored", healthService.Errors[29]);
        }

    }
}