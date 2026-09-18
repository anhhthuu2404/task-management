import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ProjectService, ProjectDto } from '@proxy/projects';
import { CoreModule } from '@abp/ng.core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, CoreModule, ReactiveFormsModule, RouterModule],
  templateUrl: './projects.component.html'
})
export class ProjectsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private projectService = inject(ProjectService);
  private router = inject(Router);
  private httpClient = inject(HttpClient);

  projects: ProjectDto[] = [];
  selectedProject: ProjectDto | null = null;
  milestones: any[] = [];
  members: any[] = [];
  projectTasks: any[] = [];
  
  usersList: any[] = [];
  departments: any[] = [];
  categories: any[] = [];
  selectedDepartmentId: string = '';

  isProjectModalOpen = false;
  isDetailModalOpen = false;
  selectedEditingProjectId: string | null = null;
  
  projectForm: FormGroup = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    departmentId: [null],
    categoryId: [null], // Quản lý danh mục dự án
    startDate: [null],
    endDate: [null],
    status: ['Active', Validators.required]
  });

  milestoneForm: FormGroup = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    dueDate: ['', Validators.required],
    status: [0, Validators.required],
    assigneeUserId: [null]
  });

  memberForm: FormGroup = this.fb.group({
    userId: ['', Validators.required],
    role: ['Member', Validators.required]
  });

  // Getter chỉ lấy các phòng ban nhánh phụ (có phòng ban cha)
  get subDepartments(): any[] {
    if (!this.departments) return [];
    return this.departments.filter((d: any) => {
      const parentId = d.parentDepartmentId || d.ParentDepartmentId || d.parentId || d.ParentId;
      return parentId && parentId !== '00000000-0000-0000-0000-000000000000';
    });
  }

  // Hàm lọc phòng ban con dựa theo phòng ban của dự án hiện tại cho phần "Thêm nhanh theo Phòng ban"
  getChildDepartmentsForSelectedProject() {
    const currentDeptId = this.selectedProject?.departmentId || (this.selectedProject as any)?.DepartmentId;
    if (!currentDeptId) {
      return []; 
    }
    
    return this.departments.filter(d => {
      const pId = d.parentDepartmentId || d.ParentDepartmentId || d.parentId || d.ParentId;
      return pId === currentDeptId;
    });
  }

  getDepartmentName(departmentId: string): string {
    if (!departmentId || departmentId === '00000000-0000-0000-0000-000000000000') return '---';
    const dept = this.departments.find(d => (d.id || d.Id) === departmentId);
    return dept ? (dept.name || dept.Name || dept.displayName || dept.DisplayName) : '---';
  }

  // Chỉ lấy phòng ban chính (nhánh gốc) cho Dropdown tạo/sửa dự án
  get parentDepartments(): any[] {
    if (!this.departments) return [];
    
    return this.departments
      .filter((d: any) => {
        const parentId = d.parentDepartmentId || d.ParentDepartmentId || d.parentId || d.ParentId;
        return !parentId || parentId === '00000000-0000-0000-0000-000000000000';
      })
      .map((d: any) => ({
        ...d,
        id: d.id || d.Id,
        displayNameFormatted: d.name || d.displayName || d.DisplayName
      }));
  }

  get availableUsersList(): any[] {
    if (!this.members || this.members.length === 0) {
      return this.usersList;
    }
    const existingUserIds = this.members.map(m => m.userId || m.UserId);
    return this.usersList.filter(u => {
      const uid = u.id || u.Id;
      return !existingUserIds.includes(uid);
    });
  }

  get assignees(): any[] {
    if (!this.members || this.members.length === 0) {
      return [];
    }
    return this.members.map(m => {
      const uid = m.userId || m.UserId;
      const roleName = m.role || m.Role ? ` (${m.role || m.Role})` : '';
      const name = this.getUserName(uid);
      
      return {
        id: uid,
        name: name + roleName
      };
    });
  }

  // Hàm đồng bộ danh sách người thực hiện cho mốc tiến độ/công việc giống task-list
  getUsersForProject(projectId?: string): any[] {
    if (this.members && this.members.length > 0) {
      return this.members.map(m => {
        const uid = m.userId || m.UserId;
        return {
          id: uid,
          name: this.getUserName(uid)
        };
      });
    }
    return this.usersList;
  }

  ngOnInit(): void {
    this.loadProjects();
    this.loadUsersList();
    this.loadDepartments();
    this.loadCategories();
  }

  loadProjects(): void {
    this.projectService.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: (res: unknown) => {
        const data = res as { items?: ProjectDto[] } | ProjectDto[];
        this.projects = Array.isArray(data) ? data : (data?.items || []);
      },
      error: (err: any) => console.error('Lỗi tải danh sách dự án:', err)
    });
  }

  loadUsersList(): void {
    this.httpClient.get<any>('/api/identity/users?maxResultCount=100').subscribe({
      next: (res: any) => {
        this.usersList = Array.isArray(res) ? res : (res?.items || []);
        
        if (this.isDetailModalOpen && this.selectedProject?.id) {
          this.loadMembers(this.selectedProject.id);
          this.loadProjectTasksAndMilestones(this.selectedProject.id);
        }
      },
      error: (err) => console.error('Lỗi tải danh sách người dùng:', err)
    });
  }

  loadDepartments(): void {
    this.httpClient.get<any>('/api/app/department').subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.departments = data;
      },
      error: (err) => console.error('Lỗi tải danh sách phòng ban:', err)
    });
  }

  loadCategories(): void {
    this.httpClient.get<any>('/api/app/category').subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.categories = data;
      },
      error: (err) => console.error('Lỗi tải danh sách danh mục:', err)
    });
  }

  openCreateProjectModal(): void {
    this.selectedEditingProjectId = null;
    this.projectForm.reset({ status: 'Active', departmentId: null, categoryId: null });
    this.isProjectModalOpen = true;
  }

  openEditProjectModal(project: any): void {
    this.selectedEditingProjectId = project.id || null;
    this.projectForm.patchValue({
      name: project.name,
      description: project.description,
      departmentId: project.departmentId || project.DepartmentId || null,
      categoryId: project.categoryId || project.CategoryId || null,
      startDate: project.startDate ? new Date(project.startDate).toISOString().substring(0, 10) : null,
      endDate: project.endDate ? new Date(project.endDate).toISOString().substring(0, 10) : null,
      status: project.status || 'Active'
    });
    this.isProjectModalOpen = true;
  }

  saveProject(): void {
    if (this.projectForm.invalid) return;

    const formValue = this.projectForm.value;

    if (this.selectedEditingProjectId) {
      this.projectService.update(this.selectedEditingProjectId, formValue).subscribe({
        next: () => {
          this.isProjectModalOpen = false;
          this.selectedEditingProjectId = null;
          this.projectForm.reset({ status: 'Active', departmentId: null, categoryId: null });
          this.loadProjects();
        },
        error: (err: any) => console.error('Lỗi khi cập nhật dự án:', err)
      });
    } else {
      this.projectService.create(formValue).subscribe({
        next: () => {
          this.isProjectModalOpen = false;
          this.projectForm.reset({ status: 'Active', departmentId: null, categoryId: null });
          this.loadProjects();
        },
        error: (err: any) => console.error('Lỗi khi tạo dự án:', err)
      });
    }
  }

  deleteProject(id?: string): void {
    if (!id) return;
    if (confirm('Bạn có chắc chắn muốn xóa dự án này không?')) {
      this.projectService.delete(id).subscribe({
        next: () => {
          this.loadProjects();
        },
        error: (err: any) => console.error('Lỗi khi xóa dự án:', err)
      });
    }
  }

  openDetailModal(project: ProjectDto): void {
    this.selectedProject = project;
    this.selectedDepartmentId = ''; 
    this.isDetailModalOpen = true;
    if (project.id) {
      this.loadMembers(project.id);
      this.loadProjectTasksAndMilestones(project.id);
    }
  }

  closeDetailModal(): void {
    this.isDetailModalOpen = false;
    this.selectedProject = null;
    this.milestones = [];
    this.members = [];
    this.projectTasks = [];
    this.selectedDepartmentId = '';
  }

  loadMembers(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/by-project/${projectId}/members`).subscribe({
      next: (res: any) => {
        const data = res;
        const rawMembers = Array.isArray(data) ? data : (data?.items || []);
        
        this.members = rawMembers.map((m: any) => {
          const uid = m.userId || m.UserId;
          return {
            ...m,
            resolvedUserName: this.getUserName(uid)
          };
        });
      },
      error: (err: any) => console.error('Lỗi tải thành viên:', err)
    });
  }

  // Tải danh sách cột mốc trước, sau đó tải tasks và lọc bỏ các task đã nằm trong cột mốc để tránh bị hiện 2 dòng trùng nhau
  loadProjectTasksAndMilestones(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/milestones/${projectId}`).subscribe({
      next: (milestoneRes: any) => {
        const milestoneData = Array.isArray(milestoneRes) ? milestoneRes : (milestoneRes?.items || milestoneRes?.result || []);
        
        // 1. Map danh sách cột mốc ban đầu
        this.milestones = milestoneData.map((m: any) => {
          const milestoneId = m.id || m.Id;
          return {
            id: milestoneId,
            title: m.title || m.Title,
            description: m.description || m.Description,
            dueDate: m.dueDate || m.DueDate,
            status: m.status || m.Status,
            assigneeUserId: m.assigneeUserId || m.AssigneeUserId,
            resolvedAssigneeName: this.getUserName(m.assigneeUserId || m.AssigneeUserId)
          };
        });

        // 2. Tải danh sách công việc (tasks) của dự án
        this.httpClient.get<any>(`/api/app/task?projectId=${projectId}&maxResultCount=100`).subscribe({
          next: (taskRes: any) => {
            const taskData = Array.isArray(taskRes) ? taskRes : (taskRes?.items || taskRes?.result || []);
            
            // Lọc bỏ các task đã khớp với cột mốc để bảng chỉ hiện thị 1 dòng duy nhất là cột mốc
            this.projectTasks = taskData.filter((task: any) => {
              const tMilestoneId = task.milestoneId || task.MilestoneId;
              const taskTitle = (task.title || task.Title || '').trim().toLowerCase();
              
              const isMatchedWithMilestone = this.milestones.some((m: any) => {
                const mId = m.id;
                const mTitle = (m.title || '').trim().toLowerCase();
                return (tMilestoneId && tMilestoneId === mId) || (taskTitle && taskTitle === mTitle);
              });

              return !isMatchedWithMilestone; 
            });

            // 3. Đồng bộ thông tin mới nhất từ Task sang Cột mốc
            this.milestones = this.milestones.map((m: any) => {
              const milestoneId = m.id;
              const milestoneTitle = (m.title || '').trim().toLowerCase();

              const matchedTask = taskData.find((t: any) => {
                const tMilestoneId = t.milestoneId || t.MilestoneId;
                const tTitle = (t.title || t.Title || '').trim().toLowerCase();
                return (tMilestoneId && tMilestoneId === milestoneId) || (tTitle === milestoneTitle);
              });

              if (matchedTask) {
                const assignId = matchedTask.assigneeUserId || matchedTask.AssigneeUserId || matchedTask.assignedUserId || matchedTask.AssignedUserId || m.assigneeUserId;
                return {
                  ...m,
                  dueDate: matchedTask.dueDate || matchedTask.DueDate || m.dueDate,
                  status: matchedTask.status ?? matchedTask.Status ?? m.status,
                  assigneeUserId: assignId,
                  resolvedAssigneeName: this.getUserName(assignId)
                };
              }
              return m;
            });
          },
          error: (err: any) => {
            console.error('Lỗi tải danh sách công việc của dự án:', err);
            this.projectTasks = [];
          }
        });
      },
      error: (err: any) => {
        console.error('Lỗi tải mốc tiến độ dự án:', err);
        this.milestones = [];
        this.projectTasks = [];
      }
    });
  }

  deleteTask(taskId: string): void {
    if (!taskId) return;
    
    if (confirm('Bạn có chắc chắn muốn xóa công việc này không?')) {
      this.httpClient.delete(`/api/app/task/${taskId}`).subscribe({
        next: () => {
          this.projectTasks = this.projectTasks.filter(t => (t.id || t.Id) !== taskId);
        },
        error: (err: any) => {
          console.error('Lỗi khi xóa công việc:', err);
          alert('Không thể xóa công việc này.');
        }
      });
    }
  }

  addMilestone(): void {
    if (this.milestoneForm.invalid || !this.selectedProject?.id) return;
    
    const formValue = this.milestoneForm.value;
    const formDateStr = formValue.dueDate;
    const milestoneDate = new Date(formDateStr);
    milestoneDate.setHours(0, 0, 0, 0);

    if (this.selectedProject.startDate) {
      const projectStartDate = new Date(this.selectedProject.startDate);
      projectStartDate.setHours(0, 0, 0, 0);
      if (milestoneDate < projectStartDate) {
        alert(`Ngày mốc tiến độ không được nhỏ hơn ngày bắt đầu của dự án!`);
        return;
      }
    }

    if (this.selectedProject.endDate) {
      const projectEndDate = new Date(this.selectedProject.endDate);
      projectEndDate.setHours(0, 0, 0, 0);
      if (milestoneDate > projectEndDate) {
        alert(`Ngày mốc tiến độ không được lớn hơn ngày kết thúc của dự án!`);
        return;
      }
    }

    const payload = {
      title: formValue.title,
      description: formValue.description || '',
      dueDate: new Date(formValue.dueDate).toISOString(), 
      status: Number(formValue.status),
      assigneeUserId: formValue.assigneeUserId || null
    };
    
    this.httpClient.post(`/api/app/project/milestone/${this.selectedProject.id}`, payload).subscribe({
      next: () => {
        this.milestoneForm.reset({ status: 0, assigneeUserId: null });
        this.loadProjectTasksAndMilestones(this.selectedProject!.id!);
      },
      error: (err: any) => {
        console.error('Lỗi khi thêm mốc tiến độ:', err);
        alert('Không thể thêm mốc tiến độ. Vui lòng kiểm tra lại console F12!');
      }
    });
  }

  deleteMilestone(id: string): void {
    if (confirm('Bạn có chắc muốn xóa cột mốc này?')) {
      this.httpClient.delete(`/api/app/project/milestone/${id}`).subscribe({
        next: () => {
          if (this.selectedProject?.id) this.loadProjectTasksAndMilestones(this.selectedProject.id);
        },
        error: (err: any) => console.error('Lỗi khi xóa mốc tiến độ:', err)
      });
    }
  }

  addMember(): void {
    if (this.memberForm.invalid || !this.selectedProject?.id) return;
    
    const formVal = this.memberForm.value;
    const selectedUserId = formVal.userId;

    const isAlreadyMember = this.members.some(m => (m.userId || m.UserId) === selectedUserId);
    if (isAlreadyMember) {
      alert('Thành viên này đã có trong dự án rồi!');
      return;
    }

    const inputPayload = {
      userId: selectedUserId,
      role: formVal.role
    };

    this.httpClient.post(`/api/app/project/member/${this.selectedProject.id}`, inputPayload).subscribe({
      next: () => {
        this.memberForm.reset({ role: 'Member', userId: '' });
        this.loadMembers(this.selectedProject!.id!);
      },
      error: (err: any) => {
        console.error('Lỗi khi thêm thành viên:', err);
        alert('Không thể thêm thành viên này.');
      }
    });
  }

  removeMember(id: string): void {
    if (confirm('Bạn có chắc muốn xóa thành viên khỏi dự án?')) {
      this.httpClient.delete(`/api/app/project/member/${id}`).subscribe({
        next: () => {
          if (this.selectedProject?.id) this.loadMembers(this.selectedProject.id);
        },
        error: (err: any) => console.error('Lỗi khi xóa thành viên:', err)
      });
    }
  }

  addAllMembersByDepartment(): void {
    if (!this.selectedDepartmentId || !this.selectedProject?.id) {
      alert('Vui lòng chọn phòng ban trước!');
      return;
    }

    const rawDepts = this.departments || [];
    const targetDeptIds = new Set<string>();

    const findSubDepartments = (parentId: string) => {
      targetDeptIds.add(parentId);
      const children = rawDepts.filter((d: any) => {
        const pId = d.parentDepartmentId || d.ParentDepartmentId || d.parentId || d.ParentId;
        return pId === parentId;
      });
      children.forEach((child: any) => {
        const childId = child.id || child.Id;
        if (childId) findSubDepartments(childId);
      });
    };

    findSubDepartments(this.selectedDepartmentId);

    const requests = Array.from(targetDeptIds).map(deptId => 
      this.httpClient.get<any>(`/api/app/department/${deptId}/users`).pipe(
        catchError(() => of([]))
      )
    );

    forkJoin(requests).subscribe({
      next: (responses: any[]) => {
        const allDeptUsers: any[] = [];
        responses.forEach(res => {
          const users = Array.isArray(res) ? res : (res?.items || res?.result || []);
          allDeptUsers.push(...users);
        });

        const uniqueUsers = Array.from(
          new Map(allDeptUsers.map((u: any) => [u.id || u.Id || u.userId, u])).values()
        );

        if (uniqueUsers.length === 0) {
          alert('Phòng ban này và các nhánh con không có nhân sự nào.');
          return;
        }

        const existingUserIds = this.members.map(m => m.userId || m.UserId);
        const usersToAdd = uniqueUsers.filter((u: any) => {
          const uid = u.id || u.Id || u.userId;
          return !existingUserIds.includes(uid);
        });

        if (usersToAdd.length === 0) {
          alert('Tất cả nhân sự trong phòng ban này và các nhánh con đã có trong dự án rồi!');
          return;
        }

        let successCount = 0;
        usersToAdd.forEach((u: any) => {
          const uid = u.id || u.Id || u.userId;
          const inputPayload = {
            userId: uid,
            role: 'Member'
          };

          this.httpClient.post(`/api/app/project/member/${this.selectedProject!.id}`, inputPayload).subscribe({
            next: () => {
              successCount++;
              if (successCount === usersToAdd.length) {
                this.loadMembers(this.selectedProject!.id!);
                this.selectedDepartmentId = '';
                alert(`Đã thêm thành công ${successCount} nhân sự từ phòng ban vào dự án!`);
              }
            },
            error: (err: any) => console.error('Lỗi khi thêm thành viên từ phòng ban:', err)
          });
        });
      },
      error: (err: any) => console.error('Lỗi tải nhân sự theo phòng ban:', err)
    });
  }

  getUserName(userId: string): string {
    if (!userId || userId === '00000000-0000-0000-0000-000000000000') return 'Chưa phân công';

    const matchedMember = this.members.find(m => (m.userId || m.UserId) === userId);
    if (matchedMember && matchedMember.resolvedUserName && matchedMember.resolvedUserName !== userId) {
      return matchedMember.resolvedUserName;
    }

    const user = this.usersList.find(u => 
      u.id === userId || 
      u.Id === userId || 
      u.id?.toLowerCase() === userId?.toLowerCase()
    );
    
    if (user) {
      return user.userName || user.UserName || user.name || user.Name;
    }
    
    return userId;
  }

  viewProjectTasks(projectId?: string): void {
    if (!projectId) return;
    this.router.navigate(['/tasks/list'], { queryParams: { projectId: projectId } });
  }

  getCategoryName(categoryId: string): string {
    if (!categoryId) return '---';
    const cat = this.categories.find((c: any) => (c.id || c.Id) === categoryId);
    return cat ? (cat.name || cat.Name) : '---';
  }
}