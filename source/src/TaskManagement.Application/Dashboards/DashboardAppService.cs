using System.Threading.Tasks;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Response;

namespace TaskManagement.Dashboards
{
    public class DashboardAppService : TaskManagementAppService, IDashboardAppService
    {
        private readonly IDashboardProvider _dashboardProvider;

        public DashboardAppService(IDashboardProvider dashboardProvider)
        {
            _dashboardProvider = dashboardProvider;
        }

        public async Task<DashboardStatisticsDto> GetStatisticsAsync()
        {
            DashboardQueryResponse queryResponse = await _dashboardProvider.GetStatisticsAsync();

            return new DashboardStatisticsDto
            {
                TotalCategories = queryResponse.TotalCategories,
                TotalTags = queryResponse.TotalTags,
                TotalDepartments = queryResponse.TotalDepartments,
                TotalUsers = queryResponse.TotalUsers,
                TotalProjects = queryResponse.TotalProjects,
                TotalTasks = queryResponse.TotalTasks,
                CompletedTasks = queryResponse.CompletedTasks,
                PendingTasks = queryResponse.PendingTasks,
                TasksByStatus = queryResponse.TasksByStatus,
                UsersByDepartment = queryResponse.UsersByDepartment
            };
        }
    }
}