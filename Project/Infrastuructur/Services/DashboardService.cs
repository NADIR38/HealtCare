using HealthcareSystem.Application.DTOs.Dashboard;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminDashboardResponse> GetAdminDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Overview Stats
            var totalPatients = await _context.Patients.CountAsync();
            var totalDoctors = await _context.Doctor.CountAsync();
            var activeDoctors = await _context.Doctor.CountAsync(d => d.IsAvailableForBooking);
            var totalAppointments = await _context.Appointments.CountAsync();
            
            // Today's Appointments
            var todayAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today)
                .CountAsync();

            var todayCheckedIn = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today && a.Status == AppointmentStatus.CheckedIn);

            var todayCompleted = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today && a.Status == AppointmentStatus.Completed);

            var todayScheduled = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today && a.Status == AppointmentStatus.Scheduled);

            // Revenue Stats
            var allInvoices = await _context.Invoices
                .Include(i => i.Payments)
                .ToListAsync();

            var totalRevenue = allInvoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            var monthlyRevenue = allInvoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.InvoiceDate >= startOfMonth)
                .Sum(i => i.TotalAmount);

            var pendingAmount = allInvoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid)
                .Sum(i => i.TotalAmount);

            var overdueAmount = allInvoices
                .Where(i => i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.TotalAmount);

            var totalInvoices = allInvoices.Count;
            var paidInvoices = allInvoices.Count(i => i.Status == InvoiceStatus.Paid);
            var pendingInvoices = allInvoices.Count(i => i.Status == InvoiceStatus.Pending);
            var overdueInvoices = allInvoices.Count(i => i.Status == InvoiceStatus.Overdue);

            // Recent Activities (last 10)
            // ISAY BADAL DEN:
            var recentAppointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .OrderByDescending(a => a.CreatedAt).Take(10)
                .ToListAsync(); // Database se data fetch ho gaya

            // Alag se formatting karen:
            var activities = recentAppointments.Select(a => new RecentActivityItem
            {
                Description = $"New appointment booked with {a.Patient.User.FirstName} {a.Patient.User.LastName}",
                UserName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                // baqi fields...
            }).ToList();

            // Appointment Trends (last 7 days)
            var appointmentTrends = new List<AppointmentTrendData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayAppointments = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == date)
                    .ToListAsync();

                appointmentTrends.Add(new AppointmentTrendData
                {
                    Date = date.ToString("MMM dd"),
                    Count = dayAppointments.Count,
                    Completed = dayAppointments.Count(a => a.Status == AppointmentStatus.Completed),
                    Cancelled = dayAppointments.Count(a => a.Status == AppointmentStatus.Cancelled)
                });
            }

            // Revenue Trends (last 6 months)
            var revenueTrends = new List<RevenueTrendData>();
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = startOfMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthRevenue = allInvoices
                    .Where(inv => inv.Status == InvoiceStatus.Paid &&
                                  inv.InvoiceDate >= monthStart &&
                                  inv.InvoiceDate < monthEnd)
                    .Sum(inv => inv.TotalAmount);

                revenueTrends.Add(new RevenueTrendData
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Amount = monthRevenue
                });
            }

            // Top Doctors (by appointment count this month)
            // ISAY BADAL DEN:
            var topDoctorsDataRaw = await _context.Appointments
                          .Where(a => a.AppointmentDate >= startOfMonth)
                          .GroupBy(a => new
                          {
                              a.DoctorId,
                              a.Doctor.User.FirstName,
                              a.Doctor.User.LastName,
                              a.Doctor.Specialization
                          })
                          .Select(g => new
                          {
                              g.Key.DoctorId,
                              Name = "Dr. " + g.Key.FirstName + " " + g.Key.LastName,
                              g.Key.Specialization,
                              Count = g.Count()
                          })
                          .OrderByDescending(x => x.Count).Take(5)
                          .ToListAsync();

            var topDoctorsData = topDoctorsDataRaw
                .Select(x => new TopDoctorData
                {
                    DoctorId = x.DoctorId,
                    DoctorName = x.Name,
                    Specialization = x.Specialization,
                    AppointmentCount = x.Count,
                    Revenue = 0 // If you want to add revenue, calculate it here
                })
                .ToList();

            // Appointments by Status
            var appointmentsByStatus = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Revenue by Payment Method (this month)
            var revenueByPaymentMethod = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= startOfMonth)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key.ToString(), Amount = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.Method, x => x.Amount);

            return new AdminDashboardResponse
            {
                TotalPatients = totalPatients,
                TotalDoctors = totalDoctors,
                ActiveDoctors = activeDoctors,
                TotalAppointments = totalAppointments,
                TodayAppointments = todayAppointments,
                TodayCheckedIn = todayCheckedIn,
                TodayCompleted = todayCompleted,
                TodayScheduled = todayScheduled,
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                PendingAmount = pendingAmount,
                OverdueAmount = overdueAmount,
                TotalInvoices = totalInvoices,
                PaidInvoices = paidInvoices,
                PendingInvoices = pendingInvoices,
                OverdueInvoices = overdueInvoices,
                RecentActivities = activities,
                AppointmentTrends = appointmentTrends,
                RevenueTrends = revenueTrends,
                TopDoctors = topDoctorsData,
                AppointmentsByStatus = appointmentsByStatus,
                RevenueByPaymentMethod = revenueByPaymentMethod
            };
        }

        public async Task<DoctorDashboardResponse> GetDoctorDashboardAsync(Guid doctorId)
        {
            var doctor = await _context.Doctor
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", doctorId);
            }

            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Today's Stats
            var todayAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == today)
                .CountAsync();

            var todayCompleted = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                               a.AppointmentDate.Date == today &&
                               a.Status == AppointmentStatus.Completed);

            var todayScheduled = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                               a.AppointmentDate.Date == today &&
                               a.Status == AppointmentStatus.Scheduled);

            var todayCheckedIn = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                               a.AppointmentDate.Date == today &&
                               a.Status == AppointmentStatus.CheckedIn);

            // This Week
            var weekAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate >= startOfWeek);

            var weekCompleted = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                               a.AppointmentDate >= startOfWeek &&
                               a.Status == AppointmentStatus.Completed);

            // This Month
            var monthAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate >= startOfMonth);

            var monthCompleted = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                               a.AppointmentDate >= startOfMonth &&
                               a.Status == AppointmentStatus.Completed);

            // Patient Stats
            var totalPatients = await _context.Appointments
                .Where(a => a.DoctorId == doctorId)
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();

            var newPatientsThisMonth = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= startOfMonth)
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();

            // Today's Schedule
            var todaysSchedule = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == today)
                .OrderBy(a => a.StartTime)
                .Select(a => new TodayAppointmentItem
                {
                    AppointmentId = a.Id,
                    AppointmentNumber = a.AppointmentNumber,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    PatientNumber = a.Patient.PatientNumber,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = a.Status.ToString(),
                    Type = a.Type.ToString(),
                    Reason = a.Reason
                })
                .ToListAsync();

            // Upcoming Appointments (next 7 days)
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate > today &&
                           a.AppointmentDate <= today.AddDays(7) &&
                           a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Select(a => new UpcomingAppointmentItem
                {
                    AppointmentId = a.Id,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    Type = a.Type.ToString()
                })
                .Take(5)
                .ToListAsync();

            // Recent Patients
            var recentPatients = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.MedicalRecords)
                .Where(a => a.DoctorId == doctorId && a.Status == AppointmentStatus.Completed)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(10)
                .Select(a => new
                {
                    a.Patient.Id,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    a.Patient.PatientNumber,
                    a.AppointmentDate,
                    LastDiagnosis = a.Patient.MedicalRecords
                        .OrderByDescending(m => m.VisitDate)
                        .Select(m => m.Diagnosis)
                        .FirstOrDefault()
                })
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToListAsync();

            var recentPatientsResult = recentPatients.Select(p => new RecentPatientItem
            {
                PatientId = p.Id,
                PatientName = p.PatientName,
                PatientNumber = p.PatientNumber,
                LastVisit = p.AppointmentDate,
                LastDiagnosis = p.LastDiagnosis
            }).ToList();

            // Pending Tasks
            var pendingLeaveRequests = await _context.DoctorLeave
                .CountAsync(l => l.DoctorId == doctorId && l.Status == LeaveStatus.Pending);

            var pendingLabTests = await _context.LabTests
                .CountAsync(l => l.DoctorId == doctorId &&
                               (l.Status == LabTestStatus.Ordered || l.Status == LabTestStatus.InProgress));

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == doctor.UserId && !n.IsRead);

            return new DoctorDashboardResponse
            {
                TodayAppointments = todayAppointments,
                TodayCompleted = todayCompleted,
                TodayScheduled = todayScheduled,
                TodayCheckedIn = todayCheckedIn,
                WeekAppointments = weekAppointments,
                WeekCompleted = weekCompleted,
                MonthAppointments = monthAppointments,
                MonthCompleted = monthCompleted,
                TotalPatients = totalPatients,
                NewPatientsThisMonth = newPatientsThisMonth,
                TodaysSchedule = todaysSchedule,
                UpcomingAppointments = upcomingAppointments,
                RecentPatients = recentPatientsResult,
                PendingLeaveRequests = pendingLeaveRequests,
                PendingLabTests = pendingLabTests,
                UnreadNotifications = unreadNotifications
            };
        }

        public async Task<PatientDashboardResponse> GetPatientDashboardAsync(Guid patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                throw new NotFoundException("Patient", patientId);
            }

            var today = DateTime.UtcNow.Date;

            // Next Appointment
            var nextAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => a.PatientId == patientId &&
                           a.AppointmentDate >= today &&
                           a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Select(a => new AppointmentSummary
                {
                    AppointmentId = a.Id,
                    AppointmentNumber = a.AppointmentNumber,
                    DoctorName = $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    Specialization = a.Doctor.Specialization,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    Status = a.Status.ToString(),
                    Type = a.Type.ToString()
                })
                .FirstOrDefaultAsync();

            // Upcoming Appointments (next 5)
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => a.PatientId == patientId &&
                           a.AppointmentDate >= today &&
                           a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Take(5)
                .Select(a => new AppointmentSummary
                {
                    AppointmentId = a.Id,
                    AppointmentNumber = a.AppointmentNumber,
                    DoctorName = $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    Specialization = a.Doctor.Specialization,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.StartTime,
                    Status = a.Status.ToString(),
                    Type = a.Type.ToString()
                })
                .ToListAsync();

            // Health Summary
            var totalAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patientId);

            var completedAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patientId && a.Status == AppointmentStatus.Completed);

            var totalPrescriptions = await _context.Prescriptions
                .CountAsync(p => p.PatientId == patientId);

            var totalLabTests = await _context.LabTests
                .CountAsync(l => l.PatientId == patientId);

            // Pending Items
            var pendingInvoices = await _context.Invoices
                .CountAsync(i => i.PatientId == patientId &&
                               (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue));

            var pendingAmount = await _context.Invoices
                .Where(i => i.PatientId == patientId &&
                           (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue))
                .SumAsync(i => i.TotalAmount);

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == patient.UserId && !n.IsRead);

            // Recent Activities
            var recentActivities = new List<RecentActivitySummary>();

            var recentAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            foreach (var appt in recentAppointments)
            {
                recentActivities.Add(new RecentActivitySummary
                {
                    Type = "Appointment",
                    Title = "Appointment Booked",
                    Description = $"With Dr. {appt.Doctor.User.FirstName} {appt.Doctor.User.LastName}",
                    Date = appt.CreatedAt
                });
            }

            var recentPrescriptions = await _context.Prescriptions
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.PrescriptionDate)
                .Take(2)
                .ToListAsync();

            foreach (var rx in recentPrescriptions)
            {
                recentActivities.Add(new RecentActivitySummary
                {
                    Type = "Prescription",
                    Title = "Prescription Issued",
                    Description = $"By Dr. {rx.Doctor.User.FirstName} {rx.Doctor.User.LastName}",
                    Date = rx.PrescriptionDate
                });
            }

            recentActivities = recentActivities.OrderByDescending(a => a.Date).Take(5).ToList();

            // Recent Documents
            var recentDocuments = new List<DocumentSummary>();

            var recentRx = await _context.Prescriptions
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.PrescriptionDate)
                .Take(3)
                .ToListAsync();

            foreach (var rx in recentRx)
            {
                recentDocuments.Add(new DocumentSummary
                {
                    DocumentId = rx.Id,
                    Type = "Prescription",
                    Title = $"Prescription {rx.PrescriptionNumber}",
                    Date = rx.PrescriptionDate,
                    DownloadUrl = $"/api/prescriptions/{rx.Id}/pdf"
                });
            }

            var recentLabs = await _context.LabTests
                .Where(l => l.PatientId == patientId && l.Status == LabTestStatus.Completed)
                .OrderByDescending(l => l.ResultDate)
                .Take(3)
                .ToListAsync();

            foreach (var lab in recentLabs)
            {
                recentDocuments.Add(new DocumentSummary
                {
                    DocumentId = lab.Id,
                    Type = "Lab Report",
                    Title = lab.TestName,
                    Date = lab.ResultDate ?? lab.OrderedDate,
                    DownloadUrl = $"/api/labtests/{lab.Id}/report-pdf"
                });
            }

            recentDocuments = recentDocuments.OrderByDescending(d => d.Date).Take(5).ToList();

            return new PatientDashboardResponse
            {
                NextAppointment = nextAppointment,
                UpcomingAppointments = upcomingAppointments,
                RecentActivities = recentActivities,
                TotalAppointments = totalAppointments,
                CompletedAppointments = completedAppointments,
                TotalPrescriptions = totalPrescriptions,
                TotalLabTests = totalLabTests,
                PendingInvoices = pendingInvoices,
                PendingAmount = pendingAmount,
                UnreadNotifications = unreadNotifications,
                RecentDocuments = recentDocuments
            };
        }
    }
}