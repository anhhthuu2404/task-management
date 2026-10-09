using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using TaskManagement.Tags;

namespace TaskManagement.Provider.Interface
{
    public interface ITagProvider
    {
        Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input, string culture = "vi");
        Task<TagDto> GetByIdAsync(Guid id, string culture = "vi");
        Task CreateAsync(TagDto input, Guid? creatorId);
        Task UpdateAsync(Guid id, CreateUpdateTagDto input, Guid? modifierId);
        Task DeleteAsync(Guid id, Guid? deleterId);
    }
}