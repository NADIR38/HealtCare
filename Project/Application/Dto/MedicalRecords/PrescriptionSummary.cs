namespace HealthcareSystem.Application.Dto.MedicalRecords
{

    public class PrescriptionSummary
    {
        public Guid Id { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; }
        public int ItemCount { get; set; }
    }
}