using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Tasks;

namespace TaskManagement.Tasks;

public class UpdateTaskInputDto
{
    [Required]
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public Guid? AssigneeId { get; set; }

    [Required]
    public int Priority { get; set; }

    [Required]
    public int Status { get; set; }

    public DateTime? DueDate { get; set; }

    public List<TaskAttachmentDto>? Attachments { get; set; }

    // === BỔ SUNG CÁC TRƯỜNG LẶP LẠI ĐỂ HỨNG DỮ LIỆU TỪ FRONTEND ===
    public bool IsRecurring { get; set; } = false;
    public RecurrenceFrequency? Frequency { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
}