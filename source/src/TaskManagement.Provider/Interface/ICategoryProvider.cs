using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Categories;
using TaskManagement.Provider.Request;

namespace TaskManagement.Provider.Interface
{
    public interface ICategoryProvider
    {
        Task<(List<CategoryDto> Items, int TotalCount)> GetPagedListAsync(CategoryGetListRequest request);
        Task<CategoryDto?> GetByIdAsync(Guid id); 
        Task InsertAsync(CategoryDto input);
        Task UpdateAsync(Guid id, CategoryDto input);
        Task DeleteAsync(Guid id, Guid? deleterId = null);
    }
}