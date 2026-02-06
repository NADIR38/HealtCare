using HealthcareSystem.Domain.Enums;
using System;

namespace HealthcareSystem.Application.DTOs.LabTest
{
    public class LabTestResponse
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;

        public string TestName { get; set; } = string.Empty;
        public string? TestType { get; set; }
        public LabTestStatus Status { get; set; }

        public DateTime OrderedDate { get; set; }
        public DateTime? SampleCollectedDate { get; set; }
        public DateTime? ResultDate { get; set; }

        public string? Results { get; set; }
        public string? ResultFileUrl { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}