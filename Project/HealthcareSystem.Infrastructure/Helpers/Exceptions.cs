using System;
using System.Collections.Generic;

namespace HealthcareSystem.Infrastructure.Helpers
{
    /// <summary>
    /// Base exception for business logic errors
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }

        public BusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a requested resource is not found
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object key)
            : base($"{entityName} with key '{key}' was not found")
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]>? Errors { get; }

        public ValidationException(string message)
            : base(message)
        {
        }

        public ValidationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// Exception thrown when a resource already exists
    /// </summary>
    public class DuplicateException : Exception
    {
        public DuplicateException(string message) : base(message)
        {
        }

        public DuplicateException(string entityName, string propertyName, object value)
            : base($"{entityName} with {propertyName} '{value}' already exists")
        {
        }
    }

    /// <summary>
    /// Exception thrown when an operation conflicts with the current state
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}