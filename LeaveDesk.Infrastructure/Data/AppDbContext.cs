using LeaveDesk.Domain.Entities;
using LeaveDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveDesk.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments => Set<LeaveBalanceAdjustment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>(entity =>
        {
            entity.HasIndex(d => d.Name).IsUnique();
        });

        builder.Entity<LeaveType>(entity =>
        {
            entity.HasIndex(lt => lt.Name).IsUnique();
        });

        builder.Entity<Holiday>(entity =>
        {
            entity.HasIndex(h => h.Date).IsUnique();
        });

        builder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Manager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveBalance>(entity =>
        {
            entity.Ignore(b => b.RemainingDays);

            entity.HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.Year }).IsUnique();

            entity.HasOne(b => b.Employee)
                .WithMany(e => e.LeaveBalances)
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.LeaveType)
                .WithMany()
                .HasForeignKey(b => b.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.HasOne(r => r.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Approver)
                .WithMany()
                .HasForeignKey(r => r.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveBalanceAdjustment>(entity =>
        {
            entity.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.LeaveType)
                .WithMany()
                .HasForeignKey(a => a.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AdjustedByEmployee)
                .WithMany()
                .HasForeignKey(a => a.AdjustedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
