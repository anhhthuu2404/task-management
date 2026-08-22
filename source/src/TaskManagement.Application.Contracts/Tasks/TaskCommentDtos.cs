using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class CommentAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string? FileContent { get; set; } 
    public string? FileUrl { get; set; }    
}


public class TaskCommentDto : EntityDto<Guid>
{
    public Guid TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }

    public Guid? CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }

    public List<CommentAttachmentDto> Attachments { get; set; } = [];
}


public class CreateTaskCommentDto
{
    public string Text { get; set; } = string.Empty;

   
    public string? FileName { get; set; }
    public string? FileContent { get; set; }

    public List<CommentAttachmentDto> Attachments { get; set; } = [];
}

public class UpdateTaskCommentDto
{
    public string Text { get; set; } = string.Empty;
    public List<CommentAttachmentDto> Attachments { get; set; } = [];
}