using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Categories;

public class CategoryDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionVi { get; set; } // Bổ sung
    public string? DescriptionEn { get; set; }
    public string? ColorCode { get; set; }
}