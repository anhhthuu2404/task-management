import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ProjectService } from '@proxy/projects';
import { Router } from '@angular/router';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-create-task',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CoreModule],
  templateUrl: './create-task.component.html'
})
export class CreateTaskComponent implements OnInit {
  private fb = inject(FormBuilder);
  private httpClient = inject(HttpClient);
  private projectService = inject(ProjectService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  categories: any[] = [];
  
  // Khai báo đủ biến cho cả HTML và Logic lọc
  projects: any[] = [];
  departments: any[] = [];
  
  allProjects: any[] = [];
  filteredProjects: any[] = [];
  
  allDepartments: any[] = [];
  filteredDepartments: any[] = [];

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

    // 1. Lắng nghe thay đổi Danh mục để lọc Dự án và Phòng ban tương ứng
    this.form.get('categoryId')?.valueChanges.subscribe(categoryId => {
      this.form.patchValue({ projectId: null, departmentId: null, assigneeId: null, milestoneId: null }, { emitEvent: false });
      
      if (categoryId) {
        // Lọc dự án thuộc danh mục được chọn
        this.filteredProjects = this.allProjects.filter(p => 
          String(p.categoryId || p.CategoryId || p.catId || '') === String(categoryId)
        );
        this.projects = [...this.filteredProjects]; // Đồng bộ cho thẻ select HTML

        // Lọc phòng ban liên quan tới danh mục hoặc các dự án trong danh mục đó
        const validDepartmentIds = this.filteredProjects.map(p => String(p.departmentId || p.DepartmentId)).filter(id => !!id);
        
        this.filteredDepartments = this.allDepartments.filter(d => 
          validDepartmentIds.includes(String(d.id || d.Id)) || 
          String(d.categoryId || d.CategoryId || '') === String(categoryId)
        );
        this.departments = [...this.filteredDepartments]; // Đồng bộ cho thẻ select HTML
      } else {
        this.filteredProjects = [];
        this.projects = [];
        this.filteredDepartments = [];
        this.departments = [];
      }

      this.milestones = [];
      this.users = [];
      this.filteredUsers = [];
      this.cdr.detectChanges();
    });

    // 2. Lắng nghe thay đổi Dự án để tự động đồng bộ Phòng ban và tải Thành viên/Mốc tiến độ
    this.form.get('projectId')?.valueChanges.subscribe(projectId => {
      this.form.patchValue({ assigneeId: null, milestoneId: null, dueDate: null }, { emitEvent: false });
      this.milestones = [];
      this.users = [];
      this.filteredUsers = [];

      if (projectId) {
        const selectedProj = this.allProjects.find(p => String(p.id || p.Id) === String(projectId));
        if (selectedProj && (selectedProj.departmentId || selectedProj.DepartmentId)) {
          this.form.patchValue({ departmentId: selectedProj.departmentId || selectedProj.DepartmentId }, { emitEvent: false });
        }

        this.loadProjectMembers(projectId);
        this.loadProjectMilestones(projectId);
      }
    });

    // 3. Lắng nghe thay đổi Mốc tiến độ
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
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh mục:', err)
    });
  }

  loadProjects(): void {
    this.projectService.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: (res: any) => {
        const data = res as { items?: any[] } | any[];
        this.allProjects = Array.isArray(data) ? data : (data?.items || []);
        this.filteredProjects = [];
        this.projects = []; // Ban đầu chưa chọn danh mục thì danh sách trống
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Lỗi tải dự án:', err)
    });
  }

  loadDepartments(): void {
    this.httpClient.get<any>('/api/app/department').subscribe({
      next: (res: any) => {
        const rawDepts = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.allDepartments = rawDepts.filter((d: any) => !d.parentId && !d.parentDepartmentId);
        this.filteredDepartments = [];
        this.departments = []; // Ban đầu chưa chọn danh mục thì danh sách trống
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Lỗi tải phòng ban:', err)
    });
  }

  loadProjectMembers(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/by-project/${projectId}/members`).subscribe({
      next: (res: any) => {
        const rawMembers = Array.isArray(res) ? res : (res?.items || res?.result || res?.rows || []);
        
        if (rawMembers && rawMembers.length > 0) {
          const memberIds = rawMembers.map((m: any) => 
            String(m.userId || m.UserId || m.id || m.Id || (m.user && (m.user.id || m.user.Id)))
          ).filter((id: string) => !!id);

          this.httpClient.get<any>('/api/identity/users', { params: { maxResultCount: 1000 } }).subscribe({
            next: (userRes: any) => {
              const allSystemUsers = Array.isArray(userRes) ? userRes : (userRes?.items || userRes?.result || []);
              
              const matchedUsers = allSystemUsers
                .filter((u: any) => memberIds.includes(String(u.id || u.Id)))
                .map((u: any) => {
                  const name = u.name || u.Name || u.firstName || u.FirstName || '';
                  const userName = u.userName || u.UserName || '';
                  const displayName = u.fullName || u.FullName || '';

                  let finalName = name;
                  if (!finalName) finalName = displayName;
                  if (!finalName) finalName = userName;
                  if (!finalName) finalName = u.email || 'Thành viên';

                  return {
                    id: String(u.id || u.Id),
                    name: finalName,
                    userName: userName,
                    email: u.email || u.Email
                  };
                });

              this.users = matchedUsers.length > 0 ? matchedUsers : allSystemUsers.map((u: any) => {
                const name = u.name || u.Name || u.firstName || u.FirstName || '';
                return {
                  id: String(u.id || u.Id),
                  name: name || u.userName || u.email || 'Thành viên',
                  userName: u.userName,
                  email: u.email
                };
              });

              this.filteredUsers = [...this.users];
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Lỗi tải danh sách user hệ thống:', err);
              this.loadAllSystemUsers();
            }
          });

        } else {
          this.loadAllSystemUsers();
        }
      },
      error: (err) => {
        console.warn('Không tải được thành viên theo dự án, chuyển sang lấy toàn hệ thống:', err);
        this.loadAllSystemUsers();
      }
    });
  }

  loadAllSystemUsers(): void {
    this.httpClient.get<any>('/api/identity/users', { params: { maxResultCount: 1000 } }).subscribe({
      next: (res: any) => {
        const rawUsers = Array.isArray(res) ? res : (res?.items || res?.result || res?.rows || []);
        this.users = rawUsers.map((u: any) => {
          const name = u.name || u.Name || u.firstName || u.FirstName || '';
          const userName = u.userName || u.UserName || '';
          const displayName = u.fullName || u.FullName || '';

          let finalName = name;
          if (!finalName) finalName = displayName;
          if (!finalName) finalName = userName;
          if (!finalName) finalName = u.email || 'Thành viên';

          return {
            id: String(u.id || u.Id),
            name: finalName,
            userName: userName,
            email: u.email || u.Email
          };
        });

        this.filteredUsers = [...this.users];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Lỗi tải danh sách người dùng hệ thống:', err);
        this.users = [];
        this.filteredUsers = [];
        this.cdr.detectChanges();
      }
    });
  }

  loadProjectMilestones(projectId: string): void {
    this.httpClient.get<any>(`/api/app/project/milestones/${projectId}`).subscribe({
      next: (res: any) => {
        this.milestones = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.cdr.detectChanges();
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
        this.cdr.detectChanges();
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
  }
}