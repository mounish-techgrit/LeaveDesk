using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;

namespace LeaveDesk.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IRepository<Department> _repository;

    public DepartmentService(IRepository<Department> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var departments = await _repository.GetAllAsync();
        return departments.Select(ToDto);
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var department = await _repository.GetByIdAsync(id);
        return department is null ? null : ToDto(department);
    }

    public async Task<ServiceResult<DepartmentDto>> CreateAsync(DepartmentRequestDto request)
    {
        if (await _repository.ExistsAsync(d => d.Name == request.Name))
        {
            return ServiceResult<DepartmentDto>.Failure("A department with this name already exists.");
        }

        var department = new Department { Name = request.Name };
        await _repository.AddAsync(department);

        return ServiceResult<DepartmentDto>.Success(ToDto(department));
    }

    public async Task<ServiceResult<DepartmentDto>> UpdateAsync(int id, DepartmentRequestDto request)
    {
        var department = await _repository.GetByIdAsync(id);
        if (department is null)
        {
            return ServiceResult<DepartmentDto>.Failure("Department not found.");
        }

        if (await _repository.ExistsAsync(d => d.Name == request.Name && d.Id != id))
        {
            return ServiceResult<DepartmentDto>.Failure("A department with this name already exists.");
        }

        department.Name = request.Name;
        await _repository.UpdateAsync(department);

        return ServiceResult<DepartmentDto>.Success(ToDto(department));
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var department = await _repository.GetByIdAsync(id);
        if (department is null)
        {
            return ServiceResult.Failure("Department not found.");
        }

        try
        {
            await _repository.DeleteAsync(department);
            return ServiceResult.Success();
        }
        catch (RepositoryDeleteConflictException)
        {
            return ServiceResult.Failure("Cannot delete this department because employees are still assigned to it.");
        }
    }

    private static DepartmentDto ToDto(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name
    };
}
