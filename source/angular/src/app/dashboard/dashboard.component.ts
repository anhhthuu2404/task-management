import { Component, OnInit, AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BaseChartDirective } from 'ng2-charts';
import { CoreModule, LocalizationService } from '@abp/ng.core';
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
  Legend,
  Filler
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
  Legend,
  Filler
);

import { DashboardService, DashboardStatisticsDto } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, BaseChartDirective, CoreModule]
})
export class DashboardComponent implements OnInit, AfterViewInit {
  isLoading = false;
  stats: DashboardStatisticsDto | null = null;
  selectedFilter: string = 'all';
  isToastVisible = false;

  totalTasks = 0;
  completedTasks = 0;
  inProgressTasks = 0;
  overdueTasks = 0;

  // Biến cho phần phân tích theo thời gian (Analysis Line Chart) lấy dữ liệu thực tế
  analysisViewMode: 'month' | 'year' = 'month';
  allLoadedTasks: any[] = [];
  analysisChartData: any = { labels: [], datasets: [] };

  taskChartData: any = { labels: [], datasets: [] };
  completedOverdueChartData: any = { labels: [], datasets: [] }; 
  categoryChartData: any = { labels: [], datasets: [] };
  tagChartData: any = { labels: [], datasets: [] };
  departmentChartData: any = { labels: [], datasets: [] };
  userChartData: any = { labels: [], datasets: [] };
  roleChartData: any = { labels: [], datasets: [] };
  projectChartData: any = { labels: [], datasets: [] };

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

