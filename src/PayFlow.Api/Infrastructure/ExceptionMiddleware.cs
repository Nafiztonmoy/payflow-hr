using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Domain.Exceptions;

namespace PayFlow.Api.Infrastructure;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        int statusCode = exception switch
        {
            EntityNotFoundException => (int)HttpStatusCode.NotFound,
            PayrollValidationException => (int)HttpStatusCode.BadRequest,
            ConcurrencyConflictException => (int)HttpStatusCode.Conflict,
            UnauthorizedDomainAccessException => (int)HttpStatusCode.Forbidden,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception switch
            {
                EntityNotFoundException => "Entity Not Found",
                PayrollValidationException => "Validation Failure",
                ConcurrencyConflictException => "Concurrency Conflict",
                UnauthorizedDomainAccessException => "Forbidden Action",
                _ => "An error occurred while processing your request"
            },
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        if (exception is PayrollValidationException valEx && valEx.Errors.Count > 1)
        {
            problemDetails.Extensions["errors"] = valEx.Errors;
        }

        context.Response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
