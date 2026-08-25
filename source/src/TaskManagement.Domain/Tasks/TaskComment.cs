using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class TaskComment : CreationAuditedAggregateRoot<Guid>
{
    public Guid TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }

    public virtual ICollection<CommentAttachment> Attachments { get; protected set; } = [];

    public TaskComment() { }

    public TaskComment(Guid id) : base(id) { }

    public void AddAttachment(string fileName, string? fileUrl)
    {
        Attachments ??= [];
        Attachments.Add(new CommentAttachment
        {
            FileName = fileName,
            FileUrl = fileUrl ?? string.Empty
        });
    }
}

public class CommentAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
}