using Hangfire;
using HealthcareSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class BackgroundJobsController : ControllerBase
    {
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<BackgroundJobsController> _logger;

        public BackgroundJobsController(
            IBackgroundJobService backgroundJobService,
            ILogger<BackgroundJobsController> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        /// <summary>
        /// Manually trigger appointment reminders
        /// </summary>
        [HttpPost("trigger-appointment-reminders")]
        public IActionResult TriggerAppointmentReminders()
        {
            _logger.LogInformation("Manually triggering appointment reminders");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.SendAppointmentRemindersAsync());

            return Ok(new { message = "Appointment reminders job queued successfully" });
        }

        /// <summary>
        /// Manually trigger overdue invoice reminders
        /// </summary>
        [HttpPost("trigger-overdue-invoice-reminders")]
        public IActionResult TriggerOverdueInvoiceReminders()
        {
            _logger.LogInformation("Manually triggering overdue invoice reminders");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.SendOverdueInvoiceRemindersAsync());

            return Ok(new { message = "Overdue invoice reminders job queued successfully" });
        }

        /// <summary>
        /// Manually trigger daily appointment summary
        /// </summary>
        [HttpPost("trigger-daily-summary")]
        public IActionResult TriggerDailyAppointmentSummary()
        {
            _logger.LogInformation("Manually triggering daily appointment summary");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.SendDailyAppointmentSummaryToDoctorsAsync());

            return Ok(new { message = "Daily appointment summary job queued successfully" });
        }

        /// <summary>
        /// Manually trigger overdue invoice status update
        /// </summary>
        [HttpPost("trigger-update-overdue-status")]
        public IActionResult TriggerUpdateOverdueStatus()
        {
            _logger.LogInformation("Manually triggering overdue invoice status update");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.UpdateOverdueInvoiceStatusAsync());

            return Ok(new { message = "Update overdue status job queued successfully" });
        }

        /// <summary>
        /// Manually trigger data cleanup
        /// </summary>
        [HttpPost("trigger-data-cleanup")]
        public IActionResult TriggerDataCleanup()
        {
            _logger.LogInformation("Manually triggering data cleanup");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.CleanupOldDataAsync());

            return Ok(new { message = "Data cleanup job queued successfully" });
        }

        /// <summary>
        /// Manually trigger monthly revenue report
        /// </summary>
        [HttpPost("trigger-revenue-report")]
        public IActionResult TriggerMonthlyRevenueReport()
        {
            _logger.LogInformation("Manually triggering monthly revenue report");

            BackgroundJob.Enqueue<IBackgroundJobService>(
                service => service.SendMonthlyRevenueReportAsync());

            return Ok(new { message = "Monthly revenue report job queued successfully" });
        }

        /// <summary>
        /// Schedule a specific appointment reminder
        /// </summary>
        [HttpPost("schedule-appointment-reminder/{appointmentId}")]
        public IActionResult ScheduleAppointmentReminder(Guid appointmentId, [FromQuery] DateTime reminderTime)
        {
            _logger.LogInformation("Scheduling reminder for appointment {AppointmentId} at {ReminderTime}",
                appointmentId, reminderTime);

            BackgroundJob.Schedule<IBackgroundJobService>(
                service => service.SendAppointmentReminderAsync(appointmentId),
                reminderTime);

            return Ok(new
            {
                message = "Appointment reminder scheduled successfully",
                appointmentId,
                scheduledFor = reminderTime
            });
        }

        /// <summary>
        /// Get job statistics
        /// </summary>
        [HttpGet("statistics")]
        public IActionResult GetJobStatistics()
        {
            var monitor = JobStorage.Current.GetMonitoringApi();

            // Aggregate enqueued jobs count across all queues
            var queues = monitor.Queues();
            long enqueuedJobs = 0;
            foreach (var queue in queues)
            {
                enqueuedJobs += monitor.EnqueuedCount(queue.Name);
            }

            // Recurring jobs count is not available via IMonitoringApi in Hangfire.Core.
            // You may need to use RecurringJobManager or RecurringJobStorage directly if needed.
            // For now, set recurringJobs to 0 or remove it from the response.
            var statistics = new
            {
                servers = monitor.Servers().Count,
                // recurringJobs = monitor.RecurringJobs(0, int.MaxValue).Count, // Removed due to missing method
                succeededJobs = monitor.SucceededListCount(),
                failedJobs = monitor.FailedCount(),
                processingJobs = monitor.ProcessingCount(),
                scheduledJobs = monitor.ScheduledCount(),
                enqueuedJobs = enqueuedJobs
            };

            return Ok(statistics);
        }
    }
}