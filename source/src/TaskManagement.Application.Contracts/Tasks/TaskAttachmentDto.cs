using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskAttachmentDto : EntityDto<Guid>
{
    public Guid TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
}