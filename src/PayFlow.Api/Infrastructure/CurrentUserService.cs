using System.Security.Claims;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Enums;

namespace PayFlow.Api.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public UserRole? Role
    {
        get
        {
            var roleStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                       ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
            return Enum.TryParse<UserRole>(roleStr, true, out var role) ? role : null;
        }
    }

    public Guid? EmployeeId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("employee_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? CorrelationId => _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
}

public class CorrelationIdMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;

        await _next(context);
    }
}
