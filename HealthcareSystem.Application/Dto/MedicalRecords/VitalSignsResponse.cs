namespace HealthcareSystem.Application.Dto.MedicalRecords
{
    public class VitalSignsResponse
    {
        public Guid Id { get; set; }
        public string? BloodPressure { get; set; } // Combined "120/80"
        public decimal? Temperature { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? BMI { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}