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

        var cat1 = new Category(_guidGenerator.Create(), "Phát triển phần mềm", "#007bff");
        cat1.NameEn = "Software Development";
        await _categoryRepository.InsertAsync(cat1, autoSave: true);

        var cat2 = new Category(_guidGenerator.Create(), "Hành chính - Nhân sự", "#28a745");
        cat2.NameEn = "Administration - HR";
        await _categoryRepository.InsertAsync(cat2, autoSave: true);
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _departmentRepository.GetCountAsync() > 0) return;

        var devDept = new Department(_guidGenerator.Create(), "DEV", "Khối Công Nghệ");
        devDept.NameEn = "Technology Division";
        devDept = await _departmentRepository.InsertAsync(devDept, autoSave: true);

        var subDept = new Department(_guidGenerator.Create(), "FE", "Phòng Frontend");
        subDept.NameEn = "Frontend Department";
        subDept.ParentId = devDept.Id;
        await _departmentRepository.InsertAsync(subDept, autoSave: true);
    }

    private async Task SeedTagsAsync()
    {
        if (await _tagRepository.GetCountAsync() > 0) return;

        // Tag chỉ sử dụng các thuộc tính có sẵn (không gọi NameEn để tránh lỗi)
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