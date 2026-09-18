using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Roles
{
    public class RoleDto : EntityDto<Guid>
    {
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public int EntityVersion { get; set; }
        public DateTime CreationTime { get; set; }
    }

    public class CreateUpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsPublic { get; set; }
    }
}