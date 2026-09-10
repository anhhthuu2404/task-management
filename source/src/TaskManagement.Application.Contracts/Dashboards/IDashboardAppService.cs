using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Dashboards
{
    public interface IDashboardAppService : IApplicationService
    {
        Task<DashboardStatisticsDto> GetStatisticsAsync();
    }
}