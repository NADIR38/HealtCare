using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.MedicalRecord
{
    public class CreateMedicalRecordRequest
    {
        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        public Guid? AppointmentId { get; set; }

        [Required]
        public DateTime VisitDate { get; set; }

        public string? ChiefComplaint { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? PhysicalExamination { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }

        // Vital Signs (optional during creation)
        public VitalSignsRequest? VitalSigns { get; set; }
    }
}