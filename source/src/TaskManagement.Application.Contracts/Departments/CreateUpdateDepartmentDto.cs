using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Departments;

public class CreateUpdateDepartmentDto
{
    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? ParentId { get; set; } 

    public bool IsActive { get; set; } = true;
}