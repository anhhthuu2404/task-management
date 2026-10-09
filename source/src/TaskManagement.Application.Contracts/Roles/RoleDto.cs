using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Roles
{
    public class RoleDto : EntityDto<Guid>
    {
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameVi { get; set; }
        public string? NameEn { get; set; }
        public string NormalizedName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // Tên hiển thị theo ngôn ngữ hiện tại
        public bool IsDefault { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public int EntityVersion { get; set; }
        public DateTime CreationTime { get; set; }
    }

    public class CreateUpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? NameVi { get; set; }
        public string? NameEn { get; set; }
        public bool IsDefault { get; set; }
        public bool IsPublic { get; set; }
    }
}