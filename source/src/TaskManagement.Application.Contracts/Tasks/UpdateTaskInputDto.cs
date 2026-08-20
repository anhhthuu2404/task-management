using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks;

public class UpdateTaskInputDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public int Priority { get; set; } = 1;

    public DateTime? DueDate { get; set; }

    public Guid? AssigneeId { get; set; }
}