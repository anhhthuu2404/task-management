import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ProjectService, ProjectDto } from '@proxy/projects';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ReactiveFormsModule],
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
  
  usersList: any[] = [];
  departments: any[] = [];
  selectedDepartmentId: string = '';

  isProjectModalOpen = false;
  isDetailModalOpen = false;
  selectedEditingProjectId: string | null = null;
  
  projectForm: FormGroup = this.fb.group({
    name: ['', Validators.required],
    description: [''],
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

  ngOnInit(): void {
    this.loadProjects();
    this.loadUsersList();
    this.loadDepartments();
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
          this.loadMilestones(this.selectedProject.id);
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

  openCreateProjectModal(): void {
    this.selectedEditingProjectId = null;
    this.projectForm.reset({ status: 'Active' });
    this.isProjectModalOpen = true;
  }

  openEditProjectModal(project: ProjectDto): void {
    this.selectedEditingProjectId = project.id || null;
    this.projectForm.patchValue({
      name: project.name,
      description: project.description,
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
          this.projectForm.reset({ status: 'Active' });
          this.loadProjects();
        },
        error: (err: any) => console.error('Lỗi khi cập nhật dự án:', err)
      });
    } else {
      this.projectService.create(formValue).subscribe({
        next: () => {
          this.isProjectModalOpen = false;
          this.projectForm.reset({ status: 'Active' });
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
    this.isDetailModalOpen = true;
    if (project.id) {
      this.loadMembers(project.id);
      this.loadMilestones(project.id);
    }
  }

  closeDetailModal(): void {
    this.isDetailModalOpen = false;
    this.selectedProject = null;
    this.milestones = [];
    this.members = [];
  }

  loadMilestones(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/milestones/${projectId}`).subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : (res?.items || res?.result || []);
        
        this.milestones = data.map((m: any) => {
          const assignId = m.assigneeUserId || m.AssigneeUserId;
          return {
            id: m.id || m.Id,
            title: m.title || m.Title,
            description: m.description || m.Description,
            dueDate: m.dueDate || m.DueDate,
            status: m.status || m.Status,
            assigneeUserId: assignId,
            resolvedAssigneeName: this.getUserName(assignId)
          };
        });
      },
      error: (err: any) => {
        console.error('Lỗi tải mốc tiến độ dự án:', err);
        this.milestones = [];
      }
    });
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

 addMilestone(): void {
  if (this.milestoneForm.invalid || !this.selectedProject?.id) return;
  
  const formDateStr = this.milestoneForm.value.dueDate;
  const milestoneDate = new Date(formDateStr);
  milestoneDate.setHours(0, 0, 0, 0);

  if (this.selectedProject.startDate) {
    const projectStartDate = new Date(this.selectedProject.startDate);
    projectStartDate.setHours(0, 0, 0, 0);
    if (milestoneDate < projectStartDate) {
      alert(`Ngày mốc tiến độ không được nhỏ hơn ngày bắt đầu của dự án (${this.selectedProject.startDate.substring(0, 10)})!`);
      return;
    }
  }

  if (this.selectedProject.endDate) {
    const projectEndDate = new Date(this.selectedProject.endDate);
    projectEndDate.setHours(0, 0, 0, 0);
    if (milestoneDate > projectEndDate) {
      alert(`Ngày mốc tiến độ không được lớn hơn ngày kết thúc của dự án (${this.selectedProject.endDate.substring(0, 10)})!`);
      return;
    }
  }
  
  this.httpClient.post(`/api/app/project/milestone/${this.selectedProject.id}`, this.milestoneForm.value).subscribe({
    next: () => {
      this.milestoneForm.reset({ status: 0, assigneeUserId: null });
      this.loadMilestones(this.selectedProject!.id!);
    },
    error: (err: any) => console.error('Lỗi khi thêm mốc tiến độ:', err)
  });
}
  deleteMilestone(id: string): void {
    if (confirm('Bạn có chắc muốn xóa cột mốc này?')) {
      this.httpClient.delete(`/api/app/project/milestone/${id}`).subscribe({
        next: () => {
          if (this.selectedProject?.id) this.loadMilestones(this.selectedProject.id);
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

    this.httpClient.get<any>(`/api/app/department/${this.selectedDepartmentId}/users`).subscribe({
      next: (res: any) => {
        const deptUsers = Array.isArray(res) ? res : (res?.items || res?.result || []);
        
        if (deptUsers.length === 0) {
          alert('Phòng ban này không có nhân sự nào.');
          return;
        }

        const existingUserIds = this.members.map(m => m.userId || m.UserId);
        const usersToAdd = deptUsers.filter((u: any) => {
          const uid = u.id || u.Id || u.userId;
          return !existingUserIds.includes(uid);
        });

        if (usersToAdd.length === 0) {
          alert('Tất cả nhân sự trong phòng ban này đã có trong dự án rồi!');
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
              }
            },
            error: (err) => console.error('Lỗi khi thêm thành viên từ phòng ban:', err)
          });
        });
      },
      error: (err) => console.error('Lỗi tải nhân sự theo phòng ban:', err)
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
    this.router.navigate(['/tasks'], { queryParams: { projectId: projectId } });
  }
}