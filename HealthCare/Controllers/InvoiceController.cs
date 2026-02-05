using HealthcareSystem.Application.DTOs.Invoice;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Create invoice manually
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<InvoiceResponse>> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var invoice = await _invoiceService.CreateInvoiceAsync(request, userId);
            return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, invoice);
        }

        /// <summary>
        /// Create invoice from appointment automatically
        /// </summary>
        [HttpPost("from-appointment")]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<ActionResult<InvoiceResponse>> CreateInvoiceFromAppointment(
            [FromBody] CreateInvoiceFromAppointmentRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var invoice = await _invoiceService.CreateInvoiceFromAppointmentAsync(request, userId);
            return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, invoice);
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceResponse>> GetInvoiceById(Guid id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            return Ok(invoice);
        }

        /// <summary>
        /// Get invoice by invoice number
        /// </summary>
        [HttpGet("by-number/{invoiceNumber}")]
        public async Task<ActionResult<InvoiceResponse>> GetInvoiceByNumber(string invoiceNumber)
        {
            var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
            return Ok(invoice);
        }

        /// <summary>
        /// Get all invoices for a patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<InvoiceResponse>>> GetPatientInvoices(
            Guid patientId,
            [FromQuery] InvoiceStatus? status = null)
        {
            var invoices = await _invoiceService.GetPatientInvoicesAsync(patientId, status);
            return Ok(invoices);
        }

        /// <summary>
        /// Get all invoices with pagination and filters
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<InvoiceResponse>>> GetAllInvoices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] InvoiceStatus? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var invoices = await _invoiceService.GetAllInvoicesAsync(
                page, pageSize, status, fromDate, toDate, searchTerm);

            return Ok(  invoices);
        }

        /// <summary>
        /// Update invoice (only if status is Draft or Pending)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<InvoiceResponse>> UpdateInvoice(
            Guid id,
            [FromBody] UpdateInvoiceRequest request)
        {
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, request);
            return Ok(invoice);
        }

        /// <summary>
        /// Update invoice status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<InvoiceResponse>> UpdateInvoiceStatus(
            Guid id,
            [FromBody] UpdateInvoiceStatusRequest request)
        {
            var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, request.NewStatus);
            return Ok(invoice);
        }

        /// <summary>
        /// Send invoice to patient via email
        /// </summary>
        [HttpPost("{id}/send")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<InvoiceResponse>> SendInvoice(Guid id)
        {
            var invoice = await _invoiceService.SendInvoiceAsync(id);
            return Ok( invoice);
        }

        /// <summary>
        /// Cancel invoice
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CancelInvoice(Guid id)
        {
            await _invoiceService.CancelInvoiceAsync(id);
            return Ok(new { message = "Invoice cancelled successfully" });
        }

        /// <summary>
        /// Get overdue invoices
        /// </summary>
        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<InvoiceResponse>>> GetOverdueInvoices()
        {
            var invoices = await _invoiceService.GetOverdueInvoicesAsync();
            return Ok(invoices);
        }

        /// <summary>
        /// Get revenue statistics
        /// </summary>
        [HttpGet("statistics/revenue")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetRevenueStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            // Default to current month if dates not provided
            var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var to = toDate ?? DateTime.UtcNow;

            var statistics = await _invoiceService.GetRevenueStatisticsAsync(from, to);
            return Ok(statistics);
        }
    }

    // Additional DTO for status update
    public class UpdateInvoiceStatusRequest
    {
        public InvoiceStatus NewStatus { get; set; }
    }
}