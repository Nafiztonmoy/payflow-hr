using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Exceptions;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public EmployeesController(
        PayFlowDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public record EmployeeListDto(
        Guid Id,
        string EmployeeNo,
        string FullName,
        string WorkEmail,
        string DepartmentName,
        string DesignationTitle,
        string? ManagerName,
        string Status,
        DateTime JoinDate
    );

    public record CreateEmployeeRequest(
        string EmployeeNo,
        string FirstName,
        string LastName,
        string WorkEmail,
        string? Phone,
        DateTime DateOfBirth,
        DateTime JoinDate,
        Guid DepartmentId,
        Guid DesignationId,
        Guid? ManagerId,
        PaymentMethod PaymentMethod,
        string? BankName,
        string? BankAccountNumber,
        string? BankRoutingNumber,
        Guid SalaryStructureId,
        decimal BaseSalary
    );

    public record UpdateEmployeeRequest(
        string? FirstName,
        string? LastName,
        string? Phone,
        Guid? DepartmentId,
        Guid? DesignationId,
        Guid? ManagerId,
        EmploymentStatus? Status,
        PaymentMethod? PaymentMethod,
        string? BankName,
        string? BankAccountNumber,
        string? BankRoutingNumber
    );

    public record AddCompensationRequest(
        Guid SalaryStructureId,
        decimal BaseSalary,
        DateTime EffectiveFrom,
        string? Remarks
    );

    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] EmploymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Manager)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.EmployeeNo.ToLower().Contains(s) ||
                                     e.FirstName.ToLower().Contains(s) ||
                                     e.LastName.ToLower().Contains(s) ||
                                     e.WorkEmail.ToLower().Contains(s));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListDto(
                e.Id,
                e.EmployeeNo,
                e.FullName,
                e.WorkEmail,
                e.Department != null ? e.Department.Name : "N/A",
                e.Designation != null ? e.Designation.Title : "N/A",
                e.Manager != null ? e.Manager.FullName : null,
                e.Status.ToString(),
                e.JoinDate
            ))
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken ct)
    {
        var emp = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (emp == null)
            return NotFound(new { message = $"Employee '{id}' not found." });

        return Ok(new
        {
            emp.Id,
            emp.EmployeeNo,
            emp.FirstName,
            emp.LastName,
            emp.FullName,
            emp.WorkEmail,
            emp.Phone,
            emp.DateOfBirth,
            emp.JoinDate,
            emp.TerminationDate,
            Status = emp.Status.ToString(),
            emp.DepartmentId,
            DepartmentName = emp.Department?.Name,
            emp.DesignationId,
            DesignationTitle = emp.Designation?.Title,
            emp.ManagerId,
            ManagerName = emp.Manager?.FullName,
            PaymentMethod = emp.PaymentMethod.ToString(),
            emp.BankName,
            emp.BankAccountNumber,
            emp.BankRoutingNumber
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest req, CancellationToken ct)
    {
        bool exists = await _context.Employees.AnyAsync(e => e.EmployeeNo == req.EmployeeNo || e.WorkEmail == req.WorkEmail, ct);
        if (exists)
            return BadRequest(new { message = "Employee with this Employee Number or Work Email already exists." });

        var emp = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = req.EmployeeNo,
            FirstName = req.FirstName,
            LastName = req.LastName,
            WorkEmail = req.WorkEmail,
            Phone = req.Phone,
            DateOfBirth = req.DateOfBirth,
            JoinDate = req.JoinDate,
            DepartmentId = req.DepartmentId,
            DesignationId = req.DesignationId,
            ManagerId = req.ManagerId,
            PaymentMethod = req.PaymentMethod,
            BankName = req.BankName,
            BankAccountNumber = req.BankAccountNumber,
            BankRoutingNumber = req.BankRoutingNumber,
            Status = EmploymentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var comp = new EmployeeCompensation
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp.Id,
            SalaryStructureId = req.SalaryStructureId,
            BaseSalary = req.BaseSalary,
            EffectiveFrom = req.JoinDate,
            IsActive = true,
            Remarks = "Initial salary assignment upon hire",
            CreatedAtUtc = DateTime.UtcNow
        };

        emp.Compensations.Add(comp);

        _context.Employees.Add(emp);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "CREATE_EMPLOYEE",
            nameof(Employee),
            emp.Id.ToString(),
            $"Created employee {emp.FullName} ({emp.EmployeeNo}) with initial base salary ${req.BaseSalary:N2}.",
            null,
            new { emp.Id, emp.EmployeeNo, emp.FullName },
            ct
        );

        return CreatedAtAction(nameof(GetEmployeeById), new { id = emp.Id }, new { emp.Id, emp.EmployeeNo, emp.FullName });
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest req, CancellationToken ct)
    {
        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (emp == null)
            return NotFound(new { message = $"Employee '{id}' not found." });

        var beforeValues = new
        {
            emp.FirstName,
            emp.LastName,
            emp.Phone,
            emp.DepartmentId,
            emp.DesignationId,
            emp.ManagerId,
            emp.Status,
            emp.PaymentMethod,
            emp.BankName,
            emp.BankAccountNumber,
            emp.BankRoutingNumber
        };

        if (!string.IsNullOrWhiteSpace(req.FirstName)) emp.FirstName = req.FirstName;
        if (!string.IsNullOrWhiteSpace(req.LastName)) emp.LastName = req.LastName;
        if (req.Phone != null) emp.Phone = req.Phone;
        if (req.DepartmentId.HasValue) emp.DepartmentId = req.DepartmentId.Value;
        if (req.DesignationId.HasValue) emp.DesignationId = req.DesignationId.Value;
        if (req.ManagerId.HasValue) emp.ManagerId = req.ManagerId.Value;
        if (req.Status.HasValue) emp.Status = req.Status.Value;
        if (req.PaymentMethod.HasValue) emp.PaymentMethod = req.PaymentMethod.Value;
        if (req.BankName != null) emp.BankName = req.BankName;
        if (req.BankAccountNumber != null) emp.BankAccountNumber = req.BankAccountNumber;
        if (req.BankRoutingNumber != null) emp.BankRoutingNumber = req.BankRoutingNumber;

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "UPDATE_EMPLOYEE",
            nameof(Employee),
            emp.Id.ToString(),
            $"Updated employee details for {emp.FullName} ({emp.EmployeeNo}).",
            beforeValues,
            new { emp.FirstName, emp.LastName, emp.Status, emp.PaymentMethod, emp.BankAccountNumber },
            ct
        );

        return Ok(new { message = "Employee updated successfully.", emp.Id });
    }

    [HttpGet("{id}/compensation")]
    public async Task<IActionResult> GetEmployeeCompensation(Guid id, CancellationToken ct)
    {
        // Enforce strict security boundaries:
        // Admin, HR, and Accountant can view.
        // Employee can view ONLY their own compensation.
        // Manager CANNOT view compensation unless they have HR/Admin role.
        var role = _currentUserService.Role;
        bool isPrivileged = role == UserRole.Admin || role == UserRole.HR || role == UserRole.Accountant;
        bool isSelf = _currentUserService.EmployeeId == id;

        if (!isPrivileged && !isSelf)
        {
            return Forbid();
        }

        var compensations = await _context.EmployeeCompensations
            .Include(c => c.SalaryStructure)
                .ThenInclude(s => s!.Components)
            .Where(c => c.EmployeeId == id)
            .OrderByDescending(c => c.EffectiveFrom)
            .Select(c => new
            {
                c.Id,
                c.SalaryStructureId,
                SalaryStructureName = c.SalaryStructure != null ? c.SalaryStructure.Name : "N/A",
                c.BaseSalary,
                c.EffectiveFrom,
                c.EffectiveTo,
                c.IsActive,
                c.Remarks,
                Components = c.SalaryStructure != null ? c.SalaryStructure.Components.Select(cp => new
                {
                    cp.Code,
                    cp.DisplayName,
                    Type = cp.Type.ToString(),
                    Mode = cp.CalculationMode.ToString(),
                    cp.DefaultAmount,
                    cp.PercentageRate,
                    cp.IsTaxable
                }) : null
            })
            .ToListAsync(ct);

        return Ok(compensations);
    }

    [HttpPost("{id}/compensation")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> AddCompensation(Guid id, [FromBody] AddCompensationRequest req, CancellationToken ct)
    {
        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (emp == null)
            return NotFound(new { message = $"Employee '{id}' not found." });

        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.Id == req.SalaryStructureId, ct);
        if (structure == null)
            return BadRequest(new { message = "Salary Structure not found." });

        // Effective dating: close previous active compensation
        var activeComp = await _context.EmployeeCompensations
            .Where(c => c.EmployeeId == id && c.IsActive && c.EffectiveTo == null)
            .FirstOrDefaultAsync(ct);

        if (activeComp != null)
        {
            activeComp.EffectiveTo = req.EffectiveFrom.AddDays(-1);
            activeComp.IsActive = false;
        }

        var newComp = new EmployeeCompensation
        {
            Id = Guid.NewGuid(),
            EmployeeId = id,
            SalaryStructureId = req.SalaryStructureId,
            BaseSalary = req.BaseSalary,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            Remarks = req.Remarks ?? "Salary revision",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.EmployeeCompensations.Add(newComp);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "ASSIGN_COMPENSATION",
            nameof(EmployeeCompensation),
            newComp.Id.ToString(),
            $"Assigned compensation to {emp.FullName}: Structure '{structure.Name}', Base ${req.BaseSalary:N2}, Effective From {req.EffectiveFrom:yyyy-MM-dd}.",
            activeComp != null ? new { activeComp.BaseSalary, activeComp.EffectiveFrom } : null,
            new { newComp.BaseSalary, newComp.EffectiveFrom },
            ct
        );

        return Ok(new { message = "Compensation assigned successfully.", newComp.Id });
    }
}
