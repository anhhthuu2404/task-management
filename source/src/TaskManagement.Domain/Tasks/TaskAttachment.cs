using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

[Table("TaskAttachments")]
public class TaskAttachment : CreationAuditedEntity<Guid>
{
    public Guid TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }

    protected TaskAttachment() { }

    public TaskAttachment(Guid id, Guid taskId, string fileName, string filePath, long fileSize) : base(id)
    {
        TaskId = taskId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
    }
}