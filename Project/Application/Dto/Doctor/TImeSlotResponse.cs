using System;

namespace HealthcareSystem.Application.DTOs.Doctor
{
    public class TimeSlotResponse
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}