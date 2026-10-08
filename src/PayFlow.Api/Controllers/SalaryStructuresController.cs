using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/salary-structures")]
[Authorize(Roles = "Admin,HR,Accountant")]
public class SalaryStructuresController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly IAuditService _auditService;

    public SalaryStructuresController(PayFlowDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public record PayComponentDto(Guid Id, string Code, string DisplayName, string Type, string CalculationMode, decimal DefaultAmount, decimal PercentageRate, bool IsTaxable, int DisplayOrder);
    public record SalaryStructureDto(Guid Id, string Name, string Code, string? Description, int Version, DateTime EffectiveFrom, bool IsActive, int EmployeeCount, List<PayComponentDto> Components);
    public record CreateSalaryStructureRequest(string Name, string Code, string? Description, DateTime EffectiveFrom, List<CreatePayComponentRequest> Components);
    public record CreatePayComponentRequest(string Code, string DisplayName, PayComponentType Type, CalculationMode CalculationMode, decimal DefaultAmount, decimal PercentageRate, bool IsTaxable, int DisplayOrder);

    [HttpGet]
    public async Task<IActionResult> GetSalaryStructures(CancellationToken ct)
    {
        var structures = await _context.SalaryStructures
            .Include(s => s.Components)
            .Include(s => s.Compensations)
            .OrderBy(s => s.Name)
            .Select(s => new SalaryStructureDto(
                s.Id,
                s.Name,
                s.Code,
                s.Description,
                s.Version,
                s.EffectiveFrom,
                s.IsActive,
                s.Compensations.Count(c => c.IsActive),
                s.Components.OrderBy(c => c.DisplayOrder).Select(c => new PayComponentDto(
                    c.Id,
                    c.Code,
                    c.DisplayName,
                    c.Type.ToString(),
                    c.CalculationMode.ToString(),
                    c.DefaultAmount,
                    c.PercentageRate,
                    c.IsTaxable,
                    c.DisplayOrder
                )).ToList()
            ))
            .ToListAsync(ct);

        return Ok(structures);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> CreateSalaryStructure([FromBody] CreateSalaryStructureRequest req, CancellationToken ct)
    {
        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Code = req.Code.ToUpper(),
            Description = req.Description,
            Version = 1,
            EffectiveFrom = req.EffectiveFrom,
            IsActive = true
        };

        foreach (var c in req.Components)
        {
            structure.Components.Add(new PayComponent
            {
                Id = Guid.NewGuid(),
                SalaryStructureId = structure.Id,
                Code = c.Code.ToUpper(),
                DisplayName = c.DisplayName,
                Type = c.Type,
                CalculationMode = c.CalculationMode,
                DefaultAmount = c.DefaultAmount,
                PercentageRate = c.PercentageRate,
                IsTaxable = c.IsTaxable,
                DisplayOrder = c.DisplayOrder,
                IsActive = true
            });
        }

        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync("CREATE_SALARY_STRUCTURE", nameof(SalaryStructure), structure.Id.ToString(), $"Created salary structure '{structure.Name}' ({structure.Code}) with {req.Components.Count} components.", null, structure, ct);

        return Ok(structure);
    }
}
