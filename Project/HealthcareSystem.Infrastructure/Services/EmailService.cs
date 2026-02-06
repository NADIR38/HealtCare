using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Application.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                ?? throw new InvalidOperationException("Email settings not configured");
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                emailMessage.To.AddRange(message.To.Select(email => new MailboxAddress("", email)));

                if (message.Cc != null)
                    emailMessage.Cc.AddRange(message.Cc.Select(email => new MailboxAddress("", email)));

                if (message.Bcc != null)
                    emailMessage.Bcc.AddRange(message.Bcc.Select(email => new MailboxAddress("", email)));

                emailMessage.Subject = message.Subject;

                var bodyBuilder = new BodyBuilder();
                if (message.IsHtml)
                {
                    bodyBuilder.HtmlBody = message.Body;
                }
                else
                {
                    bodyBuilder.TextBody = message.Body;
                }

                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort,
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {string.Join(", ", message.To)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                throw;
            }
        }
        public async Task SendEmailWithAttachmentAsync(EmailMessage message, byte[] attachment, string fileName)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                emailMessage.To.AddRange(message.To.Select(email => new MailboxAddress("", email)));
                emailMessage.Subject = message.Subject;

                var bodyBuilder = new BodyBuilder();
                if (message.IsHtml)
                {
                    bodyBuilder.HtmlBody = message.Body;
                }
                else
                {
                    bodyBuilder.TextBody = message.Body;
                }

                // Add attachment
                bodyBuilder.Attachments.Add(fileName, attachment, new ContentType("application", "pdf"));

                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort,
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email with attachment sent successfully to {string.Join(", ", message.To)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email with attachment: {ex.Message}");
                throw;
            }
        }
        public async Task SendLeaveApprovedEmailAsync(string doctorEmail, string doctorName, string startDate, string endDate)
        {
            var subject = "Leave Request Approved ✅";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #10b981; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .details {{ background-color: white; padding: 15px; border-left: 4px solid #10b981; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Leave Request Approved</h1>
                        </div>
                        <div class='content'>
                            <p>Dear Dr. {doctorName},</p>
                            <p>We are pleased to inform you that your leave request has been <strong>approved</strong>.</p>
                            
                            <div class='details'>
                                <h3>Leave Details:</h3>
                                <p><strong>From:</strong> {startDate}</p>
                                <p><strong>To:</strong> {endDate}</p>
                            </div>
                            
                            <p>Please ensure all your appointments are rescheduled or covered during this period.</p>
                            <p>Have a great time off!</p>
                            
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var message = new EmailMessage
            {
                To = new System.Collections.Generic.List<string> { doctorEmail },
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(message);
        }

        public async Task SendLeaveRejectedEmailAsync(string doctorEmail, string doctorName, string startDate, string endDate, string? reason = null)
        {
            var subject = "Leave Request Not Approved ❌";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .details {{ background-color: white; padding: 15px; border-left: 4px solid #ef4444; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Leave Request Status</h1>
                        </div>
                        <div class='content'>
                            <p>Dear Dr. {doctorName},</p>
                            <p>We regret to inform you that your leave request has been <strong>rejected</strong>.</p>
                            
                            <div class='details'>
                                <h3>Leave Details:</h3>
                                <p><strong>From:</strong> {startDate}</p>
                                <p><strong>To:</strong> {endDate}</p>
                                {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                            </div>
                            
                            <p>If you have any questions or would like to discuss this further, please contact the administration.</p>
                            
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var message = new EmailMessage
            {
                To = new System.Collections.Generic.List<string> { doctorEmail },
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(message);
        }

        public async Task SendAppointmentConfirmationAsync(string patientEmail, string patientName, string doctorName, string appointmentDate, string appointmentTime)
        {
            var subject = "Appointment Confirmation 📅";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #3b82f6; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .details {{ background-color: white; padding: 15px; border-left: 4px solid #3b82f6; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Appointment Confirmed</h1>
                        </div>
                        <div class='content'>
                            <p>Dear {patientName},</p>
                            <p>Your appointment has been successfully scheduled.</p>
                            
                            <div class='details'>
                                <h3>Appointment Details:</h3>
                                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                                <p><strong>Date:</strong> {appointmentDate}</p>
                                <p><strong>Time:</strong> {appointmentTime}</p>
                            </div>
                            
                            <p>Please arrive 10 minutes early for check-in.</p>
                            <p>If you need to cancel or reschedule, please contact us at least 24 hours in advance.</p>
                            
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var message = new EmailMessage
            {
                To = new System.Collections.Generic.List<string> { patientEmail },
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(message);
        }

        public async Task SendAppointmentReminderAsync(string patientEmail, string patientName, string doctorName, string appointmentDate, string appointmentTime)
        {
            var subject = "Appointment Reminder ⏰";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f59e0b; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .details {{ background-color: white; padding: 15px; border-left: 4px solid #f59e0b; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Appointment Reminder</h1>
                        </div>
                        <div class='content'>
                            <p>Dear {patientName},</p>
                            <p>This is a friendly reminder about your upcoming appointment.</p>
                            
                            <div class='details'>
                                <h3>Appointment Details:</h3>
                                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                                <p><strong>Date:</strong> {appointmentDate}</p>
                                <p><strong>Time:</strong> {appointmentTime}</p>
                            </div>
                            
                            <p>Please arrive 10 minutes early. If you need to cancel, please notify us as soon as possible.</p>
                            
                            <p>We look forward to seeing you!</p>
                            
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var message = new EmailMessage
            {
                To = new System.Collections.Generic.List<string> { patientEmail },
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(message);
        }

        public async Task SendAppointmentCancellationAsync(string email, string name, string doctorName, string appointmentDate, string reason)
        {
            var subject = "Appointment Cancelled ❌";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .details {{ background-color: white; padding: 15px; border-left: 4px solid #ef4444; margin: 15px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Appointment Cancelled</h1>
                        </div>
                        <div class='content'>
                            <p>Dear {name},</p>
                            <p>Your appointment has been cancelled.</p>
                            
                            <div class='details'>
                                <h3>Cancelled Appointment:</h3>
                                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                                <p><strong>Date:</strong> {appointmentDate}</p>
                                <p><strong>Reason:</strong> {reason}</p>
                            </div>
                            
                            <p>If you would like to reschedule, please contact us or book a new appointment through the system.</p>
                            
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var message = new EmailMessage
            {
                To = new System.Collections.Generic.List<string> { email },
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(message);
        }
    }
}