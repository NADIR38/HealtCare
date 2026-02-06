using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.Appointment
{
    public class CancelAppointmentRequest
    {
        [Required]
        [MinLength(10, ErrorMessage = "Cancellation reason must be at least 10 characters")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}