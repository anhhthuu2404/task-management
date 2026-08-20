using System;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tags;

public interface ITagAppService :
    ICrudAppService<
        TagDto,
        Guid,
        GetTagListInput,
        CreateUpdateTagDto>
{
}