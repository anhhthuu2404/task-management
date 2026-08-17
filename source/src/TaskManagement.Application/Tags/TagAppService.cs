using System;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tags;

public class TagAppService :
    CrudAppService<
        Tag,
        TagDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateTagDto>,
    ITagAppService
{
    public TagAppService(IRepository<Tag, Guid> repository)
        : base(repository)
    {
        GetPolicyName = TaskManagementPermissions.Tags.Default;
        GetListPolicyName = TaskManagementPermissions.Tags.Default;
        CreatePolicyName = TaskManagementPermissions.Tags.Create;
        UpdatePolicyName = TaskManagementPermissions.Tags.Edit;
        DeletePolicyName = TaskManagementPermissions.Tags.Delete;
    }
}