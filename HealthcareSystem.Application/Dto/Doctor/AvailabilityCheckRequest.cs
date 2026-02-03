// AvailabilityCheckRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class AvailabilityCheckRequest
    {
        [Required(ErrorMessage = "Doctor ID is required")]
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }
    }
}