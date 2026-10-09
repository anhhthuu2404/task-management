using System.Threading.Tasks;
using TaskManagement.Tasks;
using TaskManagement.Localization;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;
using GTranslate.Translators;

namespace TaskManagement
{
    [DependsOn(
        typeof(TaskManagementDomainModule),
        typeof(TaskManagementApplicationContractsModule),
        typeof(AbpPermissionManagementApplicationModule),
        typeof(AbpFeatureManagementApplicationModule),
        typeof(AbpIdentityApplicationModule),
        typeof(AbpAccountApplicationModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpSettingManagementApplicationModule),
        typeof(TaskManagementProvidersModule),
        typeof(AbpAspNetCoreSignalRModule)
    )]
    public class TaskManagementApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<DatabaseLocalizationContributor>();

            // Truyền đủ các bộ dịch theo đúng chữ ký constructor của phiên bản GTranslate hiện tại
            context.Services.AddTransient<ITranslator>(provider => new AggregateTranslator(
                new GoogleTranslator(),
                new GoogleTranslator2(),
                new MicrosoftTranslator(),
                new YandexTranslator(),
                new BingTranslator()
            ));
        }

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
        }
    }
}