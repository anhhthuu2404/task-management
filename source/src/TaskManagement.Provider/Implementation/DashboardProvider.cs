using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Response;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Provider.Implementation
{
    public class DashboardProvider : IDashboardProvider, ITransientDependency
    {
        private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;

        public DashboardProvider(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<DashboardQueryResponse> GetStatisticsAsync()
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            var connection = RelationalDatabaseFacadeExtensions.GetDbConnection(dbContext.Database);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var multi = await connection.QueryMultipleAsync(
                "sp_GetDashboardStatistics",
                commandType: CommandType.StoredProcedure
            );

            var kpiStats = await multi.ReadFirstOrDefaultAsync<DashboardKpiInternalDto>() ?? new DashboardKpiInternalDto();

            var statusRows = await multi.ReadAsync<TaskStatusCountInternalDto>();
            // Sử dụng GroupBy để tránh lỗi trùng lặp StatusKey
            var tasksByStatus = statusRows
                .GroupBy(x => x.StatusKey)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var deptRows = await multi.ReadAsync<UserDepartmentCountInternalDto>();
            // Sử dụng GroupBy để tránh lỗi trùng lặp DepartmentName
            var usersByDepartment = deptRows
                .GroupBy(x => x.DepartmentName)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.UserCount));

            return new DashboardQueryResponse
            {
                TotalCategories = kpiStats.TotalCategories,
                TotalTags = kpiStats.TotalTags,
                TotalDepartments = kpiStats.TotalDepartments,
                TotalUsers = kpiStats.TotalUsers,
                TotalProjects = kpiStats.TotalProjects,
                TotalTasks = kpiStats.TotalTasks,
                CompletedTasks = kpiStats.CompletedTasks,
                PendingTasks = kpiStats.PendingTasks,
                TasksByStatus = tasksByStatus,
                UsersByDepartment = usersByDepartment
            };
        }
    }

    internal class DashboardKpiInternalDto
    {
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
    }

    internal class TaskStatusCountInternalDto
    {
        public string StatusKey { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    internal class UserDepartmentCountInternalDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}