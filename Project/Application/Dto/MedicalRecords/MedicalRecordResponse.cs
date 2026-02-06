using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.MedicalRecords
{
    public class MedicalRecordResponse
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;

        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;

        public Guid? AppointmentId { get; set; }
        public string? AppointmentNumber { get; set; }

        public DateTime VisitDate { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? PhysicalExamination { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }

        public VitalSignsResponse? VitalSigns { get; set; }
        public List<PrescriptionSummary> Prescriptions { get; set; } = new List<PrescriptionSummary>();
        public List<LabTestSummary> LabTests { get; set; } = new List<LabTestSummary>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
