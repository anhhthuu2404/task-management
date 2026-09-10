
export interface DashboardStatisticsDto {
  totalCategories?: number;
  totalTags?: number;
  totalDepartments?: number;
  totalUsers?: number;
  totalProjects?: number;
  totalTasks?: number;
  completedTasks?: number;
  pendingTasks?: number;
  tasksByStatus?: Record<string, number>;
  usersByDepartment?: Record<string, number>;
}
