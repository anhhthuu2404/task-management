import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ProjectService } from '@proxy/projects';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-task',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-task.component.html'
})
export class CreateTaskComponent implements OnInit {
  private fb = inject(FormBuilder);
  private httpClient = inject(HttpClient);
  private projectService = inject(ProjectService);
  private router = inject(Router);

  categories: any[] = [];
  projects: any[] = [];
  departments: any[] = [];
  users: any[] = [];
  milestones: any[] = [];       
  filteredUsers: any[] = [];    
  selectedFiles: File[] = [];
  
  isSubmitting = false;
  uploadProgress = 0;
  minDate = new Date().toISOString().split('T')[0];

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(128)]],
    description: [''],
    categoryId: ['', Validators.required],
    projectId: [null, Validators.required],
    departmentId: [null],
    milestoneId: [null], 
    assigneeId: [null, Validators.required],
    dueDate: [null, Validators.required],
    priority: [1, Validators.required], 
    status: [0, Validators.required],    
    isRecurring: [false],
    frequency: [0]
  });

  ngOnInit(): void {
    this.loadCategories();
    this.loadProjects();
    this.loadDepartments();

    this.form.get('projectId')?.valueChanges.subscribe(projectId => {
      this.form.patchValue({ assigneeId: null, milestoneId: null, dueDate: null }, { emitEvent: false });
      this.milestones = [];
      this.users = [];
      this.filteredUsers = [];

      if (projectId) {
        this.loadProjectMembers(projectId);
        this.loadProjectMilestones(projectId);
      }
    });

    this.form.get('milestoneId')?.valueChanges.subscribe(milestoneId => {
      this.form.patchValue({ assigneeId: null }, { emitEvent: false });
      this.filterUsersByMilestone(milestoneId);
      this.updateDueDateFromMilestone(milestoneId);
    });
  }

  loadCategories(): void {
    this.httpClient.get<any>('/api/app/category').subscribe({
      next: (res: any) => {
        this.categories = Array.isArray(res) ? res : (res?.items || res?.result || []);
      },
      error: (err) => console.error('Lỗi tải danh mục:', err)
    });
  }

  loadProjects(): void {
    this.projectService.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: (res: any) => {
        const data = res as { items?: any[] } | any[];
        this.projects = Array.isArray(data) ? data : (data?.items || []);
      },
      error: (err) => console.error('Lỗi tải dự án:', err)
    });
  }

  loadDepartments(): void {
    this.httpClient.get<any>('/api/app/department').subscribe({
      next: (res: any) => {
        this.departments = Array.isArray(res) ? res : (res?.items || res?.result || []);
      },
      error: (err) => console.error('Lỗi tải phòng ban:', err)
    });
  }

  loadProjectMembers(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/by-project/${projectId}/members`).subscribe({
      next: (res: any) => {
        const rawMembers = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.users = rawMembers.map((m: any) => {
          const userObj = m.user || m.appUser || m;
          return {
            id: userObj.userId || userObj.UserId || userObj.id || userObj.Id || m.userId || m.id,
            name: userObj.userName || userObj.UserName || userObj.fullName || userObj.FullName || userObj.name || userObj.Name || 'Thành viên'
          };
        });
        this.filteredUsers = [...this.users];
      },
      error: (err) => {
        console.error('Lỗi tải thành viên dự án:', err);
        this.users = [];
        this.filteredUsers = [];
      }
    });
  }

  loadProjectMilestones(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/milestones/${projectId}`).subscribe({
      next: (res: any) => {
        this.milestones = Array.isArray(res) ? res : (res?.items || res?.result || []);
      },
      error: (err) => console.error('Lỗi tải mốc tiến độ:', err)
    });
  }

  filterUsersByMilestone(milestoneId: string): void {
    if (!milestoneId) {
      this.filteredUsers = [...this.users];
      return;
    }

    const selectedMilestone = this.milestones.find(m => (m.id || m.Id) === milestoneId);
    if (selectedMilestone && (selectedMilestone.assigneeUserId || selectedMilestone.AssigneeUserId)) {
      const assignedUserId = selectedMilestone.assigneeUserId || selectedMilestone.AssigneeUserId;
      this.filteredUsers = this.users.filter(u => u.id === assignedUserId);
    } else {
      this.filteredUsers = [...this.users];
    }
  }

  private updateDueDateFromMilestone(milestoneId: string): void {
    if (!milestoneId) return;

    const selectedMilestone = this.milestones.find(m => (m.id || m.Id) === milestoneId);
    if (selectedMilestone) {
      const rawDate = selectedMilestone.dueDate || selectedMilestone.DueDate;
      if (rawDate) {
        const formattedDate = new Date(rawDate).toISOString().split('T')[0];
        this.form.patchValue({ dueDate: formattedDate }, { emitEvent: false });
      }
    }
  }

  onFileSelect(event: any): void {
    const files: FileList = event.target.files;
    if (files) {
      for (let i = 0; i < files.length; i++) {
        this.selectedFiles.push(files[i]);
      }
    }
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      alert('Vui lòng điền đầy đủ thông tin bắt buộc trước khi lưu!');
      return;
    }

    this.isSubmitting = true;
    const formValues = this.form.value;

    this.httpClient.post('/api/app/task', formValues).subscribe({
      next: () => {
        this.isSubmitting = false;
        alert('Tạo công việc thành công!');
        this.router.navigate(['/tasks']);
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Lỗi khi lưu công việc:', err);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
  }
}