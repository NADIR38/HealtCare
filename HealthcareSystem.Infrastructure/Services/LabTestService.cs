using HealthcareSystem.Application.DTOs.LabTest;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Application.Models;
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
using static System.Net.Mime.MediaTypeNames;
namespace HealthcareSystem.Infrastructure.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LabTestService> _logger;
        private readonly DatabaseHelpers _helper;
        private readonly IEmailService _emailService;
        public LabTestService(ApplicationDbContext context, ILogger<LabTestService> logger, DatabaseHelpers helper,IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _helper = helper;
            _emailService=emailService;
        }

        public async Task<bool> CancelLabTestAsync(Guid labTestId)
        {
            var Test = await _context.LabTests.Include(u => u.Doctor).ThenInclude(u => u.User).Include(p => p.Patient).ThenInclude(u => u.User).FirstOrDefaultAsync(u => u.Id == labTestId);
            if (Test == null)
            {
                throw new NotFoundException("No test found for this id", labTestId);

            }
            if (Test.Status == LabTestStatus.Completed)
            {
                throw new BusinessException("Test Already Completed");
            }

            Test.Status = LabTestStatus.Cancelled;
           await  _context.SaveChangesAsync();
          
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    Subject = $"Cancellation of Test{Test.TestName}",
                    Body = $"The test is Cancelled",
                    To = new List<string> { Test.Patient.User.Email},
                });
              
            return true;
        }

        public async  Task<List<LabTestResponse>> GetDoctorLabTestsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate)
        {
            var labTest = _context.LabTests.Include(u => u.Doctor).ThenInclude(u => u.User).Include(p => p.Patient).ThenInclude(u => u.User).Where(u=>u.DoctorId==doctorId).AsQueryable();
            if(labTest== null)
            {
                throw new NotFoundException("No Test for this Id", doctorId);

            }
            var doctor = await _helper.CheckDoctorExists(doctorId);
            if (fromDate != null && toDate != null)
            {
                labTest = labTest.Where(u => u.CreatedAt > fromDate && u.CreatedAt < toDate);
            }
            var tests=await labTest.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var response = new List<LabTestResponse>();
            foreach (var item in tests)
            {
                response.Add( MapToResponse(item));
            }
            return response;
        }

        public async  Task<LabTestResponse> GetLabTestByIdAsync(Guid labTestId)
        {
            var labTest= await _context.LabTests.Include(u=>u.Doctor).ThenInclude(u=>u.User).Include(p=>p.Patient).ThenInclude(u=>u.User).FirstOrDefaultAsync(u=>u.Id==labTestId);
            if(labTest==null)
            {
                throw new NotFoundException("No test found for this id", labTestId);
            }
            return  MapToResponse(labTest);
        }

        private LabTestResponse MapToResponse(LabTest labTest)
        {
            var response = new LabTestResponse
            {
                Id = labTest.Id,
                DoctorName = labTest.Doctor.User.FirstName,
                PatientId = labTest.Patient.Id,
                DoctorId = labTest.Doctor.Id,
                PatientName = labTest.Patient.User.FirstName,
                TestName = labTest.TestName,
                TestType = labTest.TestType,
                Status = labTest.Status,
                OrderedDate = labTest.OrderedDate,
                SampleCollectedDate = labTest.SampleCollectedDate,
                ResultDate = labTest.ResultDate,
                Results = labTest.Results,
                ResultFileUrl = labTest.ResultFileUrl,
                Notes = labTest.Notes,
                CreatedAt = labTest.CreatedAt,
                UpdatedAt = labTest.UpdatedAt
            };
            return response;
        }

        public async  Task<List<LabTestResponse>> GetPatientLabTestsAsync(Guid patientId, LabTestStatus? status)
        {
            var patient = await _helper.CheckPatientExist(patientId);
            var labTest = _context.LabTests.Include(u => u.Doctor).ThenInclude(u => u.User).Include(p => p.Patient).ThenInclude(u => u.User).Where(u => u.PatientId==patientId).AsQueryable();
            if (status.HasValue)
            {
               labTest= labTest.Where(u=>u.Status==status.Value);
            }
            var tests = await labTest.OrderByDescending(u => u.OrderedDate).ToListAsync();
            var response = new List<LabTestResponse>();
            foreach (var item in tests)
            {
                response.Add( MapToResponse(item));
            }
            return response;

        }

        public async Task<LabTestResponse> OrderLabTestAsync(CreateLabTestRequest request)
        {
            var doctor=await _helper.CheckDoctorExists(request.DoctorId);
            var patient=await _helper.CheckPatientExist(request.PatientId);
            if (request.MedicalRecordId.HasValue)
            {
               request.MedicalRecordId= request.MedicalRecordId.Value;
            }
            var labTest = new LabTest
            {
                Id=Guid.NewGuid(),
                PatientId=request.PatientId,
                DoctorId=request.DoctorId,
                MedicalRecordId=request.MedicalRecordId,
                TestName=request.TestName,
                TestType=request.TestType,
                OrderedDate=DateTime.UtcNow,
                Status=LabTestStatus.Ordered,
                CreatedAt=DateTime.UtcNow,
                UpdatedAt=DateTime.UtcNow,


            };

            
            _context.LabTests.Add(labTest);
            await _context.SaveChangesAsync();
            var response =new LabTestResponse{
               Id=labTest.Id,
               PatientId=labTest.PatientId,
               DoctorId=labTest.DoctorId,
               DoctorName=doctor.User.FirstName,
               PatientName=patient.User.FirstName,
               TestName=labTest.TestName,
               TestType=labTest.TestType,
               OrderedDate=labTest.OrderedDate,
               Status=labTest.Status,
               CreatedAt=labTest.CreatedAt,
               UpdatedAt =labTest.UpdatedAt,

            };
            return response;
        }

        public async Task<LabTestResponse> UpdateLabTestAsync(Guid labTestId, UpdateLabTestRequest request)
        {
            var labTest = await _context.LabTests.Include(u => u.Doctor).ThenInclude(u => u.User).Include(p => p.Patient).ThenInclude(u => u.User).FirstOrDefaultAsync(u => u.Id == labTestId);
            if(labTest == null)
            {
                throw new NotFoundException("No test for thsi id", labTestId);
            }
               if (request.Status != null)
                {
                    labTest.Status = (LabTestStatus)request.Status;
                }
            labTest.Results = request.Results;
            labTest.Notes= request.Notes;

            
            await _context.SaveChangesAsync();
            if (request.Status == LabTestStatus.Completed)
            {
               await  _emailService.SendEmailAsync(new EmailMessage
                {
                    Body = $"The Lab Test {labTest.TestName} is Completed on Date{labTest.ResultDate}  ",
                    Subject=$"Status Update regarding your Test about{labTest.TestName}",
                   To = new List<string> { labTest.Patient.User.Email },

               });
            }

            return  MapToResponse(labTest);
            

        }

        public async Task<LabTestResponse> UploadLabTestResultAsync(Guid labTestId, byte[] fileContent, string fileName)
        {
            var labTest = await _context.LabTests.Include(u => u.Doctor).ThenInclude(u => u.User).Include(p => p.Patient).ThenInclude(u => u.User).FirstOrDefaultAsync(u => u.Id == labTestId);
            if (labTest == null)
            {
                throw new NotFoundException("No test for thsi id", labTestId);
            }
            var uploadsFolder = Path.Combine("uploads", "lab_results");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(filePath, fileContent);
            labTest.Status = LabTestStatus.Completed;
            labTest.ResultDate = DateTime.UtcNow;
            labTest.ResultFileUrl = filePath;   
            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(new EmailMessage
            {
                Subject = $"Uplaod of report Of test {labTest.TestName}",
                Body = $"The test is Completd and report is Uploaded on Website",
                To = new List<string> { labTest.Patient.User.Email },
            });
            var response =  MapToResponse(labTest);
            return response;
         
        }
    }
}
