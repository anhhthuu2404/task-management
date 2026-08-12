using TaskManagement.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using TaskManagement.Provider;

[DependsOn(
    typeof(TaskManagementEntityFrameworkCoreModule)
)]
public class TaskManagementProvidersModule : AbpModule
{
    public override void ConfigureServices(
        ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ILanguagesProvider, LanguagesProvider>();
        context.Services.AddTransient<ISysMasterListsProvider, SysMasterListsProvider>();
    }
}
