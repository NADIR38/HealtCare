using HealthcareSystem.Application.Dto.Patient;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : Controller
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientservice)
        {
            _patientService = patientservice;
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CreatePatient([FromBody]CreatePatientRequest request)
        {
            try
            {
                var response = await _patientService.CreatePatientAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // InnerException ka message asli wajah batayega (e.g. Data too long, or Column cannot be null)
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = innerMessage });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            try
            {
                var response = await _patientService.GetPatientByIdAsync(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPatientByUserId(Guid userId)
        {
            try
            {
                var response = await _patientService.GetPatientByUserIdAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllPatients([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search=null)
        {
            try
            {
                var response = await _patientService.GetAllPatientsAsync(page,pageSize,search);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequest request)
        {
            try
            {

                var response = await _patientService.UpdatePatientAsync(id, request);


                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            try
            {
                await _patientService.DeletePatientAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Medical History Endpoints
        [HttpPost("{patientId}/medical-history")]
        public async Task<IActionResult> CreateOrUpdateMedicalHistory(
            Guid patientId,
            [FromBody] MedicalHistoryRequest request)
        {
            try
            {
                var response = await _patientService.CreateOrUpdateMedicalHistoryAsync(patientId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("number/{patientNumber}")]
        public async Task<IActionResult> GetPatientByNumber(string patientNumber)
        {
            try
            {
                var response = await _patientService.GetPatientByNumberAsync(patientNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpGet("{patientId}/medical-history")]
        public async Task<IActionResult> GetMedicalHistory(Guid patientId)
        {
            try
            {
                var response = await _patientService.GetMedicalHistoryAsync(patientId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


    }
}
