using HealthCheck.Domain.Models;

namespace HealthCheck.Web.Services
{
    public class LocalService()
    {
        public HealthSettings MapSettings(IConfigurationManager configuration)
        {
            var settings = new HealthSettings
            {
                // Set your properties here
                Date = configuration.GetSection("Switch")?["Date"]?.ToLower() ?? "",
                Version = configuration.GetValue<string>("Version")?.ToString() ?? "",
                HealthCheckUrl = configuration.GetSection("Switch")?["HealthCheckUrl"]?.ToLower().TrimEnd('/') ?? "",
                StartDays = configuration.GetSection("Switch")?["StartDays"] ?? "",
                IsActive = configuration.GetSection("Switch")?["IsActive"]?.ToLower() ?? "",
                Servername = configuration.GetSection("Switch")?["Servername"]?.ToLower() ?? "",
                IsComplete = configuration.GetSection("Switch")?["IsComplete"]?.ToLower() ?? "",
                ParityStatusWhenMatched = configuration.GetSection("Switch")?["ParityStatusWhenMatched"] ?? "0"
            };

            return settings;
        }
    }
}
