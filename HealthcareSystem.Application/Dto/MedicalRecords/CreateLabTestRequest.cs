using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.LabTest
{
    public class CreateLabTestRequest
    {
        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        public Guid? MedicalRecordId { get; set; }

        [Required]
        public string TestName { get; set; } = string.Empty;

        public string? TestType { get; set; }
        public string? Notes { get; set; }
    }
}