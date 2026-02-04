using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Appointments
{
    public class UpdateAppointmentRequest
    {
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public AppointmentType? Type { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }
}
