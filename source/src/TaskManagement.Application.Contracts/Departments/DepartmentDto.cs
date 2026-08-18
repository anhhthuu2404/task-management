using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Departments;

public class DepartmentDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } // Cho phép null đồng bộ với Entity
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
}

public class DepartmentTreeDto : DepartmentDto
{
    public List<DepartmentTreeDto> Children { get; set; } = [];
}

public class AssignUserToDepartmentDto
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsManager { get; set; }
}