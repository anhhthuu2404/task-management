import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProjectService, ProjectDto, MilestoneDto, ProjectMemberDto } from '@proxy/projects';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './projects.component.html'
})
export class ProjectsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private projectService = inject(ProjectService);

  projects: ProjectDto[] = [];
  selectedProject: ProjectDto | null = null;
  milestones: MilestoneDto[] = [];
  members: any[] = [];
  
  usersList: any[] = [
    { id: '00000000-0000-0000-0000-000000000001', userName: 'admin', email: 'admin@abp.io' },
    { id: '00000000-0000-0000-0000-000000000002', userName: 'Anhthuu', email: 'anhthuu24405@gmail.com' }
  ];

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

  get assignees(): any[] {
    if (!this.members || this.members.length === 0) {
      return [];
    }
    return this.members.map(m => {
      const uid = m.userId || m.UserId;
      const roleName = m.role || m.Role ? ` (${m.role || m.Role})` : '';
      const user = this.usersList.find(u => u.id === uid);
      const name = user ? user.userName : (m.userName || m.UserName || 'Thành viên');
      return {
        id: uid,
        name: name + roleName
      };
    });
  }

  ngOnInit(): void {
    this.loadProjects();
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

  loadMilestones(projectId: string): void {
    this.projectService.getMilestones(projectId).subscribe({
      next: (res: unknown) => {
        const data = res as { items?: MilestoneDto[] } | MilestoneDto[];
        this.milestones = Array.isArray(data) ? data : (data?.items || []);
      },
      error: (err: any) => console.error(err)
    });
  }

  loadMembers(projectId: string): void {
    this.projectService.getMembers(projectId).subscribe({
      next: (res: unknown) => {
        const data = res as { items?: any[] } | any[];
        this.members = Array.isArray(data) ? data : (data?.items || []);
      },
      error: (err: any) => console.error(err)
    });
  }

  addMilestone(): void {
    if (this.milestoneForm.invalid || !this.selectedProject?.id) return;
    this.projectService.createMilestone(this.selectedProject.id, this.milestoneForm.value).subscribe({
      next: () => {
        this.milestoneForm.reset({ status: 0, assigneeUserId: null });
        this.loadMilestones(this.selectedProject!.id!);
      },
      error: (err: any) => console.error(err)
    });
  }

  deleteMilestone(id: string): void {
    this.projectService.deleteMilestone(id).subscribe({
      next: () => {
        if (this.selectedProject?.id) this.loadMilestones(this.selectedProject.id);
      },
      error: (err: any) => console.error(err)
    });
  }

  addMember(): void {
    if (this.memberForm.invalid || !this.selectedProject?.id) return;
    
    const inputPayload = {
      projectId: this.selectedProject.id,
      ...this.memberForm.value
    };

    this.projectService.addMember(this.selectedProject.id, inputPayload).subscribe({
      next: () => {
        this.memberForm.reset({ role: 'Member' });
        this.loadMembers(this.selectedProject!.id!);
      },
      error: (err: any) => console.error('Lỗi khi thêm thành viên:', err)
    });
  }

  removeMember(id: string): void {
    this.projectService.removeMember(id).subscribe({
      next: () => {
        if (this.selectedProject?.id) this.loadMembers(this.selectedProject.id);
      },
      error: (err: any) => console.error(err)
    });
  }

  getUserName(userId: string): string {
    if (!userId) return 'Chưa phân công';
    const user = this.usersList.find(u => u.id === userId);
    if (user) return user.userName;
    
    const member = this.members.find(m => (m.userId || m.UserId) === userId);
    if (member) {
      return member.userName || member.UserName || userId;
    }
    
    return userId;
  }
}