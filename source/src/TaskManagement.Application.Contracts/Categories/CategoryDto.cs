using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Categories;

public class CategoryDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }
}