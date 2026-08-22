using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class ChecklistItemDto : EntityDto<Guid>
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }

    public class CreateUpdateChecklistItemDto
    {
        [Required]
        [StringLength(256)]
        public string Title { get; set; } = string.Empty;
    }

    
    public class CreateChecklistItemDto : CreateUpdateChecklistItemDto { }
}