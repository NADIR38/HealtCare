// DoctorLeaveRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class DoctorLeaveRequest
    {
        [Required(ErrorMessage = "Doctor ID is required")]
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string? Reason { get; set; }
    }
}