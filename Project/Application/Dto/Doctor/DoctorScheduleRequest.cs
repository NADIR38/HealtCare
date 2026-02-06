// DoctorScheduleRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class DoctorScheduleRequest
    {
        [Required(ErrorMessage = "Day of week is required")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }

        [Range(5, 180, ErrorMessage = "Slot duration must be between 5 and 180 minutes")]
        public int SlotDurationMinutes { get; set; } = 30;
    }
}