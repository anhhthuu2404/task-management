import { Component, OnInit, AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BaseChartDirective } from 'ng2-charts';
import {
  Chart,
  DoughnutController,
  BarController,
  LineController,
  PieController,
  PolarAreaController,
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  RadialLinearScale,
  Tooltip,
  Legend
} from 'chart.js';

Chart.register(
  DoughnutController,
  BarController,
  LineController,
  PieController,
  PolarAreaController,
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  RadialLinearScale,
  Tooltip,
  Legend
);

import { DashboardService, DashboardStatisticsDto } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, BaseChartDirective]
})
export class DashboardComponent implements OnInit, AfterViewInit {
  isLoading = false;
  stats: DashboardStatisticsDto | null = null;
  selectedFilter: string = 'all';

  totalTasks = 0;
  completedTasks = 0;
  inProgressTasks = 0;
  overdueTasks = 0;

  taskChartData: any = { labels: [], datasets: [] };
  completedOverdueChartData: any = { labels: [], datasets: [] }; 
  categoryChartData: any = { labels: [], datasets: [] };
  tagChartData: any = { labels: [], datasets: [] };
  departmentChartData: any = { labels: [], datasets: [] };
  userChartData: any = { labels: [], datasets: [] };
  roleChartData: any = { labels: [], datasets: [] };
  projectChartData: any = { labels: [], datasets: [] };

