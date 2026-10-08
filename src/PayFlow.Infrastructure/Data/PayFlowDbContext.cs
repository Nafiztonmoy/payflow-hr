using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Common;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Data;

public class PayFlowDbContext : DbContext
{
    public PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<PayComponent> PayComponents => Set<PayComponent>();
    public DbSet<EmployeeCompensation> EmployeeCompensations => Set<EmployeeCompensation>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<TaxRuleSet> TaxRuleSets => Set<TaxRuleSet>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<PayrollItemComponent> PayrollItemComponents => Set<PayrollItemComponent>();
    public DbSet<PayrollException> PayrollExceptions => Set<PayrollException>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Decimal Precisions (18, 2)
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.UserName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();

            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Designation
        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
        });

        // Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeNo).IsUnique();
            entity.HasIndex(e => e.WorkEmail).IsUnique();
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.EmployeeNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.WorkEmail).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Designation)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Manager)
                .WithMany(m => m.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SalaryStructure
        modelBuilder.Entity<SalaryStructure>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // PayComponent
        modelBuilder.Entity<PayComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.CalculationMode).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.SalaryStructure)
                .WithMany(s => s.Components)
                .HasForeignKey(e => e.SalaryStructureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmployeeCompensation
        modelBuilder.Entity<EmployeeCompensation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.EffectiveFrom });

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Compensations)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SalaryStructure)
                .WithMany(s => s.Compensations)
                .HasForeignKey(e => e.SalaryStructureId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AttendanceRecord
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.Date }).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.CorrectionStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.AttendanceRecords)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LeaveType
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // LeaveBalance
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.LeaveBalances)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LeaveType)
                .WithMany(lt => lt.LeaveBalances)
                .HasForeignKey(e => e.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.Status });
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.LeaveRequests)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LeaveType)
                .WithMany(lt => lt.LeaveRequests)
                .HasForeignKey(e => e.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TaxRuleSet
        modelBuilder.Entity<TaxRuleSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.JurisdictionLabel, e.Version });
        });

        // PayrollRun
        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PeriodYear, e.PeriodMonth, e.RunNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });

        // PayrollItem
        modelBuilder.Entity<PayrollItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.PayrollRun)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PayrollItemComponent
        modelBuilder.Entity<PayrollItemComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PayrollItemId);
            entity.Property(e => e.ComponentType).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.PayrollItem)
                .WithMany(i => i.Components)
                .HasForeignKey(e => e.PayrollItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PayrollException
        modelBuilder.Entity<PayrollException>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PayrollRunId, e.IsResolved });
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.PayrollRun)
                .WithMany(r => r.Exceptions)
                .HasForeignKey(e => e.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PayrollItem)
                .WithMany(i => i.Exceptions)
                .HasForeignKey(e => e.PayrollItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payslip
        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PayslipNumber).IsUnique();
            entity.HasIndex(e => e.PayrollItemId).IsUnique();

            entity.HasOne(e => e.PayrollItem)
                .WithOne(i => i.Payslip)
                .HasForeignKey<Payslip>(e => e.PayrollItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => e.CorrelationId);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        var concurrencyEntries = ChangeTracker.Entries<IConcurrencyAware>();
        foreach (var entry in concurrencyEntries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
