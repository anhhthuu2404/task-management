using System;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagement.Categories;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
namespace TaskManagement.Tags;

public class Tag : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public bool IsActive { get; set; }

    public Tag() { }

    public Tag(Guid id, string name, string? colorCode = null, bool isActive = true) : base(id)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 64);
        ColorCode = colorCode;
        IsActive = isActive;
    }
    public Guid? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; }
}