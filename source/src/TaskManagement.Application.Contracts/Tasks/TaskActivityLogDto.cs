using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskActivityLogDto : EntityDto<Guid>
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserName { get; set; } 
    public string? Details { get; set; }
    public DateTime CreationTime { get; set; }
}