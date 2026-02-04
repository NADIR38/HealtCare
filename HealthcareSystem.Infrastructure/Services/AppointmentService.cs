using HealthcareSystem.Application.Dto.Appointments;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AppointmentService> _logger;
        private readonly DatabaseHelpers _helper;


        public AppointmentService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AppointmentService> logger, DatabaseHelpers helper)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _helper = helper;
        }

        public async Task<bool> CancelAppointmentAsync(Guid appointmentId, string cancellationReason)
        {
            var appointment = await _context.Appointments
                           .Include(a => a.Patient)
                           .ThenInclude(p => p.User)
                           .Include(a => a.Doctor)
                           .ThenInclude(d => d.User)
                           .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new NotFoundException("Appointment", appointmentId);
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                throw new BusinessException("Cannot cancel completed appointment");
            }

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                throw new BusinessException("Appointment is already cancelled");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = cancellationReason;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} cancelled. Reason: {Reason}",
                appointmentId, cancellationReason);

            // Send cancellation email
            try
            {
                await _emailService.SendAppointmentCancellationAsync(
                    appointment.Patient.User.Email,
                    $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                    $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                    appointment.AppointmentDate.ToString("MMMM dd, yyyy"),
                    cancellationReason
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email");
            }

            return true;
        }

        public async Task<AppointmentResponse> CheckInAsync(Guid appointmentId)
        {
            return await UpdateStatusAsync(appointmentId, AppointmentStatus.CheckedIn, "Patient checked in");
        }

        public async Task<AppointmentResponse> CompleteAppointmentAsync(Guid appointmentId)
        {
            return await UpdateStatusAsync(appointmentId, AppointmentStatus.Completed, "Appointment completed");
        }

        public async Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, Guid createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                if (DateValidation(request.AppointmentDate.Date, DateTime.UtcNow.Date))
                {
                    throw new ValidationException("Cannot book appointments in the past");

                }
                var patient = await _helper.CheckPatientExist(request.PatientId);
                var doctor = await _helper.DoctorExistsAndAvailable(request.DoctorId);
                if (!doctor.IsAvailableForBooking)
                {
                    throw new BusinessException("Doctor is not available for booking");

                }
                if (await _helper.CheckDoctorOnLeave(request.DoctorId, request.AppointmentDate.Date))
                {
                    throw new BusinessException("Doctor is not available for booking");

                }

                var dayOfWeek = request.AppointmentDate.DayOfWeek;
                var schedule = doctor.Schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek && s.IsActive);
                if (schedule == null)
                {
                    throw new BusinessException($"Doctor does not have availability on {dayOfWeek}");
                }
                var endTime = request.StartTime.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes));

                if (request.StartTime < schedule.StartTime || endTime > schedule.EndTime)
                {
                    throw new ValidationException(
                        $"Selected time is outside doctor's schedule ({schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm})");
                }
                var hasConflict = await _context.Appointments
                  .AnyAsync(a => a.DoctorId == request.DoctorId &&
                                a.AppointmentDate.Date == request.AppointmentDate.Date &&
                                a.Status != AppointmentStatus.Cancelled &&
                                a.StartTime < endTime &&
                                a.EndTime > request.StartTime);

                if (hasConflict)
                {
                    throw new ConflictException("This time slot is already booked");
                }
                var appointmentNumber = await GenerateAppointmentNumberAsync();

                // Create appointment
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentNumber = appointmentNumber,
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    AppointmentDate = request.AppointmentDate.Date,
                    StartTime = request.StartTime,
                    EndTime = endTime,
                    Status = AppointmentStatus.Scheduled,
                    Type = request.Type,
                    Reason = request.Reason,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Appointment {AppointmentNumber} created for Patient {PatientId} with Doctor {DoctorId}",
                    appointmentNumber, request.PatientId, request.DoctorId);

                // Send confirmation email
                try
                {
                    await _emailService.SendAppointmentConfirmationAsync(
                        patient.User.Email,
                        $"{patient.User.FirstName} {patient.User.LastName}",
                        $"{doctor.User.FirstName} {doctor.User.LastName}",
                        request.AppointmentDate.ToString("MMMM dd, yyyy"),
                        $"{request.StartTime:hh\\:mm tt} - {endTime:hh\\:mm tt}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send appointment confirmation email");
                }


                return await GetAppointmentByIdAsync(appointment.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create appointment");
                throw;
            }
        }
        public async Task<List<AppointmentResponse>> GetAllAppointmentsAsync(
            int page,
            int pageSize,
            AppointmentStatus? status,
            DateTime? date)
        {
            if (page < 1) throw new ValidationException("Page number must be greater than 0");
            if (pageSize < 1 || pageSize > 100) throw new ValidationException("Page size must be between 1 and 100");

            var query = _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.CreatedByUser)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            if (date.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return appointments.Select(MapToAppointmentResponse).ToList();

        }

        public async Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                         .Include(a => a.Patient)
                         .ThenInclude(p => p.User)
                         .Include(a => a.Doctor)
                         .ThenInclude(d => d.User)
                         .Include(a => a.CreatedByUser)
                         .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new NotFoundException("Appointment", appointmentId);
            }

            return MapToAppointmentResponse(appointment);
        }

        public async Task<AppointmentResponse> GetAppointmentByNumberAsync(string appointmentNumber)
        {
            if (string.IsNullOrWhiteSpace(appointmentNumber))
            {
                throw new ValidationException("Appointment number is required");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.AppointmentNumber == appointmentNumber);

            if (appointment == null)
            {
                throw new NotFoundException($"Appointment with number '{appointmentNumber}' not found");
            }

            return MapToAppointmentResponse(appointment);
        }

        public async Task<object> GetAppointmentStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ValidationException("Start date must be before end date");
            }

            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate >= startDate.Date && a.AppointmentDate <= endDate.Date)
                .ToListAsync();

            return new
            {
                totalAppointments = appointments.Count,
                scheduled = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                checkedIn = appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
                inProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress),
                completed = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow),
                inPerson = appointments.Count(a => a.Type == AppointmentType.InPerson),
                telemedicine = appointments.Count(a => a.Type == AppointmentType.Telemedicine),
                completionRate = appointments.Count > 0
                    ? Math.Round((double)appointments.Count(a => a.Status == AppointmentStatus.Completed) / appointments.Count * 100, 2)
                    : 0
            };
        }


        public async Task<List<AppointmentResponse>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime? date)
        {
            var doctorExists = await _context.Doctor.AnyAsync(d => d.Id == doctorId);
            if (!doctorExists)
            {
                throw new NotFoundException("Doctor", doctorId);
            }

            var query = _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.CreatedByUser)
                .Where(a => a.DoctorId == doctorId);

            if (date.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }
            else
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(a => a.AppointmentDate >= today);
            }

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return appointments.Select(MapToAppointmentResponse).ToList();
        }

        public async  Task<List<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientId, bool includeHistory)
        {
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId);
            if (!patientExists)
            {
                throw new NotFoundException("Patient", patientId);
            }

            var query = _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.CreatedByUser)
                .Where(a => a.PatientId == patientId);

            if (!includeHistory)
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(a => a.AppointmentDate >= today &&
                                        (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.CheckedIn));
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return appointments.Select(MapToAppointmentResponse).ToList();
        }

        public async Task<List<AppointmentResponse>> GetTodayAppointmentsAsync(Guid? doctorId)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.CreatedByUser)
                .Where(a => a.AppointmentDate.Date == today &&
                           a.Status != AppointmentStatus.Cancelled);

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);
            }

            var appointments = await query
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            return appointments.Select(MapToAppointmentResponse).ToList();
        }

        public async Task<AppointmentResponse> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate, TimeSpan newStartTime)
        {
            var appointment = await _context.Appointments
                           .Include(a => a.Patient)
                           .ThenInclude(p => p.User)
                           .Include(a => a.Doctor)
                           .ThenInclude(d => d.User)
                           .Include(a => a.CreatedByUser)
                           .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new NotFoundException("Appointment", appointmentId);
            }

            if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
            {
                throw new BusinessException($"Cannot reschedule {appointment.Status.ToString().ToLower()} appointment");
            }

            if (newDate.Date < DateTime.UtcNow.Date)
            {
                throw new ValidationException("Cannot reschedule to a past date");
            }
            if(await _helper.CheckDoctorOnLeave(appointment.DoctorId, newDate))
            {
                throw new BusinessException($"Cannot reschedule doctor is on Leave");

            }

            // Get doctor's schedule for the new date
            var dayOfWeek = newDate.DayOfWeek;
            var schedule = await _context.DoctorSchedule
                .FirstOrDefaultAsync(s => s.DoctorId == appointment.DoctorId &&
                                         s.DayOfWeek == dayOfWeek &&
                                         s.IsActive);

            if (schedule == null)
            {
                throw new BusinessException($"Doctor does not have availability on {dayOfWeek}");
            }

            var newEndTime = newStartTime.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes));

            // Check for conflicts
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.Id != appointmentId &&
                              a.DoctorId == appointment.DoctorId &&
                              a.AppointmentDate.Date == newDate.Date &&
                              a.Status != AppointmentStatus.Cancelled &&
                              a.StartTime < newEndTime &&
                              a.EndTime > newStartTime);

            if (hasConflict)
            {
                throw new ConflictException("The new time slot is already booked");
            }

            appointment.AppointmentDate = newDate.Date;
            appointment.StartTime = newStartTime;
            appointment.EndTime = newEndTime;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} rescheduled to {NewDate} {NewTime}",
                appointmentId, newDate.Date, newStartTime);

            return MapToAppointmentResponse(appointment);
        }

        public async Task<AppointmentResponse> StartConsultationAsync(Guid appointmentId)
        {
            return await UpdateStatusAsync(appointmentId, AppointmentStatus.InProgress, "Consultation started");
        }

        public async Task<AppointmentResponse> UpdateAppointmentAsync(Guid appointmentId, UpdateAppointmentRequest request)
        {
            var appointment = await _context.Appointments
                           .Include(a => a.Patient)
                           .ThenInclude(p => p.User)
                           .Include(a => a.Doctor)
                           .ThenInclude(d => d.User)
                           .Include(a => a.CreatedByUser)
                           .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new NotFoundException("Appointment", appointmentId);
            }

            if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
            {
                throw new BusinessException($"Cannot update {appointment.Status.ToString().ToLower()} appointment");
            }

            if (request.Reason != null)
                appointment.Reason = request.Reason;

            if (request.Notes != null)
                appointment.Notes = request.Notes;

            if (request.Type.HasValue)
                appointment.Type = request.Type.Value;

            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} updated", appointmentId);

            return MapToAppointmentResponse(appointment);
        }

        public async Task<AppointmentResponse> UpdateStatusAsync(Guid appointmentId, AppointmentStatus status, string? notes)
        {
            var appointment = await _context.Appointments
                           .Include(a => a.Patient)
                           .ThenInclude(p => p.User)
                           .Include(a => a.Doctor)
                           .ThenInclude(d => d.User)
                           .Include(a => a.CreatedByUser)
                           .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new NotFoundException("Appointment", appointmentId);
            }
            var oldsatus = appointment.Status;


            // Validate status transitions
            ValidateStatusTransition(appointment.Status, status);

            appointment.Status = status;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                appointment.Notes = notes;
            }
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
      

            _logger.LogInformation(
                "Appointment {AppointmentId} status changed from {OldStatus} to {NewStatus}",
                appointmentId, oldsatus, status
            );
            return MapToAppointmentResponse(appointment);
        }

        private void ValidateStatusTransition(AppointmentStatus currentStatus, AppointmentStatus newStatus)
        {
            var validTransitions = new Dictionary<AppointmentStatus, List<AppointmentStatus>>
            {
                { AppointmentStatus.Scheduled, new List<AppointmentStatus> { AppointmentStatus.CheckedIn, AppointmentStatus.Cancelled, AppointmentStatus.NoShow } },
                { AppointmentStatus.CheckedIn, new List<AppointmentStatus> { AppointmentStatus.InProgress, AppointmentStatus.Cancelled, AppointmentStatus.NoShow } },
                { AppointmentStatus.InProgress, new List<AppointmentStatus> { AppointmentStatus.Completed, AppointmentStatus.Cancelled } },
                { AppointmentStatus.Completed, new List<AppointmentStatus>() },
                { AppointmentStatus.Cancelled, new List<AppointmentStatus>() },
                { AppointmentStatus.NoShow, new List<AppointmentStatus>() }
            };

            if (!validTransitions[currentStatus].Contains(newStatus))
            {
                throw new BusinessException(
                    $"Cannot transition appointment from {currentStatus} to {newStatus}");
            }
        }

        private async Task<string> GenerateAppointmentNumberAsync()
        {
            var year = DateTime.UtcNow.Year;

            var lastAppointment = await _context.Appointments
                .FromSqlRaw("SELECT * FROM Appointments WHERE AppointmentNumber LIKE {0} ORDER BY AppointmentNumber DESC LIMIT 1 FOR UPDATE",
                    $"AP-{year}-%")
                .FirstOrDefaultAsync();

            if (lastAppointment == null)
            {
                return $"AP-{year}-00001";
            }

            var parts = lastAppointment.AppointmentNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"AP-{year}-{(lastNumber + 1):D5}";
            }

            _logger.LogWarning("Invalid appointment number format: {AppointmentNumber}. Resetting to AP-{Year}-00001",
                lastAppointment.AppointmentNumber, year);
            return $"AP-{year}-00001";
        }
        public static bool DateValidation(DateTime StartDate, DateTime EndDate)
        {
            return StartDate < EndDate;
        }
        private AppointmentResponse MapToAppointmentResponse(Appointment appointment)
        {
            return new AppointmentResponse
            {
                Id = appointment.Id,
                AppointmentNumber = appointment.AppointmentNumber,

                PatientId = appointment.PatientId,
                PatientNumber = appointment.Patient.PatientNumber,
                PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                PatientEmail = appointment.Patient.User.Email,
                PatientPhone = appointment.Patient.User.PhoneNumber,

                DoctorId = appointment.DoctorId,
                DoctorNumber = appointment.Doctor.DoctorNumber,
                DoctorName = $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                DoctorSpecialization = appointment.Doctor.Specialization,

                AppointmentDate = appointment.AppointmentDate,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                Status = appointment.Status,
                Type = appointment.Type,
                Reason = appointment.Reason,
                Notes = appointment.Notes,
                CancellationReason = appointment.CancellationReason,

                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt,
                CreatedByName = $"{appointment.CreatedByUser.FirstName} {appointment.CreatedByUser.LastName}"
            };
        }
    }
}   