using HealthcareSystem.Application.Dto.Appointments;
using HealthcareSystem.Application.DTOs.Appointment;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IAppointmentService
    {
        // Appointment Management
        Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, Guid createdBy);
        Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId);
        Task<AppointmentResponse> GetAppointmentByNumberAsync(string appointmentNumber);
        Task<List<AppointmentResponse>> GetAllAppointmentsAsync(int page, int pageSize, AppointmentStatus? status, DateTime? date);
        Task<List<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientId, bool includeHistory);
        Task<List<AppointmentResponse>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime? date);
        Task<List<AppointmentResponse>> GetTodayAppointmentsAsync(Guid? doctorId);
        Task<AppointmentResponse> UpdateAppointmentAsync(Guid appointmentId, UpdateAppointmentRequest request);
        Task<AppointmentResponse> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate, TimeSpan newStartTime);
        Task<bool> CancelAppointmentAsync(Guid appointmentId, string cancellationReason);

        // Status Management
        Task<AppointmentResponse> UpdateStatusAsync(Guid appointmentId, AppointmentStatus status, string? notes);
        Task<AppointmentResponse> CheckInAsync(Guid appointmentId);
        Task<AppointmentResponse> StartConsultationAsync(Guid appointmentId);
        Task<AppointmentResponse> CompleteAppointmentAsync(Guid appointmentId);

        // Analytics
        Task<object> GetAppointmentStatisticsAsync(DateTime startDate, DateTime endDate);
    }
}