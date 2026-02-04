namespace HealthcareSystem.Application.Dto.MedicalRecords
{
    public class LabTestSummary
    {
        public Guid Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderedDate { get; set; }
    }
}