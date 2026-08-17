using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Categories;

public class Category : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty; 
    public string? Description { get; set; }          
    public bool IsActive { get; set; }
    public string? ColorCode { get; set; }       

    public Category() { }

    public Category(Guid id, string name, string? description = null, bool isActive = true, string? colorCode = null)
        : base(id)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        Description = description;
        IsActive = isActive;
        ColorCode = colorCode;
    }
}