namespace HealthcareSystem.Application.DTOs.MedicalRecord
{
    public class VitalSignsRequest
    {
        public string? BloodPressureSystolic { get; set; }
        public string? BloodPressureDiastolic { get; set; }
        public decimal? Temperature { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public string? Notes { get; set; }
    }
}