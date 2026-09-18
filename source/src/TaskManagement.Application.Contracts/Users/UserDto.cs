using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Users // Hoặc namespace Contracts tương đương của bạn
{
    public class UserDto : EntityDto<Guid>
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreationTime { get; set; }
    }
}