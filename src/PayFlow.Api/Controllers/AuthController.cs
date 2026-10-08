using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AuthController(
        PayFlowDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public record LoginRequest(string EmailOrUsername, string Password);
    public record RefreshRequest(string? RefreshToken);
    public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
    public record UserDto(Guid Id, string UserName, string Email, string Role, Guid? EmployeeId, string? EmployeeName);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.EmailOrUsername.ToLower() ||
                                      u.UserName.ToLower() == request.EmailOrUsername.ToLower(), ct);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "User account is inactive. Please contact your administrator." });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTimeUtc = DateTime.UtcNow.AddDays(7);
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Set HttpOnly cookie for SPA security
        SetTokenCookie(accessToken);

        await _auditService.LogAsync(
            "USER_LOGIN",
            nameof(User),
            user.Id.ToString(),
            $"User {user.UserName} ({user.Role}) logged in successfully.",
            null,
            null,
            ct
        );

        var userDto = new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            user.EmployeeId,
            user.Employee?.FullName
        );

        return Ok(new AuthResponse(accessToken, refreshToken, userDto));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        var token = request?.RefreshToken;
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.RefreshToken == token && u.RefreshTokenExpiryTimeUtc > DateTime.UtcNow, ct);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTimeUtc = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        SetTokenCookie(newAccessToken);

        var userDto = new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            user.EmployeeId,
            user.Employee?.FullName
        );

        return Ok(new AuthResponse(newAccessToken, newRefreshToken, userDto));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (_currentUserService.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(new object[] { _currentUserService.UserId.Value }, ct);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTimeUtc = null;
                await _context.SaveChangesAsync(ct);
            }
        }

        Response.Cookies.Delete("payflow_token");
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e.Department)
            .Include(u => u.Employee)
                .ThenInclude(e => e.Designation)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, ct);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            Role = user.Role.ToString(),
            user.EmployeeId,
            Employee = user.Employee != null ? new
            {
                user.Employee.Id,
                user.Employee.EmployeeNo,
                user.Employee.FullName,
                Department = user.Employee.Department?.Name,
                Designation = user.Employee.Designation?.Title,
                user.Employee.Status
            } : null
        });
    }

    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
        Secure = !HttpContext.RequestServices
    .GetRequiredService<IWebHostEnvironment>()
    .IsDevelopment(), // true in prod with HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddHours(8)
        };
        Response.Cookies.Append("payflow_token", token, cookieOptions);
    }
}
