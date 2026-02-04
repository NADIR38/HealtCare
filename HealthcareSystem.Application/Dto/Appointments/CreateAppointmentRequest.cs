using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Appointments
{
    public class CreateAppointmentRequest
    {
        [Required(ErrorMessage ="Patient Id is required")]
        public Guid PatientId { get; set; }
        [Required( ErrorMessage ="Doctor Id is required")]
        public Guid DoctorId { get;  set; }
        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        public AppointmentType Type { get; set; } = AppointmentType.InPerson;

        public string? Reason { get; set; }

        public string? Notes { get; set; }

    }
}
