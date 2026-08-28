import { Component, OnInit, OnDestroy, inject, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { RestService, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { NgbPaginationModule, NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { DragDropModule, CdkDragDrop, transferArrayItem, moveItemInArray } from '@angular/cdk/drag-drop';
import { NotificationService } from '../shared/services/notification.service';
import { Subscription } from 'rxjs';

export interface TaskDto {
  id: string;
  title: string;
  description?: string;
  assigneeId?: string;
  assigneeName?: string;
  priority: number;
  status: number;
  progressPercent: number;
  dueDate: string;
  isRecurring?: boolean;
  frequency?: number;
  fileUrl?: string;
  fileName?: string;
  attachments?: any[];
}

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NgbPaginationModule,
    NgbDropdownModule,
    DragDropModule
  ],
  templateUrl: './task-list.component.html'
})
export class TaskListComponent implements OnInit, OnDestroy {
  private readonly rest = inject(RestService);
  private readonly permission = inject(PermissionService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  private readonly toaster = inject(ToasterService);
  private readonly zone = inject(NgZone);

  private notificationSub?: Subscription;

  readonly backendUrl = 'https://localhost:44399';

  taskList: TaskDto[] = [];
  totalCount = 0;
  isLoading = false;

  currentView: 'list' | 'kanban' | 'calendar' = 'list';

  readonly kanbanColumns = [
    { status: 0, title: 'Mới', headerClass: 'border-secondary text-secondary' },
    { status: 1, title: 'Đang làm', headerClass: 'border-primary text-primary' },
    { status: 2, title: 'Hoàn thành', headerClass: 'border-success text-success' },
    { status: 3, title: 'Đã hủy', headerClass: 'border-danger text-danger' },
    { status: 5, title: 'Quá hạn', headerClass: 'border-danger text-danger' }
  ];

  kanbanNewTasks: TaskDto[] = [];
  kanbanInProgressTasks: TaskDto[] = [];
  kanbanCompletedTasks: TaskDto[] = [];
  kanbanCancelledTasks: TaskDto[] = [];
  kanbanOverdueTasks: TaskDto[] = [];

  calendarDate: Date = new Date();
  calendarWeeks: any[][] = [];
  currentCalendarMonthName: string = '';
  currentCalendarYear: number = 0;

  notifications: { message: string; time: Date }[] = [];

  filters = {
    filter: '', 
    categoryId: '',
    assigneeId: '',
    priority: null as number | null,
    status: null as number | null,
    onlyMyTasks: false,
    skipCount: 0,
    maxResultCount: 10,
    sorting: 'CreationTime DESC'
  };

  page = 1;
  categories: any[] = [];
  users: any[] = [];

  readonly canCreate = this.permission.getGrantedPolicy('TaskManagement.Tasks.Create');
  readonly canEdit = this.permission.getGrantedPolicy('TaskManagement.Tasks.Edit');
  readonly canDelete = this.permission.getGrantedPolicy('TaskManagement.Tasks.Delete');

  ngOnInit(): void {
    this.loadCategories();
    this.loadUsers();
    this.fetchTasks();

    this.notificationSub = this.notificationService.notifications$.subscribe(incomingNotifications => {
      this.zone.run(() => {
        const newItems = incomingNotifications || [];
        if (newItems.length > this.notifications.length && newItems.length > 0) {
          const latest = newItems[0];
          if (latest && latest.message) {
            this.toaster.info(latest.message, 'Thông báo hệ thống mới');
            this.fetchTasks();
          }
        }
        this.notifications = newItems;
      });
    });
  }

  ngOnDestroy(): void {
    if (this.notificationSub) {
      this.notificationSub.unsubscribe();
    }
  }

  clearNotifications(): void {
    this.notifications = [];
  }

  // Đã bổ sung hàm lấy danh mục từ API
  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/task/category-lookup'
    }).subscribe({
      next: (res: any) => { 
        this.categories = res?.items || res || []; 
      },
      error: (err) => {
        console.error('Lỗi khi tải danh mục:', err);
        this.categories = [];
      }
    });
  }

  loadUsers(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/identity/users'
    }).subscribe({
      next: (res: any) => { 
        this.users = res?.items || res || []; 
      },
      error: () => {}
    });
  }

  fetchTasks(): void {
    this.isLoading = true;
    this.filters.skipCount = (this.page - 1) * this.filters.maxResultCount;

    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/task',
      params: this.cleanParams(this.filters)
    }).subscribe({
      next: (res: any) => {
        const data = res as { items?: TaskDto[]; totalCount?: number };
        this.taskList = data?.items || [];
        this.totalCount = data?.totalCount || 0;
        this.isLoading = false;

        this.updateKanbanColumns();

        if (this.currentView === 'calendar') {
          this.generateCalendar();
        }
      },
      error: (err) => {
        console.error('Lỗi khi tải danh sách công việc:', err);
        this.isLoading = false;
      }
    });
  }

  cleanParams(obj: any): any {
    const params: any = {};
    Object.keys(obj).forEach(key => {
      if (obj[key] !== null && obj[key] !== undefined && obj[key] !== '') {
        params[key] = obj[key];
      }
    });
    return params;
  }

  updateKanbanColumns(): void {
    this.kanbanNewTasks = this.taskList.filter(t => t.status === 0);
    this.kanbanInProgressTasks = this.taskList.filter(t => t.status === 1);
    this.kanbanCompletedTasks = this.taskList.filter(t => t.status === 2);
    this.kanbanCancelledTasks = this.taskList.filter(t => t.status === 3);
    this.kanbanOverdueTasks = this.taskList.filter(t => t.status === 5);
  }

  onSearch(): void {
    this.page = 1;
    this.fetchTasks();
  }

  toggleMyTasks(): void {
    this.filters.onlyMyTasks = !this.filters.onlyMyTasks;
    this.onSearch();
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
    this.fetchTasks();
  }

  updateTaskStatus(task: TaskDto, newStatus: number): void {
    const oldStatus = task.status;
    const oldProgress = task.progressPercent;

    task.status = newStatus;
    if (newStatus === 2) {
      task.progressPercent = 100;
    }

    this.updateKanbanColumns();

    this.rest.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/app/task/${task.id}/status`,
      params: { status: newStatus }
    }).subscribe({
      next: () => {
        if (this.currentView === 'calendar') {
          this.generateCalendar();
        }
      },
      error: (err) => {
        task.status = oldStatus;
        task.progressPercent = oldProgress;
        this.updateKanbanColumns();
        console.error('Lỗi đổi trạng thái:', err);
      }
    });
  }

  markAsCompleted(task: TaskDto): void {
    this.updateTaskStatus(task, 2);
    setTimeout(() => {
      this.fetchTasks();
    }, 500);
  }

  getTasksByStatus(status: number): TaskDto[] {
    switch (status) {
      case 0: return this.kanbanNewTasks;
      case 1: return this.kanbanInProgressTasks;
      case 2: return this.kanbanCompletedTasks;
      case 3: return this.kanbanCancelledTasks;
      case 5: return this.kanbanOverdueTasks;
      default: return [];
    }
  }

  onTaskDrop(event: CdkDragDrop<TaskDto[]>, targetStatus: number): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      const task: TaskDto = event.previousContainer.data[event.previousIndex];
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      this.updateTaskStatus(task, targetStatus);
    }
  }

  generateCalendar(): void {
    const year = this.calendarDate.getFullYear();
    const month = this.calendarDate.getMonth();
    
    this.currentCalendarYear = year;
    const monthNames = [
      'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 
      'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 
      'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'
    ];
    this.currentCalendarMonthName = monthNames[month];

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    let startingDayOfWeek = firstDayOfMonth.getDay();
    startingDayOfWeek = startingDayOfWeek === 0 ? 6 : startingDayOfWeek - 1;

    let currentWeek: any[] = [];
    this.calendarWeeks = [];

    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const d = new Date(year, month - 1, prevMonthLastDay - i);
      currentWeek.push({
        date: d,
        isCurrentMonth: false,
        isToday: this.isToday(d),
        tasks: this.getTasksForDate(d)
      });
    }

    for (let day = 1; day <= lastDayOfMonth.getDate(); day++) {
      const d = new Date(year, month, day);
      currentWeek.push({
        date: d,
        isCurrentMonth: true,
        isToday: this.isToday(d),
        tasks: this.getTasksForDate(d)
      });

      if (currentWeek.length === 7) {
        this.calendarWeeks.push(currentWeek);
        currentWeek = [];
      }
    }

    let nextMonthDay = 1;
    while (currentWeek.length > 0 && currentWeek.length < 7) {
      const d = new Date(year, month + 1, nextMonthDay++);
      currentWeek.push({
        date: d,
        isCurrentMonth: false,
        isToday: this.isToday(d),
        tasks: this.getTasksForDate(d)
      });
    }
    if (currentWeek.length > 0) {
      this.calendarWeeks.push(currentWeek);
    }
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.getDate() === today.getDate() &&
           date.getMonth() === today.getMonth() &&
           date.getFullYear() === today.getFullYear();
  }

  getTasksForDate(date: Date): TaskDto[] {
    return this.taskList.filter(task => {
      if (!task.dueDate) return false;
      const taskDate = new Date(task.dueDate);
      return taskDate.getDate() === date.getDate() &&
             taskDate.getMonth() === date.getMonth() &&
             taskDate.getFullYear() === date.getFullYear();
    });
  }

  prevMonth(): void {
    this.calendarDate.setMonth(this.calendarDate.getMonth() - 1);
    this.generateCalendar();
  }

  nextMonth(): void {
    this.calendarDate.setMonth(this.calendarDate.getMonth() + 1);
    this.generateCalendar();
  }

  goToCurrentMonth(): void {
    this.calendarDate = new Date();
    this.generateCalendar();
  }

  createTaskOnDate(date: Date): void {
    if (!this.canCreate) return;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const formattedDate = `${year}-${month}-${day}`;
    this.router.navigate(['/tasks/create'], { queryParams: { dueDate: formattedDate } });
  }

  updateTaskAssignee(task: TaskDto, assigneeId: string | null): void {
    const selectedUser = this.users.find(u => u.id === assigneeId);
    const oldAssigneeId = task.assigneeId;
    const oldAssigneeName = task.assigneeName;

    task.assigneeId = assigneeId || undefined;
    task.assigneeName = selectedUser ? (selectedUser.name || selectedUser.userName) : 'Chưa phân công';

    const params: any = {};
    if (assigneeId) {
      params.assigneeId = assigneeId;
    }

    this.rest.request<any, TaskDto>({
      method: 'POST',
      url: `/api/app/task/${task.id}/assignee`,
      params: params
    }).subscribe({
      next: (updatedTask: any) => {
        if (updatedTask && updatedTask.assigneeName) {
          task.assigneeName = updatedTask.assigneeName;
        }
        if (this.currentView === 'calendar') {
          this.generateCalendar();
        }
      },
      error: (err) => {
        task.assigneeId = oldAssigneeId;
        task.assigneeName = oldAssigneeName;
        console.error('Lỗi đổi người thực hiện:', err);
      }
    });
  }

  deleteTask(id: string): void {
    if (!confirm('Bạn có chắc chắn muốn xóa công việc này?')) return;

    this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/${id}`
    }).subscribe({
      next: () => {
        this.fetchTasks();
      }
    });
  }

  getFileList(task: TaskDto): { name: string; url: string }[] {
    if (task.attachments && task.attachments.length > 0) {
      return task.attachments.map(a => ({
        name: a.fileName || 'Tệp đính kèm',
        url: a.fileUrl?.startsWith('http') ? a.fileUrl : `${this.backendUrl}${a.fileUrl}`
      }));
    }

    if (!task.fileUrl) return [];

    const urls = (task.fileUrl || '').split(';').filter(u => !!u);
    const names = task.fileName ? task.fileName.split(';') : [];

    return urls.map((url, i) => ({
      name: names[i] || `File ${i + 1}`,
      url: url.startsWith('http') ? url : `${this.backendUrl}${url}`
    }));
  }

  getPriorityBadge(priority: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Thấp', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Trung bình', cssClass: 'bg-info-subtle text-info-emphasis border' },
      2: { text: 'Cao', cssClass: 'bg-warning-subtle text-warning-emphasis border' },
      3: { text: 'Khẩn cấp', cssClass: 'bg-danger-subtle text-danger border' }
    };
    return maps[priority] || { text: 'N/A', cssClass: 'bg-light text-dark' };
  }

  getStatusBadge(status: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Mới', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Đang làm', cssClass: 'bg-primary-subtle text-primary border' },
      2: { text: 'Hoàn thành', cssClass: 'bg-success-subtle text-success border' },
      3: { text: 'Đã hủy', cssClass: 'bg-danger-subtle text-danger border' },
      5: { text: 'Quá hạn', cssClass: 'bg-danger text-white border' }
    };
    return maps[status] || { text: 'Không xác định', cssClass: 'bg-light text-dark' };
  }
}