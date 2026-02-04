using HealthcareSystem.Domain.Enums;
using System;

namespace HealthcareSystem.Domain.Entities
{
    public class LabTest
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? MedicalRecordId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? TestType { get; set; }
        public DateTime OrderedDate { get; set; }
        public LabTestStatus Status { get; set; }
        public DateTime? SampleCollectedDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string? Results { get; set; } // JSON or text
        public string? ResultFileUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public MedicalRecord? MedicalRecord { get; set; }
    }
}