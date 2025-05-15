namespace HealthCheck.Domain.Models
{
    public class HealthSettings
    {
        public HealthSettings() { }

        /// <summary>
        /// The program version.
        /// </summary>
        public string Version { get; set; } = "1.0";
        /// <summary>
        /// Forces the server to be active or inactive
        /// </summary>
        public string IsActive { get; set; } = "";
        /// <summary>
        /// Determines whether the server is active or inactive when the month number and server number are the same. 
        /// </summary>
        public string ParityStatusWhenMatched { get; set; } = "0";
        /// <summary>
        /// The days of the month that the server will change state
        /// </summary>
        public string StartDays { get; set; } = "1";
        /// <summary>
        /// The fully qualified domain name of the healthcheck endpoint. 
        /// </summary>
        public string HealthCheckUrl { get; set; } = "";
        /// <summary>
        /// Simulates a successfully activated server on the firewall.
        /// Ends the pending state of the server.
        /// </summary>
        public string IsComplete { get; set; } = "";

        /// <summary>
        /// Overrides the name of the local server. 
        /// </summary>
        public string Servername { get; set; } = "";

        /// <summary>
        /// Overrides the date of the local server. 
        /// Format: yyyy-mm-dd
        /// </summary>
        public string Date { get; set; } = "";
    }
}
