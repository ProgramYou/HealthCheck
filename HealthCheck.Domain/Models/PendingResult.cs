namespace HealthCheck.Domain.Models
{
    public class PendingResult
    {
        public bool Value { get; set; }
        public bool HasError { get; set; }
        public string? Message { get; set; }
    }
}
