import { Component, OnInit, AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, NgZone } from '@angular/core'; 
import { CommonModule } from '@angular/common'; 
import { HttpClient } from '@angular/common/http'; 
import { forkJoin, of } from 'rxjs'; 
import { catchError } from 'rxjs/operators'; 
import { BaseChartDirective } from 'ng2-charts'; 
import { Chart, DoughnutController, BarController, CategoryScale, LinearScale, BarElement, ArcElement, Tooltip, Legend } from 'chart.js'; 

Chart.register( 
  DoughnutController, 
  BarController, 
  CategoryScale, 
  LinearScale, 
  BarElement, 
  ArcElement, 
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
  
  taskChartData: any = { labels: [], datasets: [] }; 
  categoryChartData: any = { labels: [], datasets: [] }; 
  tagChartData: any = { labels: [], datasets: [] }; 
  departmentChartData: any = { labels: [], datasets: [] }; 
  userChartData: any = { labels: [], datasets: [] }; 
  roleChartData: any = { labels: [], datasets: [] }; 
  projectChartData: any = { labels: [], datasets: [] }; 

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
    // 1. Ưu tiên tải dữ liệu thống kê cốt lõi trước để hiển thị giao diện ngay lập tức
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

    // 2. Tải các danh mục, thẻ, dữ liệu phụ ở luồng độc lập phía sau để tránh nghẽn mạng và tải lâu
    forkJoin({
      categories: this.httpClient.get<any>('/api/app/category?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      tags: this.httpClient.get<any>('/api/app/tag?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      departments: this.httpClient.get<any>('/api/app/department?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      users: this.httpClient.get<any>('/api/identity/users?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      roles: this.httpClient.get<any>('/api/identity/roles?maxResultCount=100').pipe(catchError(() => of({ items: [] }))),
      projects: this.httpClient.get<any>('/api/app/project?maxResultCount=100').pipe(catchError(() => of({ items: [] })))
    }).subscribe({
      next: (res: any) => {
        const catItems = res.categories?.items || res.categories || [];
        this.categoryChartData = {
          labels: catItems.map((x: any) => x.name ?? ''),
          datasets: [{ data: catItems.map(() => 1), label: 'Danh mục', backgroundColor: '#c5d3e8' }]
        };

        const tagItems = res.tags?.items || res.tags || [];
        this.tagChartData = {
          labels: tagItems.map((x: any) => x.name ?? ''),
          datasets: [{ data: tagItems.map(() => 1), label: 'Thẻ Tag', backgroundColor: '#b7efc5' }]
        };

        const deptRes = res.departments;
        const allDepts = Array.isArray(deptRes) ? deptRes : (deptRes?.items || deptRes?.result || []);
        this.departmentChartData = {
          labels: allDepts.map((d: any) => (d.displayName || d.name) ?? ''),
          datasets: [{ data: allDepts.map(() => 0), label: 'Số lượng nhân sự', backgroundColor: '#fcf6bd' }]
        };

        const users = res.users?.items || res.users?.result || [];
        this.userChartData = {
          labels: users.map((u: any) => u.userName ?? ''),
          datasets: [{ data: users.map(() => 1), label: 'Người dùng', backgroundColor: '#ffc8dd' }]
        };

        const roles = res.roles?.items || res.roles?.result || [];
        this.roleChartData = {
          labels: roles.map((r: any) => r.name ?? ''),
          datasets: [{ data: roles.map(() => 1), label: 'Vai trò', backgroundColor: '#e2ece9' }]
        };

        const projects = res.projects?.items || res.projects || [];
        this.projectChartData = {
          labels: projects.map((p: any) => p.name ?? ''),
          datasets: [{ data: projects.map(() => 1), label: 'Dự án', backgroundColor: '#b5e2fa' }]
        };

        this.cdr.markForCheck();
        
        setTimeout(() => {
          window.dispatchEvent(new Event('resize'));
        }, 100);
      }
    });
  } 
}