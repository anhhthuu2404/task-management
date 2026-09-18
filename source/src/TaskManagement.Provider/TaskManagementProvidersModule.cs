using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Departments;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.Provider;
using TaskManagement.Provider.Implementation;
using TaskManagement.Provider.Interface;
using Volo.Abp.Modularity;

[DependsOn(
    typeof(TaskManagementEntityFrameworkCoreModule)
)]
public class TaskManagementProvidersModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ILanguagesProvider, LanguagesProvider>();
        context.Services.AddTransient<ISysMasterListsProvider, SysMasterListsProvider>();
        context.Services.AddTransient<ITagProvider, TagProvider>();
        context.Services.AddTransient<ICategoryProvider, CategoryProvider>();
        context.Services.AddTransient<IRoleProvider, RoleProvider>();
        context.Services.AddTransient<IDepartmentProvider, DepartmentProvider>();
        context.Services.AddTransient<IUserProvider, UserProvider>();

        // Bổ sung thêm dòng này để đăng ký IProjectProvider vào DI Container
        context.Services.AddTransient<IProjectProvider, ProjectProvider>();
    }
}