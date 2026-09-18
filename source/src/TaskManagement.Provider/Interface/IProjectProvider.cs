using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Provider.Request;
using TaskManagement.Provider.Response;

namespace TaskManagement.Provider.Interface
{
    public interface IProjectProvider
    {
        Task<List<ProjectQueryResponse>> GetListAsync(ProjectGetListRequest request);
        Task<ProjectQueryResponse?> GetByIdAsync(Guid id);

        // Bổ sung các hàm Thêm, Sửa, Xóa
        Task CreateAsync(ProjectQueryResponse input);
        Task UpdateAsync(ProjectQueryResponse input);
        Task DeleteAsync(Guid id);
    }
}