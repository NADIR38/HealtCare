using HealthcareSystem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage message);
        Task SendLeaveApprovedEmailAsync(string doctorEmail, string doctorName, string startDate, string endDate);
        Task SendLeaveRejectedEmailAsync(string doctorEmail, string doctorName, string startDate, string endDate, string? reason = null);
        Task SendAppointmentConfirmationAsync(string patientEmail, string patientName, string doctorName, string appointmentDate, string appointmentTime);
        Task SendAppointmentReminderAsync(string patientEmail, string patientName, string doctorName, string appointmentDate, string appointmentTime);
        Task SendAppointmentCancellationAsync(string email, string name, string doctorName, string appointmentDate, string reason);
    }
}
