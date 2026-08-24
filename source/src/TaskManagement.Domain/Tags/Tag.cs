using System;
using System.ComponentModel.DataAnnotations.Schema;
using Riok.Mapperly.Abstractions; 
using TaskManagement.Categories;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tags;

public class Tag : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CategoryId { get; set; }
    public string? SubmissionNote { get; set; }
    public string? SubmissionFilesJson { get; set; }

    [MapperConstructor] 
    public Tag() { }

    public Tag(Guid id, string name, string? colorCode = null, Guid? categoryId = null)
        : base(id)
    {
        Name = name;
        ColorCode = colorCode;
        CategoryId = categoryId;
    }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
}