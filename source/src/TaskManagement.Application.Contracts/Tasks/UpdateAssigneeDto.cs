using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks.Dtos
{
    public class UpdateAssigneeDto
    {
        [Required]
        public Guid TaskId { get; set; }

        public Guid? AssigneeId { get; set; }

        public string? AssigneeName { get; set; }
    }
}