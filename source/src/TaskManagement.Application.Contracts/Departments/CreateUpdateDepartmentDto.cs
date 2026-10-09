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
    public string Name { get; set; } = string.Empty; // 1 ô nhập trên giao diện

    // Bổ sung để lưu song ngữ ngầm bên dưới
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }

    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
}