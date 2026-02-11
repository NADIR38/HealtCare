using HealthcareSystem.Domain.Enums;

namespace HealthcareSystem.Application.Dto.Patient
{
    public class UpdatePatientRequest
    {
        public string? BloodGroup { get; set; } // Change from enum to string
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }

        // Medical History fields (separate table)
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
    }
}