  // Cấu hình chung tùy chỉnh Tooltip & Legend chuyên nghiệp cho Chart.js
  chartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
      },
      tooltip: {
        callbacks: {
          label: function(context: any) {
            let label = context.dataset.label || '';
            if (label) {
              label += ': ';
            }
            const value = context.parsed.y !== undefined ? context.parsed.y : context.parsed;
            return label + value;
          }
        }
      }
    }
  };

  constructor(
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone,
    private httpClient: HttpClient,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      window.dispatchEvent(new Event('resize'));
    }, 200);
  }

  onSelectFilter(filterType: string, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.ngZone.run(() => {
      this.selectedFilter = filterType;
      this.cdr.markForCheck();
      setTimeout(() => {
        window.dispatchEvent(new Event('resize'));
      }, 50);
    });
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.dashboardService.getStatistics().pipe(
      catchError(() => of(null))
    ).subscribe({
      next: (statsRes: any) => {
        this.stats = statsRes;
        const statusMap = statsRes?.tasksByStatus || {};
        this.taskChartData = {
          labels: Object.keys(statusMap),
          datasets: [{ data: Object.values(statusMap), backgroundColor: ['#ffd166', '#4ea8de', '#52b788', '#ef476f'] }]
        };
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    forkJoin({
      categories: this.httpClient.get<any>('/api/app/category?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      tags: this.httpClient.get<any>('/api/app/tag?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      departments: this.httpClient.get<any>('/api/app/department?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      users: this.httpClient.get<any>('/api/identity/users?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      roles: this.httpClient.get<any>('/api/identity/roles?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      projects: this.httpClient.get<any>('/api/app/project?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      tasks: this.httpClient.get<any>('/api/app/task?maxResultCount=1000').pipe(catchError(() => of({ items: [] })))
    }).subscribe({
      next: (res: any) => {
        const extractArray = (response: any) => {
          if (!response) return [];
          if (Array.isArray(response)) return response;
          if (Array.isArray(response.items)) return response.items;
          if (response.result) {
            if (Array.isArray(response.result)) return response.result;
            if (Array.isArray(response.result.items)) return response.result.items;
          }
          return [];
        };

        const catItems = extractArray(res.categories);
        const tagItems = extractArray(res.tags);
        const allDepts = extractArray(res.departments);
        const users = extractArray(res.users);
        const roles = extractArray(res.roles);
        const projects = extractArray(res.projects);
        const taskItems = extractArray(res.tasks);

        this.totalTasks = taskItems.length;
        
        this.completedTasks = taskItems.filter((t: any) => {
          return t.progressPercent === 100 || t.isCompleted === true || t.status === 2;
        }).length;

        this.inProgressTasks = taskItems.filter((t: any) => {
          return (t.progressPercent > 0 && t.progressPercent < 100) || t.status === 5;
        }).length;

        const today = new Date();
        today.setHours(0, 0, 0, 0); 

        this.overdueTasks = taskItems.filter((t: any) => {
          const dateValue = t.dueDate || t.deadline || t.endTime;
          if (!dateValue) return false;
          
          const dueDate = new Date(dateValue);
          dueDate.setHours(0, 0, 0, 0);

          const isCompleted = t.progressPercent === 100 || t.isCompleted === true || t.status === 2;
          return !isCompleted && dueDate < today;
        }).length;

        this.completedOverdueChartData = {
          labels: ['Công việc hoàn thành', 'Công việc quá hạn'],
          datasets: [{
            data: [this.completedTasks, this.overdueTasks],
            backgroundColor: ['#ffafcc', '#ffadad']
          }]
        };

        this.categoryChartData = {
          labels: catItems.map((x: any) => x.name ?? ''),
          datasets: [{ 
            data: catItems.map((cat: any) => taskItems.filter((t: any) => 
              t.categoryId === cat.id || t.category?.id === cat.id
            ).length), 
            label: 'Số lượng công việc', 
            backgroundColor: '#c5d3e8' 
          }]
        };

        this.tagChartData = {
          labels: tagItems.map((x: any) => x.name ?? ''),
          datasets: [{ 
            data: tagItems.map((tag: any) => taskItems.filter((t: any) => {
              return t.tagId === tag.id || 
                     (t.tagIds && Array.isArray(t.tagIds) && t.tagIds.includes(tag.id)) ||
                     (t.tags && Array.isArray(t.tags) && t.tags.some((item: any) => (item.id || item) === tag.id));
            }).length), 
            label: 'Số lượng công việc', 
            backgroundColor: '#b7efc5' 
          }]
        };

        this.projectChartData = {
          labels: projects.map((p: any) => p.name ?? ''),
          datasets: [{ 
            data: projects.map((proj: any) => taskItems.filter((t: any) => 
              t.projectId === proj.id || t.project?.id === proj.id
            ).length), 
            label: 'Số lượng công việc', 
            backgroundColor: '#b5e2fa' 
          }]
        };

        this.departmentChartData = {
          labels: allDepts.map((d: any) => {
            const parent = allDepts.find((p: any) => p.id === d.parentId);
            const deptName = d.displayName || d.name || '';
            return parent ? `${parent.name} > ${deptName}` : deptName;
          }),
          datasets: [{ 
            data: allDepts.map((d: any) => {
              if (d.members && d.members.length > 0) return d.members.length;
              return users.filter((u: any) => u.departmentId === d.id).length;
            }), 
            label: 'Số lượng nhân sự', 
            backgroundColor: '#fcf6bd' 
          }]
        };

        this.userChartData = {
          labels: users.map((u: any) => u.userName ?? u.name ?? ''),
          datasets: [{ 
            data: users.map((u: any) => taskItems.filter((t: any) => {
              const uId = u.id;
              const uName = (u.userName || u.name || '').toLowerCase();
              return t.assignedUserId === uId || 
                     t.assigneeId === uId || 
                     t.userId === uId || 
                     t.creatorId === uId || 
                     t.assignedUser?.id === uId ||
                     t.assignedUserId === uName ||
                     t.assigneeId === uName ||
                     (t.assigneeName && t.assigneeName.toLowerCase() === uName) ||
                     (t.assignedUserName && t.assignedUserName.toLowerCase() === uName) ||
                     (t.userName && t.userName.toLowerCase() === uName) ||
                     (t.userIds && Array.isArray(t.userIds) && (t.userIds.includes(uId) || t.userIds.includes(uName)));
            }).length), 
            label: 'Công việc tham gia', 
            backgroundColor: '#ffc8dd' 
          }]
        };

        this.roleChartData = {
          labels: roles.map((r: any) => r.name ?? ''),
          datasets: [{ data: roles.map(() => 1), label: 'Vai trò', backgroundColor: '#e2ece9' }]
        };

        this.cdr.markForCheck();
        setTimeout(() => {
          window.dispatchEvent(new Event('resize'));
        }, 100);
      }
    });
  }
}