  // Cấu hình riêng cho biểu đồ Line Analysis
  analysisChartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: function(context: any) {
            return ` ${context.parsed.y} công việc`;
          }
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { precision: 0 }
      },
      x: {
        grid: { display: false }
      }
    }
  };

  constructor(
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone,
    private httpClient: HttpClient,
    private dashboardService: DashboardService,
    private localizationService: LocalizationService
  ) {}

  ngOnInit(): void {
    setTimeout(() => {
      this.loadDashboardData();
    });
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

  // Chuyển đổi chế độ xem Tháng / Năm cho biểu đồ Analysis
  setAnalysisViewMode(mode: 'month' | 'year'): void {
    this.analysisViewMode = mode;
    this.updateAnalysisChartData();
    this.cdr.markForCheck();
  }

  // HÀM XỬ LÝ DỮ LIỆU THỰC TẾ CHO BIỂU ĐỒ ANALYSIS (KHÔNG GÁN CỨNG)
  updateAnalysisChartData(): void {
    const currentLang = this.localizationService.currentLang;
    
    if (this.analysisViewMode === 'month') {
      // Lấy thời gian hiện tại (Tháng và Năm thực tế)
      const now = new Date();
      const currentYear = now.getFullYear();
      const currentMonth = now.getMonth(); // 0 - 11

      // Lấy số ngày trong tháng hiện tại (ví dụ: tháng 10 có 31 ngày)
      const daysInCurrentMonth = new Date(currentYear, currentMonth + 1, 0).getDate();
      
      const labels: string[] = [];
      const dataCounts: number[] = new Array(daysInCurrentMonth).fill(0);

      for (let i = 1; i <= daysInCurrentMonth; i++) {
        labels.push(currentLang === 'en' ? `Day ${i}` : `Ngày ${i}`);
      }

      // Đếm số lượng công việc thực tế từ API dựa trên dueDate hoặc creationTime
      this.allLoadedTasks.forEach((t: any) => {
        const dateValue = t.dueDate || t.deadline || t.creationTime || t.startTime;
        if (dateValue) {
          const tDate = new Date(dateValue);
          if (tDate.getFullYear() === currentYear && tDate.getMonth() === currentMonth) {
            const dayOfMonth = tDate.getDate(); // Ngày trong tháng (1 - 31)
            if (dayOfMonth >= 1 && dayOfMonth <= daysInCurrentMonth) {
              dataCounts[dayOfMonth - 1]++;
            }
          }
        }
      });

      this.analysisChartData = {
        labels: labels,
        datasets: [{
          data: dataCounts,
          label: currentLang === 'en' ? 'Task Analysis' : 'Phân tích công việc',
          fill: true,
          borderColor: '#4ea8de',
          backgroundColor: 'rgba(78, 168, 222, 0.15)',
          tension: 0.4,
          pointBackgroundColor: '#4ea8de',
          pointBorderColor: '#fff',
          pointBorderWidth: 2,
          pointRadius: 4
        }]
      };

    } else {
      // Chế độ xem theo Năm (Tháng 1 đến Tháng 12) từ dữ liệu thực tế
      const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
      const monthNamesVi = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];
      
      const monthCounts = new Array(12).fill(0);
      const currentYear = new Date().getFullYear();

      this.allLoadedTasks.forEach((t: any) => {
        const dateValue = t.dueDate || t.deadline || t.creationTime || t.startTime;
        if (dateValue) {
          const tDate = new Date(dateValue);
          if (tDate.getFullYear() === currentYear) {
            const mIndex = tDate.getMonth();
            if (mIndex >= 0 && mIndex < 12) {
              monthCounts[mIndex]++;
            }
          }
        }
      });

      this.analysisChartData = {
        labels: currentLang === 'en' ? months : monthNamesVi,
        datasets: [{
          data: monthCounts,
          label: currentLang === 'en' ? 'Task Analysis' : 'Phân tích công việc',
          fill: true,
          borderColor: '#4ea8de',
          backgroundColor: 'rgba(78, 168, 222, 0.15)',
          tension: 0.4,
          pointBackgroundColor: '#4ea8de',
          pointBorderColor: '#fff',
          pointBorderWidth: 2,
          pointRadius: 5
        }]
      };
    }
  }

  loadDashboardData(): void {
    requestAnimationFrame(() => {
      this.isToastVisible = true;
      this.cdr.markForCheck();
      
      setTimeout(() => {
        this.isToastVisible = false;
        this.cdr.markForCheck();
      }, 3000);
    });

    const currentLang = this.localizationService.currentLang;

    this.dashboardService.getStatistics().pipe(
      catchError(() => of(null))
    ).subscribe({
      next: (statsRes: any) => {
        this.stats = statsRes;
        const statusMap = statsRes?.tasksByStatus || {};
        
        const statusNameMapping: { [key: string]: string } = {
          '0': currentLang === 'en' ? 'Pending' : 'Chờ xử lý',
          '1': currentLang === 'en' ? 'In Progress' : 'Đang thực hiện',
          '2': currentLang === 'en' ? 'Completed' : 'Đã hoàn thành',
          '3': currentLang === 'en' ? 'Cancelled' : 'Đã hủy',
          '5': currentLang === 'en' ? 'Overdue' : 'Quá hạn'
        };

        const rawKeys = Object.keys(statusMap);
        const mappedLabels = rawKeys.map(key => statusNameMapping[key] || `Status ${key}`);
        const rawValues = Object.values(statusMap);

        const statusColors = rawKeys.map(key => {
          switch (key) {
            case '0': return '#ffd166';
            case '1': return '#4ea8de';
            case '2': return '#52b788';
            case '3': return '#ef476f';
            case '5': return '#adb5bd';
            default: return '#cccccc';
          }
        });

        this.taskChartData = {
          labels: mappedLabels,
          datasets: [{ 
            data: rawValues, 
            backgroundColor: statusColors 
          }]
        };

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    // Lấy đồng thời tất cả dữ liệu thực tế từ các API của ABP Framework
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

        // Lưu danh sách task thực tế để vẽ biểu đồ Analysis
        this.allLoadedTasks = taskItems; 
        this.updateAnalysisChartData();  

        this.totalTasks = taskItems.length;
        
        this.completedTasks = taskItems.filter((t: any) => {
          return t.status === 2 || t.progressPercent === 100 || t.isCompleted === true;
        }).length;

        this.inProgressTasks = taskItems.filter((t: any) => {
          return t.status === 1 || (t.progressPercent > 0 && t.progressPercent < 100);
        }).length;

        const today = new Date();
        today.setHours(0, 0, 0, 0); 

        this.overdueTasks = taskItems.filter((t: any) => {
          if (t.status === 5) return true;
          const dateValue = t.dueDate || t.deadline || t.endTime;
          if (!dateValue) return false;
          
          const dueDate = new Date(dateValue);
          dueDate.setHours(0, 0, 0, 0);

          const isCompleted = t.status === 2 || t.progressPercent === 100 || t.isCompleted === true;
          return !isCompleted && dueDate.getTime() < today.getTime();
        }).length;

        this.completedOverdueChartData = {
          labels: [
            currentLang === 'en' ? 'Completed Tasks' : 'Công việc hoàn thành', 
            currentLang === 'en' ? 'Overdue Tasks' : 'Công việc quá hạn'
          ],
          datasets: [{
            data: [this.completedTasks, this.overdueTasks],
            backgroundColor: ['#ffafcc', '#ffadad']
          }]
        };

        // Danh mục thực tế
        this.categoryChartData = {
          labels: catItems.map((x: any) => (currentLang === 'en' && x.nameEn) ? x.nameEn : (x.name ?? '')),
          datasets: [{ 
            data: catItems.map((cat: any) => taskItems.filter((t: any) => 
              t.categoryId === cat.id || t.category?.id === cat.id
            ).length), 
            label: currentLang === 'en' ? 'Task Count' : 'Số lượng công việc', 
            backgroundColor: '#c5d3e8' 
          }]
        };

        // Thẻ thực tế
        this.tagChartData = {
          labels: catItems.map((cat: any) => (currentLang === 'en' && cat.nameEn) ? cat.nameEn : (cat.name ?? '')),
          datasets: [{ 
            data: catItems.map((cat: any) => {
              return tagItems.filter((tag: any) => 
                tag.categoryId === cat.id || tag.category?.id === cat.id
              ).length;
            }), 
            label: currentLang === 'en' ? 'Tag Count' : 'Số lượng danh mục', 
            backgroundColor: '#b7efc5' 
          }]
        };

        // Dự án thực tế
        this.projectChartData = {
          labels: projects.map((p: any) => (currentLang === 'en' && p.nameEn) ? p.nameEn : (p.name ?? '')),
          datasets: [{ 
            data: projects.map((proj: any) => taskItems.filter((t: any) => 
              t.projectId === proj.id || t.project?.id === proj.id
            ).length), 
            label: currentLang === 'en' ? 'Task Count' : 'Số lượng công việc', 
            backgroundColor: '#b5e2fa' 
          }]
        };

        // Phòng ban thực tế
        this.departmentChartData = {
          labels: allDepts.map((d: any) => {
            const parent = allDepts.find((p: any) => p.id === d.parentId);
            const deptName = (currentLang === 'en' && d.nameEn) ? d.nameEn : (d.displayName || d.name || '');
            const parentName = parent ? ((currentLang === 'en' && parent.nameEn) ? parent.nameEn : parent.name) : '';
            return parent ? `${parentName} > ${deptName}` : deptName;
          }),
          datasets: [{ 
            data: allDepts.map((d: any) => {
              if (d.members && d.members.length > 0) return d.members.length;
              return users.filter((u: any) => u.departmentId === d.id).length;
            }), 
            label: currentLang === 'en' ? 'Personnel Count' : 'Số lượng nhân sự', 
            backgroundColor: '#fcf6bd' 
          }]
        };

        // Người dùng thực tế
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
            label: currentLang === 'en' ? 'Participating Tasks' : 'Công việc tham gia', 
            backgroundColor: '#ffc8dd' 
          }]
        };

        // Vai trò thực tế
        this.roleChartData = {
          labels: roles.map((r: any) => r.name ?? ''),
          datasets: [{ data: roles.map(() => 1), label: 'Role', backgroundColor: '#e2ece9' }]
        };

        this.cdr.markForCheck();
        setTimeout(() => {
          window.dispatchEvent(new Event('resize'));
        }, 100);
      }
    });
  }
}