using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using TaskManagement.Tags;

namespace TaskManagement.Provider.Interface // Đã đổi thành số ít
{
    public interface ITagProvider
    {
        Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input);
        Task<TagDto> GetByIdAsync(Guid id);
        Task CreateAsync(TagDto input, Guid? creatorId);
        Task UpdateAsync(Guid id, CreateUpdateTagDto input, Guid? modifierId);
        Task DeleteAsync(Guid id, Guid? deleterId);
    }
}