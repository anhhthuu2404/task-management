using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Categories;

public class Category : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;          // Tên gốc (Tiếng Việt)
    public string? NameEn { get; set; }                       // Tên tự động dịch (Tiếng Anh)
    public string? Description { get; set; }                  // Mô tả gốc
    public string? DescriptionEn { get; set; }                // Mô tả tự động dịch (Tiếng Anh)
    public bool IsActive { get; set; }
    public string? ColorCode { get; set; }

    public Category() { }

    public Category(Guid id, string name, string? nameEn = null, string? description = null, string? descriptionEn = null, bool isActive = true, string? colorCode = null)
        : base(id)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        NameEn = nameEn;
        Description = description;
        DescriptionEn = descriptionEn;
        IsActive = isActive;
        ColorCode = colorCode;
    }
}