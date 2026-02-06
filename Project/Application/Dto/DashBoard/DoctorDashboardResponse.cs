using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Dashboard
{
    public class DoctorDashboardResponse
    {
        // Today's Overview
        public int TodayAppointments { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayScheduled { get; set; }
        public int TodayCheckedIn { get; set; }

        // This Week
        public int WeekAppointments { get; set; }
        public int WeekCompleted { get; set; }

        // This Month
        public int MonthAppointments { get; set; }
        public int MonthCompleted { get; set; }

        // Patient Stats
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }

        // Today's Schedule
        public List<TodayAppointmentItem> TodaysSchedule { get; set; } = new();

        // Upcoming Appointments
        public List<UpcomingAppointmentItem> UpcomingAppointments { get; set; } = new();

        // Recent Patients
        public List<RecentPatientItem> RecentPatients { get; set; } = new();

        // Pending Tasks
        public int PendingLeaveRequests { get; set; }
        public int PendingLabTests { get; set; }
        public int UnreadNotifications { get; set; }
    }

    public class TodayAppointmentItem
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class UpcomingAppointmentItem
    {
        public Guid AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class RecentPatientItem
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public DateTime LastVisit { get; set; }
        public string? LastDiagnosis { get; set; }
    }
}