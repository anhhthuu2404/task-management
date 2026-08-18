using Volo.Abp.Application.Dtos;

namespace TaskManagement.Departments;

public class GetDepartmentListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}