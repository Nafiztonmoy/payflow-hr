using System.Security.Claims;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;

namespace PayFlow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    UserRole? Role { get; }
    Guid? EmployeeId { get; }
    string? CorrelationId { get; }
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, string summary, object? beforeValues = null, object? afterValues = null, CancellationToken ct = default);
}
