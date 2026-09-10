import { Injectable } from '@angular/core'; 
import { RestService } from '@abp/ng.core'; 
import { Observable } from 'rxjs'; 

export interface DashboardStatisticsDto { 
  totalCategories: number; 
  totalTags: number; 
  totalDepartments: number; 
  totalUsers: number; 
  totalProjects: number; 
  totalTasks: number; 
  completedTasks: number; 
  pendingTasks: number; 
  tasksByStatus: Record<string, number>; 
  usersByDepartment: Record<string, number>; 
} 

@Injectable({ 
  providedIn: 'root', 
}) 
export class DashboardService { 
  constructor(private restService: RestService) {} 

  getStatistics(): Observable<DashboardStatisticsDto> { 
    return this.restService.request<void, DashboardStatisticsDto>({ 
      method: 'GET', 
      url: '/api/app/dashboard/statistics', 
    }); 
  } 
}
