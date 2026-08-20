using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tags;

public class CreateUpdateTagDto
{
    [Required]
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [StringLength(32)]
    public string? ColorCode { get; set; }

    public Guid? CategoryId { get; set; } // Bắt buộc có để lưu vào DB
}