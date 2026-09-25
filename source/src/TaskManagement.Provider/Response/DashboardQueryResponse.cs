using System.Collections.Generic;

namespace TaskManagement.Provider.Response
{
    public class DashboardQueryResponse
    {
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        public Dictionary<string, int> TasksByStatus { get; set; } = new();
        public Dictionary<string, int> UsersByDepartment { get; set; } = new();
    }
}