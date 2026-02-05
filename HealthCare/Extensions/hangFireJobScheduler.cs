using Hangfire;
using HealthcareSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HealthcareSystem.API.Extensions
{
    public static class HangfireJobScheduler
    {
        public static void ConfigureRecurringJobs(IConfiguration configuration)
        {
            var enableReminders = configuration.GetValue<bool>("BackgroundJobSettings:EnableAppointmentReminders", true);
            var enableInvoiceReminders = configuration.GetValue<bool>("BackgroundJobSettings:EnableOverdueInvoiceReminders", true);
            var dailyReportTime = configuration.GetValue<string>("BackgroundJobSettings:DailyReportTime", "08:00");

            if (enableReminders)
            {
                RecurringJob.AddOrUpdate<IBackgroundJobService>(
                    "send-appointment-reminders",
                    service => service.SendAppointmentRemindersAsync(),
                    Cron.Hourly);
            }

            if (enableInvoiceReminders)
            {
                RecurringJob.AddOrUpdate<IBackgroundJobService>(
                    "send-overdue-invoice-reminders",
                    service => service.SendOverdueInvoiceRemindersAsync(),
                    Cron.Daily(9));
            }

            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                "update-overdue-invoice-status",
                service => service.UpdateOverdueInvoiceStatusAsync(),
                Cron.Daily(0));

            var timeParts = dailyReportTime.Split(':');
            if (timeParts.Length == 2 &&
                int.TryParse(timeParts[0], out int hour) &&
                int.TryParse(timeParts[1], out int minute))
            {
                RecurringJob.AddOrUpdate<IBackgroundJobService>(
                    "send-daily-appointment-summary",
                    service => service.SendDailyAppointmentSummaryToDoctorsAsync(),
                    Cron.Daily(hour, minute));
            }

            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                "cleanup-old-data",
                service => service.CleanupOldDataAsync(),
                Cron.Monthly(1, 2));

            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                "send-monthly-revenue-report",
                service => service.SendMonthlyRevenueReportAsync(),
                Cron.Monthly(1, 8));
        }
    }
}