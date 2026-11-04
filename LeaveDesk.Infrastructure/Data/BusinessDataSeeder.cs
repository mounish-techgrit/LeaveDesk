using LeaveDesk.Domain.Entities;
using LeaveDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveDesk.Infrastructure.Data;

public static class BusinessDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await context.Departments.AnyAsync())
        {
            return;
        }

        var engineering = new Department { Name = "Engineering" };
        var sales = new Department { Name = "Sales" };
        context.Departments.AddRange(engineering, sales);

        var casual = new LeaveType { Name = "Casual", YearlyQuota = 12, AllowExceedingBalance = false, MaxPastDaysAllowed = 0, MaxCarryForwardDays = 0 };
        var sick = new LeaveType { Name = "Sick", YearlyQuota = 10, AllowExceedingBalance = false, MaxPastDaysAllowed = 3, MaxCarryForwardDays = 0 };
        var earned = new LeaveType { Name = "Earned", YearlyQuota = 15, AllowExceedingBalance = false, MaxPastDaysAllowed = 0, MaxCarryForwardDays = 10 };
        var unpaid = new LeaveType { Name = "Unpaid", YearlyQuota = 0, AllowExceedingBalance = true, MaxPastDaysAllowed = 0, MaxCarryForwardDays = 0 };
        context.LeaveTypes.AddRange(casual, sick, earned, unpaid);

        var year = DateTime.UtcNow.Year;
        context.Holidays.AddRange(
            new Holiday { Date = new DateOnly(year, 1, 1), Name = "New Year's Day" },
            new Holiday { Date = new DateOnly(year, 12, 25), Name = "Christmas Day" });

        await context.SaveChangesAsync();

        var manager1 = await CreateEmployeeWithAccountAsync(userManager, context, "Priya Sharma", "priya.manager@yopmail.com", "Manager@123", engineering.Id, null, "Manager");
        var manager2 = await CreateEmployeeWithAccountAsync(userManager, context, "Rajesh Kumar", "rajesh.manager@yopmail.com", "Manager@123", sales.Id, null, "Manager");

        var employeeNames = new[] { "Asha Patel", "Bala Krishnan", "Chetan Rao", "Divya Nair", "Esha Gupta", "Farid Khan" };
        for (var i = 0; i < employeeNames.Length; i++)
        {
            var department = i < 3 ? engineering : sales;
            var manager = i < 3 ? manager1 : manager2;
            var emailName = employeeNames[i].Split(' ')[0].ToLowerInvariant();

            await CreateEmployeeWithAccountAsync(
                userManager, context, employeeNames[i], $"{emailName}.employee@yopmail.com", "Employee@123",
                department.Id, manager.Id, "Employee");
        }
    }

    private static async Task<Employee> CreateEmployeeWithAccountAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        string fullName,
        string email,
        string password,
        int departmentId,
        int? managerId,
        string role)
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);

        var employee = new Employee
        {
            FullName = fullName,
            Email = email,
            ApplicationUserId = user.Id,
            DepartmentId = departmentId,
            ManagerId = managerId,
            IsActive = true
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return employee;
    }
}
