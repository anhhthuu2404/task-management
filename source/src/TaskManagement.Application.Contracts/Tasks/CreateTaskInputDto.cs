using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Content;

namespace TaskManagement.Tasks;

public class CreateTaskInputDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public int? Priority { get; set; } = 1;

    public DateTime? DueDate { get; set; }

    public Guid? AssigneeId { get; set; }

    public IRemoteStreamContent[]? Files { get; set; }
}