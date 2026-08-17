using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tags;

public class TagDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
}