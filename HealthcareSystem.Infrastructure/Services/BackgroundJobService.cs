using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Application.Models;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly IConfiguration _configuration;

        public BackgroundJobService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<BackgroundJobService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }
        public async  Task CleanupOldDataAsync()
        {
            try
            {
                var lasttwoyears = DateTime.UtcNow.AddYears(-2);
                var oldAppointments = await _context.Appointments.Where(a => a.Status == AppointmentStatus.Completed && a.AppointmentDate < lasttwoyears).ToListAsync();
                _context.Appointments.RemoveRange(oldAppointments);
                var oldCancelledAppointments = await _context.Appointments.Where(a => a.Status == AppointmentStatus.Cancelled && a.AppointmentDate < lasttwoyears).ToListAsync();
                _context.Appointments.RemoveRange(oldCancelledAppointments);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} old appointments",
                  oldAppointments.Count + oldCancelledAppointments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CleanupOldDataAsync");
                throw;
            }

        }

        public async Task SendAppointmentReminderAsync(Guid appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                   .Include(a => a.Patient)
                       .ThenInclude(p => p.User)
                   .Include(a => a.Doctor)
                       .ThenInclude(d => d.User)
                   .FirstOrDefaultAsync(a => a.Id == appointmentId);
                if (appointment == null)
                {
                    _logger.LogWarning("Appointment {AppointmentId} not found for reminder", appointmentId);
                    return;
                }

                await _emailService.SendAppointmentReminderAsync(
                    appointment.Patient.User.Email,
                    $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                    $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                    appointment.AppointmentDate.ToString("MMMM dd, yyyy"),
                    $"{appointment.StartTime:hh\\:mm tt} - {appointment.EndTime:hh\\:mm tt}"
                );

                _logger.LogInformation("Reminder sent for appointment {AppointmentNumber}",
                    appointment.AppointmentNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for appointment {AppointmentId}", appointmentId);
                throw;
            }
        }
        

        public async Task SendAppointmentRemindersAsync()
        {
            try
            {
                var remainderHours = _configuration.GetValue<int>("BackgroundJobSettings:AppointmentReminderHoursBefore");
                var remainderTime = DateTime.UtcNow.AddHours(remainderHours);
                var appointments = await _context.Appointments.Include(a => a.Patient).ThenInclude(u => u.User).Include(d => d.Doctor).ThenInclude(u => u.User).Where(a => a.Status == AppointmentStatus.Scheduled && a.AppointmentDate.Date == remainderTime.Date && a.StartTime >= remainderTime.TimeOfDay && a.StartTime <= remainderTime.AddHours(1).TimeOfDay).ToListAsync();
                _logger.LogInformation("Found {Count} appointments to send reminders for", appointments.Count);
                foreach (var appointment in appointments)
                {
                    try
                    {
                        await _emailService.SendAppointmentReminderAsync(
                            appointment.Patient.User.Email,
                            $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                            $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                            appointment.AppointmentDate.ToString("MMMM dd, yyyy"),
                            $"{appointment.StartTime:hh\\:mm tt} - {appointment.EndTime:hh\\:mm tt}"
                        );

                        _logger.LogInformation("Reminder sent for appointment {AppointmentNumber}",
                            appointment.AppointmentNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentNumber}",
                            appointment.AppointmentNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendAppointmentRemindersAsync");
                throw;
            }

        }

        public async Task SendDailyAppointmentSummaryToDoctorsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var doctors = await _context.Doctor
                    .Include(d => d.User)
                    .Where(d => d.IsAvailableForBooking)
                    .ToListAsync();

                _logger.LogInformation("Sending daily appointment summary to {Count} doctors", doctors.Count);

                foreach (var doctor in doctors)
                {
                    try
                    {
                        var appointments = await _context.Appointments
                            .Include(a => a.Patient)
                                .ThenInclude(p => p.User)
                            .Where(a => a.DoctorId == doctor.Id &&
                                       a.AppointmentDate.Date == today &&
                                       a.Status != AppointmentStatus.Cancelled)
                            .OrderBy(a => a.StartTime)
                            .ToListAsync();

                        if (appointments.Count == 0)
                        {
                            _logger.LogInformation("No appointments for Dr. {DoctorName} today",
                                $"{doctor.User.FirstName} {doctor.User.LastName}");
                            continue;
                        }

                        var appointmentList = string.Join("", appointments.Select(a => $@"
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{a.StartTime:hh\\:mm tt}</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{a.Patient.User.FirstName} {a.Patient.User.LastName}</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{a.Patient.PatientNumber}</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{a.Type}</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{a.Status}</td>
                            </tr>
                        "));

                        var emailBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <h2 style='color: #3b82f6;'>Daily Appointment Summary</h2>
                                <p>Good morning Dr. {doctor.User.FirstName} {doctor.User.LastName},</p>
                                <p>You have <strong>{appointments.Count}</strong> appointment(s) scheduled for today, {today:MMMM dd, yyyy}.</p>
                                <table style='border-collapse: collapse; width: 100%; margin-top: 20px;'>
                                    <thead>
                                        <tr style='background-color: #3b82f6; color: white;'>
                                            <th style='padding: 10px; border: 1px solid #ddd;'>Time</th>
                                            <th style='padding: 10px; border: 1px solid #ddd;'>Patient Name</th>
                                            <th style='padding: 10px; border: 1px solid #ddd;'>Patient ID</th>
                                            <th style='padding: 10px; border: 1px solid #ddd;'>Type</th>
                                            <th style='padding: 10px; border: 1px solid #ddd;'>Status</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {appointmentList}
                                    </tbody>
                                </table>
                                <p style='margin-top: 20px;'>Have a great day!</p>
                                <p>Best regards,<br/>Healthcare System</p>
                            </body>
                            </html>
                        ";

                        await _emailService.SendEmailAsync(new EmailMessage
                        {
                            To = new System.Collections.Generic.List<string> { doctor.User.Email },
                            Subject = $"Daily Appointment Summary - {today:MMMM dd, yyyy}",
                            Body = emailBody,
                            IsHtml = true
                        });

                        _logger.LogInformation("Daily summary sent to Dr. {DoctorName}",
                            $"{doctor.User.FirstName} {doctor.User.LastName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send daily summary to Dr. {DoctorId}", doctor.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendDailyAppointmentSummaryToDoctorsAsync");
                throw;
            }
        }

        public async Task SendMonthlyRevenueReportAsync()
        {
            try
            {
                var lastMonth = DateTime.UtcNow.AddMonths(-1);
                var startOfMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var invoices = await _context.Invoices
                    .Include(i => i.Payments)
                    .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate <= endOfMonth)
                    .ToListAsync();

                var totalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);
                var pendingAmount = invoices.Where(i => i.Status == InvoiceStatus.Pending ||
                                                        i.Status == InvoiceStatus.PartiallyPaid)
                    .Sum(i => i.TotalAmount);

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2 style='color: #10b981;'>Monthly Revenue Report - {lastMonth:MMMM yyyy}</h2>
                        <div style='background-color: #f0fdf4; padding: 20px; border-left: 4px solid #10b981; margin: 20px 0;'>
                            <h3>Summary</h3>
                            <p><strong>Total Invoices:</strong> {invoices.Count}</p>
                            <p><strong>Total Revenue (Paid):</strong> ${totalRevenue:F2}</p>
                            <p><strong>Pending Amount:</strong> ${pendingAmount:F2}</p>
                            <p><strong>Paid Invoices:</strong> {invoices.Count(i => i.Status == InvoiceStatus.Paid)}</p>
                            <p><strong>Pending Invoices:</strong> {invoices.Count(i => i.Status == InvoiceStatus.Pending)}</p>
                            <p><strong>Overdue Invoices:</strong> {invoices.Count(i => i.Status == InvoiceStatus.Overdue)}</p>
                        </div>
                        <p>For detailed reports, please log in to the admin dashboard.</p>
                        <p>Best regards,<br/>Healthcare System</p>
                    </body>
                    </html>
                ";

                // Get admin emails
                var adminUsers = await _context.Users
                    .Where(u => u.UserRoles.Any(r => r.Role == Role.Admin))
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    await _emailService.SendEmailAsync(new EmailMessage
                    {
                        To = new System.Collections.Generic.List<string> { admin.Email },
                        Subject = $"Monthly Revenue Report - {lastMonth:MMMM yyyy}",
                        Body = emailBody,
                        IsHtml = true
                    });
                }

                _logger.LogInformation("Monthly revenue report sent to {Count} admins", adminUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMonthlyRevenueReportAsync");
                throw;
            }
        }

        public async Task SendOverdueInvoiceRemindersAsync()
        {
            try
            {
                var overdueInvoices=await _context.Invoices.Include(p=>p.Patient).ThenInclude(u=>u.User).Include(a=>a.Payments).Where(i => i.DueDate.HasValue &&i.DueDate.Value.Date < DateTime.UtcNow.Date &&i.Status != InvoiceStatus.Paid &&  i.Status != InvoiceStatus.Cancelled).ToListAsync();
                _logger.LogInformation("Found {Count} overdue invoices", overdueInvoices.Count);
                foreach(var invoice in overdueInvoices)
                {
                    try
                    {
                        var totalPaid = invoice.Payments
                                .Where(p => p.Status == PaymentStatus.Completed)
                                .Sum(p => p.Amount);
                        var balance = invoice.TotalAmount - totalPaid;
                        var emailBody = EmailBody(invoice, totalPaid, balance);
                        await _emailService.SendEmailAsync(new Application.Models.EmailMessage {
                            To = new System.Collections.Generic.List<string> { invoice.Patient.User.Email },
                            Subject = $"Overdue Invoice Reminder - {invoice.InvoiceNumber}",
                            Body = emailBody,
                            IsHtml = true
                        });

                        _logger.LogInformation("Overdue reminder sent for invoice {InvoiceNumber}",
                            invoice.InvoiceNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send overdue reminder for invoice {InvoiceNumber}",
                            invoice.InvoiceNumber);
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendOverdueInvoiceRemindersAsync");
                throw;
            }
        }
        public string EmailBody(Invoice invoice ,decimal totalPaid,decimal balance)
        {
            return $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <h2 style='color: #ef4444;'>Overdue Invoice Reminder</h2>
                                <p>Dear {invoice.Patient.User.FirstName} {invoice.Patient.User.LastName},</p>
                                <p>This is a reminder that your invoice is overdue.</p>
                                <div style='background-color: #fee2e2; padding: 15px; border-left: 4px solid #ef4444; margin: 20px 0;'>
                                    <p><strong>Invoice Number:</strong> {invoice.InvoiceNumber}</p>
                                    <p><strong>Invoice Date:</strong> {invoice.InvoiceDate:MMMM dd, yyyy}</p>
                                    <p><strong>Due Date:</strong> {invoice.DueDate.Value:MMMM dd, yyyy}</p>
                                    <p><strong>Total Amount:</strong> ${invoice.TotalAmount:F2}</p>
                                    <p><strong>Amount Paid:</strong> ${totalPaid:F2}</p>
                                    <p><strong>Balance Due:</strong> ${balance:F2}</p>
                                </div>
                                <p>Please arrange payment at your earliest convenience.</p>
                                <p>If you have already made payment, please disregard this notice.</p>
                                <p>Best regards,<br/>Healthcare System</p>
                            </body>
                            </html>
                        ";
        }
        public async Task UpdateOverdueInvoiceStatusAsync()
        {
            try
            {
                var invoicesToUpdate = await _context.Invoices
                    .Where(i => i.DueDate.HasValue &&
                               i.DueDate.Value.Date < DateTime.UtcNow.Date &&
                               i.Status == InvoiceStatus.Pending)
                    .ToListAsync();

                _logger.LogInformation("Updating {Count} invoices to overdue status", invoicesToUpdate.Count);

                foreach (var invoice in invoicesToUpdate)
                {
                    invoice.Status = InvoiceStatus.Overdue;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated {Count} invoices to overdue", invoicesToUpdate.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOverdueInvoiceStatusAsync");
                throw;
            }
        }
    }
}
