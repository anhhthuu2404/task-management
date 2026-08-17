using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Categories;

public class CategoryDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ColorCode { get; set; } = string.Empty;
}