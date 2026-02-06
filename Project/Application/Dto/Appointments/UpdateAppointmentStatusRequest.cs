using HealthcareSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.Appointment
{
    public class UpdateAppointmentStatusRequest
    {
        [Required]
        public AppointmentStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}