using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Departments;

public class Department : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty;       // Mã phòng ban
    public string Name { get; set; } = string.Empty;       // Tên phòng ban
    public string? Description { get; set; }               // Cho phép null
    public Guid? ParentId { get; set; }                    // Phòng ban cấp cha
    public Guid? LeaderId { get; set; }                    // Trưởng phòng
    public bool IsActive { get; set; }                     // Trạng thái hoạt động

    // Constructor không tham số public để Mapperly khởi tạo đối tượng
    public Department() { }

    public Department(
        Guid id,
        string code,
        string name,
        string? description = null,
        Guid? parentId = null,
        Guid? leaderId = null)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        ParentId = parentId;
        LeaderId = leaderId;
        IsActive = true;
    }
}