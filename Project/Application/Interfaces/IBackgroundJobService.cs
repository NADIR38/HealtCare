using System;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Send appointment reminder emails to patients 24 hours before appointment
        /// </summary>
        Task SendAppointmentRemindersAsync();

        /// <summary>
        /// Send overdue invoice reminders to patients
        /// </summary>
        Task SendOverdueInvoiceRemindersAsync();

        /// <summary>
        /// Send daily appointment summary to doctors
        /// </summary>
        Task SendDailyAppointmentSummaryToDoctorsAsync();

        /// <summary>
        /// Cleanup old data (completed appointments older than 2 years, etc.)
        /// </summary>
        Task CleanupOldDataAsync();

        /// <summary>
        /// Update overdue invoice status
        /// </summary>
        Task UpdateOverdueInvoiceStatusAsync();

        /// <summary>
        /// Send a specific appointment reminder
        /// </summary>
        Task SendAppointmentReminderAsync(Guid appointmentId);

        /// <summary>
        /// Generate and email monthly revenue report to admin
        /// </summary>
        Task SendMonthlyRevenueReportAsync();
        Task CleanupOldNotificationsAsync();
    }
}