using HealthcareSystem.Application.DTOs.Payment;
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
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Create a new payment for an invoice
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var payment = await _paymentService.CreatePaymentAsync(request, userId);
            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentResponse>> GetPaymentById(Guid id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            return Ok(payment);
        }

        /// <summary>
        /// Get payment by payment number
        /// </summary>
        [HttpGet("by-number/{paymentNumber}")]
        public async Task<ActionResult<PaymentResponse>> GetPaymentByNumber(string paymentNumber)
        {
            var payment = await _paymentService.GetPaymentByNumberAsync(paymentNumber);
            return Ok(payment);
        }

        /// <summary>
        /// Get all payments for a specific invoice
        /// </summary>
        [HttpGet("invoice/{invoiceId}")]
        public async Task<ActionResult<List<PaymentResponse>>> GetInvoicePayments(Guid invoiceId)
        {
            var payments = await _paymentService.GetInvoicePaymentsAsync(invoiceId);
            return Ok(payments);
        }

        /// <summary>
        /// Get all payments for a specific patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<PaymentResponse>>> GetPatientPayments(Guid patientId)
        {
            var payments = await _paymentService.GetPatientPaymentsAsync(patientId);
            return Ok(payments);
        }

        /// <summary>
        /// Get all payments with pagination and filters
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<PaymentResponse>>> GetAllPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] PaymentMethod? paymentMethod = null,
            [FromQuery] PaymentStatus? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var payments = await _paymentService.GetAllPaymentsAsync(
                page, pageSize, paymentMethod, status, fromDate, toDate, searchTerm);

            return Ok(new
            {
                page,
                pageSize,
                data = payments
            });
        }

        /// <summary>
        /// Update payment status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatus(
            Guid id,
            [FromBody] UpdatePaymentStatusRequest request)
        {
            var payment = await _paymentService.UpdatePaymentStatusAsync(id, request.NewStatus);
            return Ok(payment);
        }

        /// <summary>
        /// Refund a payment
        /// </summary>
        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
        {
            await _paymentService.RefundPaymentAsync(id, request.Reason);
            return Ok(new { message = "Payment refunded successfully" });
        }

        /// <summary>
        /// Get payment statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetPaymentStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            // Default to current month if dates not provided
            var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var to = toDate ?? DateTime.UtcNow;

            var statistics = await _paymentService.GetPaymentStatisticsAsync(from, to);
            return Ok(statistics);
        }
    }

    // Additional DTOs for payment operations
  
}