using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Departments;

public class Department : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty;       // Mã phòng ban
    public string Name { get; set; } = string.Empty;       // Tên phòng ban (Tiếng Việt)
    public string? NameEn { get; set; }                    // Tên phòng ban tiếng Anh (Tự động dịch)
    public string? Description { get; set; }               // Mô tả gốc
    public string? DescriptionEn { get; set; }             // Mô tả tiếng Anh (Tự động dịch)
    public Guid? ParentId { get; set; }                    // Phòng ban cấp cha
    public Guid? LeaderId { get; set; }                    // Trưởng phòng
    public bool IsActive { get; set; }                     // Trạng thái hoạt động

    // Constructor không tham số public để Mapperly khởi tạo đối tượng
    public Department() { }

    public Department(
        Guid id,
        string code,
        string name,
        string? nameEn = null,
        string? description = null,
        string? descriptionEn = null,
        Guid? parentId = null,
        Guid? leaderId = null)
        : base(id)
    {
        Code = code;
        Name = name;
        NameEn = nameEn;
        Description = description;
        DescriptionEn = descriptionEn;
        ParentId = parentId;
        LeaderId = leaderId;
        IsActive = true;
    }
}