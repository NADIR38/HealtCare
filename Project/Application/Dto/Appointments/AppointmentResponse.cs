using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Appointments
{
    public class AppointmentResponse
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;

        // Patient Info
        public Guid PatientId { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }

        // Doctor Info
        public Guid DoctorId { get; set; }
        public string DoctorNumber { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;

        // Appointment Details
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public AppointmentType Type { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }
}
