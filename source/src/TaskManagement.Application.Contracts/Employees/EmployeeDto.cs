using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Employees;

public class EmployeeDto : EntityDto<Guid>
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? DepartmentId { get; set; }
}