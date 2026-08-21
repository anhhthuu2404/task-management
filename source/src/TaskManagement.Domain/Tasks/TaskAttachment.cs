using System;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Tasks;

public class TaskAttachment : Entity<Guid>
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid TaskItemId { get; set; }

    // Constructor mặc định cho EF Core & AutoMapper
    public TaskAttachment()
    {
    }

    public TaskAttachment(Guid id) : base(id)
    {
    }
}