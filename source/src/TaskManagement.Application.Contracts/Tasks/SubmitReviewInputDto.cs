using System.Collections.Generic;

namespace TaskManagement.Tasks;

public class SubmitReviewInputDto
{
    public string? Note { get; set; }
    public List<TaskAttachmentDto>? Attachments { get; set; } = [];
}