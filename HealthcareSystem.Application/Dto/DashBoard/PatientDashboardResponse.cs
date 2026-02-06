using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Dashboard
{
    public class PatientDashboardResponse
    {
        // Upcoming Appointments
        public AppointmentSummary? NextAppointment { get; set; }
        public List<AppointmentSummary> UpcomingAppointments { get; set; } = new();

        // Recent Activities
        public List<RecentActivitySummary> RecentActivities { get; set; } = new();

        // Health Summary
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int TotalPrescriptions { get; set; }
        public int TotalLabTests { get; set; }

        // Pending Items
        public int PendingInvoices { get; set; }
        public decimal PendingAmount { get; set; }
        public int UnreadNotifications { get; set; }

        // Recent Documents
        public List<DocumentSummary> RecentDocuments { get; set; } = new();
    }

    public class AppointmentSummary
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class RecentActivitySummary
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class DocumentSummary
    {
        public Guid DocumentId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? DownloadUrl { get; set; }
    }
}