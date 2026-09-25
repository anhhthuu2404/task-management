using System.Threading.Tasks;
using TaskManagement.Provider.Response;

namespace TaskManagement.Provider.Interface
{
    public interface IDashboardProvider
    {
        Task<DashboardQueryResponse> GetStatisticsAsync();
    }
}