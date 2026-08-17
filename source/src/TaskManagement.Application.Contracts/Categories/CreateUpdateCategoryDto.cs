using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Categories;

public class CreateUpdateCategoryDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [StringLength(32)]
    public string ColorCode { get; set; } = string.Empty;
}