using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities; // Thêm namespace này
using TaskManagement.Tasks;

namespace TaskManagement.Tasks;

public class UpdateTaskInputDto : IHasConcurrencyStamp // Implement interface này
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
    public string? TitleEn { get; set; }
    public string? DescriptionEn { get; set; }
    public string? AssigneeUserName { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? MilestoneId { get; set; }

    public List<TaskAttachmentDto>? Attachments { get; set; }

    public Guid? ProjectId { get; set; }

    public bool IsRecurring { get; set; } = false;
    public RecurrenceFrequency? Frequency { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public string? AssigneeName { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public int ProgressPercent { get; set; }

    // === BỔ SUNG TRƯỜNG NÀY ĐỂ KHẮC PHỤC LỖI CONCURRENCY ===
    public string? ConcurrencyStamp { get; set; }
}