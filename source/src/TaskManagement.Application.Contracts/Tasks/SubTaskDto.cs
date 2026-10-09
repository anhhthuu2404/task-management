using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class SubTaskDto : EntityDto<Guid>
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? TitleEn { get; set; } // <--- Thêm trường lưu tiêu đề tiếng Anh
        public bool IsCompleted { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
    }

    public class CreateUpdateSubTaskDto
    {
        [Required]
        [StringLength(256)]
        public string Title { get; set; } = string.Empty;

        public string? TitleEn { get; set; } // <--- Thêm vào đây để đồng bộ khi tạo/sửa

        public Guid? AssigneeId { get; set; }
    }
}