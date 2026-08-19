using System;

namespace TaskManagement.Departments;

public class RemoveUserFromDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
}