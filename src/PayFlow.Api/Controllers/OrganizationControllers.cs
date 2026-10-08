using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly IAuditService _auditService;

    public DepartmentsController(PayFlowDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public record DepartmentDto(Guid Id, string Name, string Code, string? Description, Guid? ManagerEmployeeId, string? ManagerName, int EmployeeCount, bool IsActive);
    public record CreateDepartmentRequest(string Name, string Code, string? Description, Guid? ManagerEmployeeId);

    [HttpGet]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var departments = await _context.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Name,
                d.Code,
                d.Description,
                d.ManagerEmployeeId,
                d.Manager != null ? d.Manager.FullName : null,
                d.Employees.Count,
                d.IsActive
            ))
            .ToListAsync(ct);

        return Ok(departments);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest req, CancellationToken ct)
    {
        if (await _context.Departments.AnyAsync(d => d.Code == req.Code, ct))
            return BadRequest(new { message = $"Department code '{req.Code}' already exists." });

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Code = req.Code.ToUpper(),
            Description = req.Description,
            ManagerEmployeeId = req.ManagerEmployeeId,
            IsActive = true
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync("CREATE_DEPARTMENT", nameof(Department), dept.Id.ToString(), $"Created department '{dept.Name}' ({dept.Code}).", null, dept, ct);

        return Ok(dept);
    }
}

[ApiController]
[Route("api/v1/designations")]
[Authorize]
public class DesignationsController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly IAuditService _auditService;

    public DesignationsController(PayFlowDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public record DesignationDto(Guid Id, string Title, string Code, int Level, string? Description, int EmployeeCount, bool IsActive);
    public record CreateDesignationRequest(string Title, string Code, int Level, string? Description);

    [HttpGet]
    public async Task<IActionResult> GetDesignations(CancellationToken ct)
    {
        var designations = await _context.Designations
            .Include(d => d.Employees)
            .OrderBy(d => d.Level)
            .ThenBy(d => d.Title)
            .Select(d => new DesignationDto(
                d.Id,
                d.Title,
                d.Code,
                d.Level,
                d.Description,
                d.Employees.Count,
                d.IsActive
            ))
            .ToListAsync(ct);

        return Ok(designations);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationRequest req, CancellationToken ct)
    {
        if (await _context.Designations.AnyAsync(d => d.Code == req.Code, ct))
            return BadRequest(new { message = $"Designation code '{req.Code}' already exists." });

        var des = new Designation
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Code = req.Code.ToUpper(),
            Level = req.Level,
            Description = req.Description,
            IsActive = true
        };

        _context.Designations.Add(des);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync("CREATE_DESIGNATION", nameof(Designation), des.Id.ToString(), $"Created designation '{des.Title}' ({des.Code}).", null, des, ct);

        return Ok(des);
    }
}
