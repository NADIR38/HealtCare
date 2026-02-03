using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// Catches all unhandled exceptions and returns appropriate HTTP responses
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception with correlation ID
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogError(exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method);

            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("X-Correlation-ID", correlationId);

            var (statusCode, message, errors) = GetExceptionDetails(exception);
            context.Response.StatusCode = statusCode;

            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                CorrelationId = correlationId,
                Path = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            // Only include details in development
            if (_env.IsDevelopment())
            {
                response.Details = exception.Message;
                response.StackTrace = exception.StackTrace;
                response.InnerException = exception.InnerException?.Message;
            }

            if (errors != null && errors.Count > 0)
            {
                response.Errors = errors;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }

        private (int statusCode, string message, Dictionary<string, string[]>? errors) GetExceptionDetails(Exception exception)
        {
            return exception switch
            {
                NotFoundException notFoundEx => (
                    (int)HttpStatusCode.NotFound,
                    notFoundEx.Message,
                    null
                ),
                ValidationException validationEx => (
                    (int)HttpStatusCode.BadRequest,
                    validationEx.Message,
                    validationEx.Errors
                ),
                ConflictException conflictEx => (
                    (int)HttpStatusCode.Conflict,
                    conflictEx.Message,
                    null
                ),
                DuplicateException duplicateEx => (
                    (int)HttpStatusCode.Conflict,
                    duplicateEx.Message,
                    null
                ),
                BusinessException businessEx => (
                    (int)HttpStatusCode.BadRequest,
                    businessEx.Message,
                    null
                ),
                UnauthorizedAccessException _ => (
                    (int)HttpStatusCode.Unauthorized,
                    "Unauthorized access",
                    null
                ),
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again later.",
                    null
                )
            };
        }
    }

    /// <summary>
    /// Standard error response model
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public string Path { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}