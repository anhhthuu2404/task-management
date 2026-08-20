using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tags;

public class GetTagListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
}