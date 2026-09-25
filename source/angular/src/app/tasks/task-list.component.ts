import { Component, OnInit, OnDestroy, inject, NgZone, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { RestService, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { NgbPaginationModule, NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { DragDropModule, CdkDragDrop, transferArrayItem, moveItemInArray } from '@angular/cdk/drag-drop';
import { NotificationService, NotificationItem } from 'src/app/shared/services/notification.service';
import { Subscription } from 'rxjs';
import { CoreModule } from '@abp/ng.core';

export interface TaskDto {
  id: string;
  title: string;
  description?: string;
  projectId?: string;
  projectName?: string;
  assigneeId?: string;
  assigneeName?: string;
  assigneeUserName?: string;
  departmentId?: string;    // ID phòng ban
  departmentName?: string;  // Tên phòng ban
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
    CoreModule,
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
  private readonly route = inject(ActivatedRoute); 
  private readonly notificationService = inject(NotificationService);
  private readonly toaster = inject(ToasterService);
  private readonly zone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  private notificationSub?: Subscription;
  private previousNotificationCount = 0;

  // Biến Set dùng để chặn các request trùng lặp cho cùng 1 dự án đang chạy dở
  private fetchingProjectIds = new Set<string>();

  readonly backendUrl = 'https://localhost:44399';

  taskList: TaskDto[] = [];
  totalCount = 0;
  isLoading = false;

  currentView: 'list' | 'kanban' | 'calendar' = 'list';

  get notifications(): NotificationItem[] {
    const subVal = (this.notificationService as any).notificationSubject?.value;
    if (subVal) return subVal;
    let list: NotificationItem[] = [];
    this.notificationService.notifications$.subscribe(items => list = items).unsubscribe();
    return list;
  }

  getUnreadCount(): number {
    const items = this.notifications || [];
    return items.filter((n: any) => !n.isRead && !n.read).length;
  }

  // Khai báo các mảng Kanban rõ ràng, khởi tạo sẵn bằng mảng rỗng []
  kanbanNewTasks: TaskDto[] = [];
  kanbanInProgressTasks: TaskDto[] = [];
  kanbanCompletedTasks: TaskDto[] = [];
  kanbanCancelledTasks: TaskDto[] = [];
  kanbanOverdueTasks: TaskDto[] = [];

  kanbanColumns = [
    { title: 'Mới', status: 0, headerClass: 'border-primary', data: this.kanbanNewTasks },
    { title: 'Đang làm', status: 1, headerClass: 'border-info', data: this.kanbanInProgressTasks },
    { title: 'Hoàn thành', status: 2, headerClass: 'border-success', data: this.kanbanCompletedTasks },
    { title: 'Đã hủy', status: 3, headerClass: 'border-secondary', data: this.kanbanCancelledTasks },
    { title: 'Quá hạn', status: 5, headerClass: 'border-danger', data: this.kanbanOverdueTasks }
  ];

  calendarDate: Date = new Date();
  calendarWeeks: any[][] = [];
  currentCalendarMonthName: string = '';
  currentCalendarYear: number = 0;
  resetFilters() {
  this.filters = {
    keyword: '',
    filter: '',
    categoryId: '',
    assigneeId: '',
    projectId: '',
    departmentId: '',
    priority: null,
    status: null,
    skipCount: 0,
    maxResultCount: 10,
    sorting: 'CreationTime DESC'
  };
  this.page = 1;
  this.onSearch();
}

  filters = {
    keyword: '',
    filter: '', 
    categoryId: '',
    assigneeId: '',
    projectId: '', 
    departmentId: '', 
    priority: null as number | null,
    status: null as number | null,
    skipCount: 0,
    maxResultCount: 10,
    sorting: 'CreationTime DESC'
  };

  page = 1;
  categories: any[] = [];
  projects: any[] = [];
  users: any[] = []; // Danh sách toàn bộ user dự phòng
  departments: any[] = []; 

  // Lưu danh sách user theo từng dự án để hiển thị đúng ở các dòng trong bảng
  projectUsersMap: { [projectId: string]: any[] } = {};

  readonly canCreate = this.permission.getGrantedPolicy('TaskManagement.Tasks.Create');
  readonly canEdit = this.permission.getGrantedPolicy('TaskManagement.Tasks.Edit');
  readonly canDelete = this.permission.getGrantedPolicy('TaskManagement.Tasks.Delete');
  

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['projectId']) {
        this.filters.projectId = params['projectId'];
      } else {
        this.filters.projectId = '';
      }
     
      this.fetchTasks();
    });

    this.loadCategories();
    this.loadProjects();
    this.loadUsers();
    this.loadDepartments(); 

    this.notificationSub = this.notificationService.notifications$.subscribe(incoming => {
      this.zone.run(() => {
        const currentItems = incoming || [];
         
        if (currentItems.length > this.previousNotificationCount && this.previousNotificationCount !== 0) {
          const latest = currentItems[0];
          if (latest && latest.message) {
            this.toaster.info(latest.message, 'Thông báo hệ thống mới');
            this.fetchTasks();
          }
        }
         
        this.previousNotificationCount = currentItems.length;
        this.cdr.detectChanges();
      });
    });
  }

  ngOnDestroy(): void {
    if (this.notificationSub) {
      this.notificationSub.unsubscribe();
    }
  }

  onBellClick(): void {
    this.notificationService.markAllAsRead();
    this.cdr.detectChanges();
  }

  deleteNotification(item: any, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (!item) return;

    const targetId = item.id || item.key || item.message;
    this.notificationService.deleteNotification(targetId);
    this.cdr.detectChanges();
  }

  markAsRead(item: any): void {
    if (!item) return;
    const targetId = item.id || item.key;
    if (targetId) {
      this.notificationService.markAsRead(targetId);
    } else {
      this.notificationService.markAsRead();
    }
    this.cdr.detectChanges();
  }

  onNotificationClick(item: any): void {
    if (!item) return;
    this.markAsRead(item);

    const messageLower = (item.message || '').toLowerCase();
    const isDeletedTaskMessage = messageLower.includes('đã bị xóa') || 
                                 messageLower.includes('đã xóa công việc') ||
                                 messageLower.includes('không còn tồn tại');

    if (isDeletedTaskMessage) {
      return; 
    }

    const targetTaskId = item.taskId || item.referenceId;
    if (targetTaskId) {
      this.router.navigate(['/tasks/detail', targetTaskId]);
    } else {
      this.router.navigate(['/tasks']); 
    }
  }

  clearNotifications(): void {
    this.notificationService.clearNotifications();
    this.cdr.detectChanges();
  }

  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/category',
      params: { maxResultCount: 100 }
    }).subscribe({
      next: (res: any) => { 
        const raw = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.categories = raw.map((c: any) => ({
          id: c.id || c.Id,
          name: c.name || c.displayName
        }));
        this.cdr.detectChanges();
      },
      error: (err) => { 
        console.error('Không tải được danh mục cho task list:', err);
        this.categories = []; 
      }
    });
  }

  loadProjects(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/project', 
      params: { maxResultCount: 100 }
    }).subscribe({
      next: (res: any) => {
        this.projects = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.cdr.detectChanges();
      },
      error: () => { 
        this.projects = []; 
      }
    });
  }

  loadUsers(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/identity/users',
      params: { maxResultCount: 1000 }
    }).subscribe({
      next: (res: any) => { 
        this.users = Array.isArray(res) ? res : (res?.items || res?.result || []); 
        this.cdr.detectChanges(); 
      },
      error: () => {
        this.users = [];
      }
    });
  }

  // Hàm lấy danh sách user tối ưu bằng Cache, an toàn tuyệt đối với lỗi Infinite Loop
  getUsersForProject(projectId?: string): any[] {
    if (!projectId) return this.users; 
    
    if (this.projectUsersMap[projectId]) {
      return this.projectUsersMap[projectId];
    }

    // Gán tạm danh sách toàn cục hoặc mảng rỗng để tránh block UI trong lúc đợi API trả về
    this.projectUsersMap[projectId] = this.users.map((u: any) => ({
      id: u.id || u.Id,
      name: u.name || u.Name || '',
      surname: u.surname || u.Surname || '',
      userName: u.userName || u.UserName || '',
      email: u.email || u.Email || ''
    }));

    if (this.fetchingProjectIds.has(projectId)) {
      return this.projectUsersMap[projectId];
    }

    this.fetchingProjectIds.add(projectId);

    this.rest.request<any, any>({
      method: 'GET',
      url: `/api/app/project/by-project/${projectId}/members`
    }).subscribe({
      next: (res) => {
        this.fetchingProjectIds.delete(projectId);

        const projectMembers = Array.isArray(res) ? res : (res?.items || res?.result || []);
        const resolvedUsers: any[] = [];

        projectMembers.forEach((m: any) => {
          if (!m) return;
          const uId = m.userId || m.UserId || m.id || m.Id;
          if (!uId) return;

          const foundGlobalUser = this.users.find((u: any) => (u.id || u.Id) === uId) || {};

          resolvedUsers.push({
            id: uId,
            name: m.name || m.Name || foundGlobalUser.name || foundGlobalUser.Name || '',
            surname: m.surname || m.Surname || foundGlobalUser.surname || foundGlobalUser.Surname || '',
            userName: m.userName || m.UserName || foundGlobalUser.userName || foundGlobalUser.UserName || '',
            email: m.email || m.Email || foundGlobalUser.email || foundGlobalUser.Email || ''
          });
        });

        if (resolvedUsers.length > 0) {
          this.projectUsersMap[projectId] = resolvedUsers;
          this.updateTaskAssigneeNamesForProject(projectId);
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.fetchingProjectIds.delete(projectId);
      }
    });

    return this.projectUsersMap[projectId];
  }

  // Tải trước danh sách thành viên cho các dự án xuất hiện trong danh sách task hiện tại
  preloadProjectMembers(): void {
    if (!this.taskList || this.taskList.length === 0) return;
    
    const uniqueProjectIds = [
      ...new Set(
        this.taskList
          .map(t => t.projectId)
          .filter((id): id is string => !!id)
      )
    ];
    
    uniqueProjectIds.forEach(projectId => {
      if (!this.projectUsersMap[projectId] && !this.fetchingProjectIds.has(projectId)) {
        this.getUsersForProject(projectId);
      }
    });
  }

  getUserDisplayName(u: any): string {
    if (!u) return 'Chưa rõ';
    return u.name || u.userName || u.email || 'Chưa rõ';
  }

  updateTaskAssigneeNamesForProject(projectId: string): void {
    const projectUsers = this.projectUsersMap[projectId] || [];
    let isUpdated = false;

    this.taskList.forEach(task => {
      const pId = task.projectId || (task as any).ProjectId;
      if (pId === projectId && task.assigneeId) {
        const foundUser = projectUsers.find((u: any) => (u.id || u.Id) === task.assigneeId);
        if (foundUser) {
          const resolvedName = foundUser.name || foundUser.userName || foundUser.displayName || '';
          
          if (resolvedName && resolvedName !== task.assigneeName) {
            task.assigneeName = resolvedName;
            isUpdated = true;
          }
        }
      }
    });

    if (isUpdated) {
      this.updateKanbanColumns();
    }
  }

  loadDepartments(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/department'
    }).subscribe({
      next: (res: any) => {
        const rawDepts = Array.isArray(res) ? res : (res?.items || res?.result || []);
        (this as any)._rawDepartments = rawDepts; 
        
        const normalizeId = (id: any) => (!id || id === '00000000-0000-0000-0000-000000000000') ? null : String(id);

        this.departments = rawDepts.filter((d: any) => {
          const pId = normalizeId(d.parentId || d.parentDepartmentId);
          return pId === null;
        }).map((d: any) => ({
          ...d,
          id: d.id || d.Id,
          displayNameFormatted: d.name || d.displayName
        }));

        if (this.departments.length === 0 && rawDepts.length > 0) {
          this.departments = rawDepts.map((d: any) => ({
            ...d,
            id: d.id || d.Id,
            displayNameFormatted: d.name || d.displayName
          }));
        }
        
        this.cdr.detectChanges();
      },
      error: () => {
        this.departments = [];
        (this as any)._rawDepartments = [];
      }
    });
  }

  fetchTasks(): void {
    this.isLoading = true;
    this.filters.skipCount = (this.page - 1) * this.filters.maxResultCount;

    const requestFilters: any = { ...this.filters };

    if (requestFilters.departmentId) {
      const targetDeptIds = new Set<string>();
       
      const findSubDepartments = (parentId: string) => {
        targetDeptIds.add(parentId);
        const allDepts = (this as any)._rawDepartments || this.departments || [];
        const children = allDepts.filter((d: any) => 
          (d.parentId === parentId || d.parentDepartmentId === parentId)
        );
        children.forEach((child: any) => {
          const childId = child.id || child.Id;
          if (childId) {
            findSubDepartments(childId);
          }
        });
      };

      findSubDepartments(requestFilters.departmentId);

      delete requestFilters.departmentId;
      requestFilters.departmentIds = Array.from(targetDeptIds);
    }

    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/task',
      params: this.cleanParams(requestFilters)
    }).subscribe({
      next: (res: any) => {
        const data = res as { items?: TaskDto[]; totalCount?: number };
        const rawItems = data?.items || [];
        
        this.taskList = rawItems.map((task: any) => {
          const assigneeId = task.assigneeId || task.AssigneeId || task.assignedUserId || task.AssignedUserId;
          let assigneeName = task.assigneeName || task.AssigneeName || task.assigneeUserName || task.AssigneeUserName || task.userName || task.UserName;

          if (assigneeId && (!assigneeName || assigneeName === 'Chưa phân công' || assigneeName.trim() === '')) {
            const foundUser = this.users.find((u: any) => (u.id || u.Id) === assigneeId);
            if (foundUser) {
              assigneeName = foundUser.name || foundUser.userName || foundUser.displayName || foundUser.Name;
            }
          }

          return {
            ...task,
            assigneeId: assigneeId,
            assigneeName: assigneeName && assigneeName.trim() !== '' ? assigneeName : 'Chưa phân công'
          };
        });

        // Chủ động tải trước danh sách user theo dự án một cách an toàn
        this.preloadProjectMembers();

        this.totalCount = data?.totalCount || 0;
        this.isLoading = false;

        this.updateKanbanColumns();

        if (this.currentView === 'calendar') {
          this.generateCalendar();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  cleanParams(obj: any): any {
    const params: any = {};
    Object.keys(obj).forEach(key => {
      if (obj[key] !== null && obj[key] !== undefined && obj[key] !== '' && !(Array.isArray(obj[key]) && obj[key].length === 0)) {
        params[key] = obj[key];
      }
    });
    return params;
  }

  updateKanbanColumns(): void {
    this.kanbanNewTasks.length = 0;
    this.kanbanNewTasks.push(...this.taskList.filter(t => t.status === 0));

    this.kanbanInProgressTasks.length = 0;
    this.kanbanInProgressTasks.push(...this.taskList.filter(t => t.status === 1));

    this.kanbanCompletedTasks.length = 0;
    this.kanbanCompletedTasks.push(...this.taskList.filter(t => t.status === 2));

    this.kanbanCancelledTasks.length = 0;
    this.kanbanCancelledTasks.push(...this.taskList.filter(t => t.status === 3));

    this.kanbanOverdueTasks.length = 0;
    this.kanbanOverdueTasks.push(...this.taskList.filter(t => t.status === 5));
  }

  onSearch(): void {
    this.page = 1;
    this.fetchTasks();
  }

  clearProjectFilter(): void {
    this.filters.projectId = '';
    this.router.navigate(['/tasks'], { queryParams: {} });
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
      error: () => {
        task.status = oldStatus;
        task.progressPercent = oldProgress;
        this.updateKanbanColumns();
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
     
    const queryParams: any = { dueDate: formattedDate };
    if (this.filters.projectId) {
      queryParams.projectId = this.filters.projectId;
    }

    this.router.navigate(['/tasks/create'], { queryParams });
  }

  updateTaskAssignee(task: TaskDto, assigneeId: string | null): void {
    const projectUsers = this.getUsersForProject(task.projectId);
    const selectedUser = projectUsers.find(u => (u.id || u.Id) === assigneeId);
    const oldAssigneeId = task.assigneeId;
    const oldAssigneeName = task.assigneeName;

    task.assigneeId = assigneeId || undefined;
    task.assigneeName = selectedUser ? (selectedUser.name || selectedUser.userName || selectedUser.email) : 'Chưa phân công';

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
        if (updatedTask) {
          const serverName = updatedTask.assigneeName || updatedTask.AssigneeName;
          if (serverName) {
            task.assigneeName = serverName;
          }
        }
        this.updateKanbanColumns();
        if (this.currentView === 'calendar') {
          this.generateCalendar();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        task.assigneeId = oldAssigneeId;
        task.assigneeName = oldAssigneeName;
        this.updateKanbanColumns();
        this.cdr.detectChanges();
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