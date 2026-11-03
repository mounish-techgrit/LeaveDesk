using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IUserAccountService _userAccountService;
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IRepository<Employee> employeeRepository,
        IRepository<Department> departmentRepository,
        IUserAccountService userAccountService,
        ILeaveRequestService leaveRequestService,
        IEmailService emailService,
        ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userAccountService = userAccountService;
        _leaveRequestService = leaveRequestService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = (await _employeeRepository.GetAllAsync()).ToList();
        var departments = (await _departmentRepository.GetAllAsync()).ToDictionary(d => d.Id);
        var employeesById = employees.ToDictionary(e => e.Id);

        var dtos = new List<EmployeeDto>();
        foreach (var employee in employees)
        {
            var roles = await _userAccountService.GetRolesAsync(employee.ApplicationUserId);
            departments.TryGetValue(employee.DepartmentId, out var department);
            Employee? manager = employee.ManagerId.HasValue && employeesById.TryGetValue(employee.ManagerId.Value, out var m) ? m : null;

            dtos.Add(ToDto(employee, department, manager, roles));
        }

        return dtos;
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        return employee is null ? null : await ToDtoAsync(employee);
    }

    public async Task<EmployeeDto?> GetByApplicationUserIdAsync(string applicationUserId)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        return employee is null ? null : await ToDtoAsync(employee);
    }

    public async Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeRequestDto request)
    {
        var applicationUserId = await _userAccountService.FindUserIdByEmailAsync(request.Email);
        if (applicationUserId is null)
        {
            return ServiceResult<EmployeeDto>.Failure("No registered account found for this email. Ask the employee to register first.");
        }

        if (await _employeeRepository.ExistsAsync(e => e.ApplicationUserId == applicationUserId))
        {
            return ServiceResult<EmployeeDto>.Failure("This account is already linked to an employee profile.");
        }

        if (await _employeeRepository.ExistsAsync(e => e.Email == request.Email))
        {
            return ServiceResult<EmployeeDto>.Failure("An employee with this email already exists.");
        }

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Department not found.");
        }

        if (request.ManagerId.HasValue && await _employeeRepository.GetByIdAsync(request.ManagerId.Value) is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Manager not found.");
        }

        var employee = new Employee
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            ApplicationUserId = applicationUserId,
            IsActive = true
        };

        await _employeeRepository.AddAsync(employee);

        await _emailService.SendAsync(
            employee.Email,
            "Welcome to LeaveDesk",
            $"Hi {employee.FullName}, your employee profile has been set up by HR. You can now log in with the account you registered.");

        _logger.LogInformation("Employee {EmployeeId} ({Email}) onboarded by HR.", employee.Id, employee.Email);

        return ServiceResult<EmployeeDto>.Success(await ToDtoAsync(employee));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequestDto request)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Employee not found.");
        }

        if (request.ManagerId == id)
        {
            return ServiceResult<EmployeeDto>.Failure("An employee cannot be their own manager.");
        }

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Department not found.");
        }

        if (request.ManagerId.HasValue && await _employeeRepository.GetByIdAsync(request.ManagerId.Value) is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Manager not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
            await _employeeRepository.ExistsAsync(e => e.PhoneNumber == request.PhoneNumber && e.Id != id))
        {
            return ServiceResult<EmployeeDto>.Failure("Another employee already uses this phone number.");
        }

        var wasActive = employee.IsActive;

        employee.FullName = request.FullName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.IsActive = request.IsActive;

        await _employeeRepository.UpdateAsync(employee);

        if (wasActive && !request.IsActive)
        {
            await _leaveRequestService.CancelAllPendingForEmployeeAsync(employee.Id);
            _logger.LogWarning("Employee {EmployeeId} ({Email}) deactivated; pending leave requests auto-cancelled.", employee.Id, employee.Email);
        }

        return ServiceResult<EmployeeDto>.Success(await ToDtoAsync(employee));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateMyProfileAsync(string applicationUserId, UpdateMyProfileRequestDto request)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return ServiceResult<EmployeeDto>.Failure("Your account is not yet linked to an employee profile. Contact HR.");
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
            await _employeeRepository.ExistsAsync(e => e.PhoneNumber == request.PhoneNumber && e.Id != employee.Id))
        {
            return ServiceResult<EmployeeDto>.Failure("Another employee already uses this phone number.");
        }

        employee.FullName = request.FullName;
        employee.PhoneNumber = request.PhoneNumber;

        await _employeeRepository.UpdateAsync(employee);

        return ServiceResult<EmployeeDto>.Success(await ToDtoAsync(employee));
    }

    public async Task<ServiceResult> AssignRoleAsync(int id, string role)
    {
        if (!Enum.TryParse<EmployeeRole>(role, ignoreCase: true, out _))
        {
            return ServiceResult.Failure("Role must be one of: Employee, Manager, HRAdmin.");
        }

        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is null)
        {
            return ServiceResult.Failure("Employee not found.");
        }

        return await _userAccountService.SetRoleAsync(employee.ApplicationUserId, role);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is null)
        {
            return ServiceResult.Failure("Employee not found.");
        }

        try
        {
            await _employeeRepository.DeleteAsync(employee);
            return ServiceResult.Success();
        }
        catch (RepositoryDeleteConflictException)
        {
            return ServiceResult.Failure("Cannot delete this employee because related records still reference them. Consider deactivating instead.");
        }
    }

    private async Task<EmployeeDto> ToDtoAsync(Employee employee)
    {
        var department = await _departmentRepository.GetByIdAsync(employee.DepartmentId);
        var manager = employee.ManagerId.HasValue ? await _employeeRepository.GetByIdAsync(employee.ManagerId.Value) : null;
        var roles = await _userAccountService.GetRolesAsync(employee.ApplicationUserId);

        return ToDto(employee, department, manager, roles);
    }

    private static EmployeeDto ToDto(Employee employee, Department? department, Employee? manager, IList<string> roles) => new()
    {
        Id = employee.Id,
        FullName = employee.FullName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        IsActive = employee.IsActive,
        DepartmentId = employee.DepartmentId,
        DepartmentName = department?.Name ?? string.Empty,
        ManagerId = employee.ManagerId,
        ManagerName = manager?.FullName,
        Roles = roles
    };
}
