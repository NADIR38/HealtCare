using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Dashboard
{
    public class AdminDashboardResponse
    {
        // Overview Stats
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int ActiveDoctors { get; set; }
        public int TotalAppointments { get; set; }

        // Today's Stats
        public int TodayAppointments { get; set; }
        public int TodayCheckedIn { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayScheduled { get; set; }

        // Revenue Stats
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int PendingInvoices { get; set; }
        public int OverdueInvoices { get; set; }

        // Recent Activities
        public List<RecentActivityItem> RecentActivities { get; set; } = new();

        // Charts Data
        public List<AppointmentTrendData> AppointmentTrends { get; set; } = new();
        public List<RevenueTrendData> RevenueTrends { get; set; } = new();
        public List<TopDoctorData> TopDoctors { get; set; } = new();
        public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserName { get; set; }
    }

    public class AppointmentTrendData
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class RevenueTrendData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TopDoctorData
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public decimal Revenue { get; set; }
    }
}