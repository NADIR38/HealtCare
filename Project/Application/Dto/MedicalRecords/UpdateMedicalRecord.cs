namespace HealthcareSystem.Application.DTOs.MedicalRecord
{
    public class UpdateMedicalRecordRequest
    {
        public string? ChiefComplaint { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? PhysicalExamination { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
    }
}