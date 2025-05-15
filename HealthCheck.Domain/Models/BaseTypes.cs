namespace HealthCheck.Domain.Models
{
    public static class BaseTypes
    {
        /// <summary>
        /// Represents the possible statuses that a server can have.
        /// </summary>
        public enum ServerStatus
        {
            Standby = 0,
            Active = 1,
            Error = 2,
            Automatic = 3,
            Invalid = 4
        }

        /// <summary>
        /// The valid intervals that describe periods that servers can switch at.
        /// </summary>
        public enum Interval
        {
            DateRange = 1,
            Monthly = 2
        }
    }
}
