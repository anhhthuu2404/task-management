using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Departments;

public class Department : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty; // Khởi tạo chuỗi rỗng
    public string Name { get; set; } = string.Empty; // Khởi tạo chuỗi rỗng
    public string? Description { get; set; }         // Thêm ? vì có thể null
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }

    public Department() { }

    // Bổ sung thêm tham số description vào Constructor
    public Department(Guid id, string code, string name, string? description = null, Guid? parentId = null, bool isActive = true)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 32);
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        Description = description;
        ParentId = parentId;
        IsActive = isActive;
    }
}