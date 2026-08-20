using System;
using System.Threading.Tasks;
using TaskManagement.Categories;
using TaskManagement.Departments;
using TaskManagement.Tags;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace TaskManagement;

public class TaskManagementDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Tag, Guid> _tagRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IGuidGenerator _guidGenerator;

    public TaskManagementDataSeedContributor(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Tag, Guid> tagRepository,
        IRepository<Department, Guid> departmentRepository,
        IGuidGenerator guidGenerator)
    {
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _departmentRepository = departmentRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedCategoriesAsync();
        await SeedDepartmentsAsync();
        await SeedTagsAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _categoryRepository.GetCountAsync() > 0) return;

        await _categoryRepository.InsertAsync(
            new Category(_guidGenerator.Create(), "Phát triển phần mềm", "#007bff", false, null),
            autoSave: true
        );
        await _categoryRepository.InsertAsync(
            new Category(_guidGenerator.Create(), "Hành chính - Nhân sự", "#28a745", false, null),
            autoSave: true
        );
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _departmentRepository.GetCountAsync() > 0) return;

        var devDept = await _departmentRepository.InsertAsync(
            new Department(_guidGenerator.Create(), "DEV", "Khối Công Nghệ", null, null, null),
            autoSave: true
        );

        await _departmentRepository.InsertAsync(
            new Department(_guidGenerator.Create(), "FE", "Phòng Frontend", null, devDept.Id, null),
            autoSave: true
        );
    }

    private async Task SeedTagsAsync()
    {
        if (await _tagRepository.GetCountAsync() > 0) return;

        await _tagRepository.InsertAsync(
            new Tag(_guidGenerator.Create(), "Ưu tiên cao"),
            autoSave: true
        );
        await _tagRepository.InsertAsync(
            new Tag(_guidGenerator.Create(), "Báo lỗi (Bug)"),
            autoSave: true
        );
    }
}