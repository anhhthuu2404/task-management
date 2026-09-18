import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { RestService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { CoreModule } from '@abp/ng.core';

export interface TaskAttachmentDto {
  fileName?: string;
  fileContent?: string;
}

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CoreModule],
  templateUrl: './task-form.component.html'
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rest = inject(RestService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toaster = inject(ToasterService);
  private readonly cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  taskId: string | null = null;
  isEditMode = false;
  isSubmitting = false;
  isLoading = false;

  categories: any[] = [];
  projects: any[] = [];
  departments: any[] = [];
  milestones: any[] = [];
  users: any[] = [];
  filteredUsers: any[] = [];

  minDate: string = '';
  selectedFiles: File[] = [];
  uploadProgress = 0;

  ngOnInit(): void {
    this.initForm();
    this.setMinDate();
    this.loadLookups();

    this.taskId = this.route.snapshot.paramMap.get('id');
    if (this.taskId) {
      this.isEditMode = true;
      this.loadTaskData(this.taskId);
    } else {
      this.route.queryParams.subscribe(params => {
        if (params['dueDate']) {
          this.form.patchValue({ dueDate: params['dueDate'] });
        }
        if (params['projectId']) {
          this.form.patchValue({ projectId: params['projectId'] });
          this.onProjectChange(params['projectId']);
        }
      });
    }
  }

  initForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [''],
      categoryId: [{ value: '', disabled: true }, Validators.required],
      projectId: [null, Validators.required],
      departmentId: [{ value: null, disabled: true }],
      milestoneId: [null],
      assigneeId: [null, Validators.required],
      dueDate: ['', Validators.required],
      priority: [1, Validators.required],
      status: [0, Validators.required],
      isRecurring: [false],
      frequency: [0]
    });

    this.form.get('dueDate')?.valueChanges.subscribe(dateValue => {
      const currentStatus = Number(this.form.get('status')?.value);
      if (dateValue && currentStatus !== 1 && currentStatus !== 2 && currentStatus !== 3) {
        this.form.patchValue({ status: 1 }, { emitEvent: false });
      }
    });

    this.form.get('projectId')?.valueChanges.subscribe(projectId => {
      if (projectId) {
        this.onProjectChange(projectId);
      } else {
        this.filteredUsers = [...this.users];
        this.milestones = [];
        this.form.patchValue({ departmentId: null, categoryId: '' }, { emitEvent: false });
        this.cdr.detectChanges();
      }
    });

    this.form.get('milestoneId')?.valueChanges.subscribe(milestoneId => {
      if (milestoneId && this.form.get('milestoneId')?.touched) {
        const selectedMilestone = this.milestones.find(m => (m.id === milestoneId || m.Id === milestoneId));
        if (selectedMilestone && (selectedMilestone.dueDate || selectedMilestone.DueDate)) {
          const dateStr = (selectedMilestone.dueDate || selectedMilestone.DueDate).split('T')[0];
          this.form.patchValue({ dueDate: dateStr });
        }
      }
    });
  }

  setMinDate(): void {
    const today = new Date();
    this.minDate = today.toISOString().split('T')[0];
  }
  
  loadLookups(): void {
    // 1. Load danh mục
    this.rest.request<any, any>({ method: 'GET', url: '/api/task-management/categories', params: { maxResultCount: 100 } }).subscribe({
      next: (res) => { 
        this.categories = Array.isArray(res) ? res : (res?.items || res?.result || []); 
        this.cdr.detectChanges();
      }
    });

    // 2. Load dự án
    this.rest.request<any, any>({ method: 'GET', url: '/api/app/project', params: { maxResultCount: 100 } }).subscribe({
      next: (res) => { 
        this.projects = Array.isArray(res) ? res : (res?.items || res?.result || []); 
        this.cdr.detectChanges();
      }
    });

    // 3. Load phòng ban
    this.rest.request<any, any>({ method: 'GET', url: '/api/app/department' }).subscribe({
      next: (res) => { 
        const rawDepts = Array.isArray(res) ? res : (res?.items || res?.result || []); 
        this.departments = rawDepts.filter((d: any) => !d.parentId && !d.parentDepartmentId && !d.ParentId);
        this.cdr.detectChanges();
      },
      error: () => { this.departments = []; }
    });

    // 4. Load toàn bộ người dùng ban đầu
    this.rest.request<any, any>({ method: 'GET', url: '/api/identity/users' }).subscribe({
      next: (res) => { 
        this.users = Array.isArray(res) ? res : (res?.items || res?.result || []); 
        this.filteredUsers = [...this.users];
        this.cdr.detectChanges();
      }
    });
  }

  onProjectChange(projectId: string): void {
    if (!projectId) {
      this.milestones = [];
      this.filteredUsers = [...this.users];
      this.form.patchValue({ departmentId: null, categoryId: '' }, { emitEvent: false });
      queueMicrotask(() => this.cdr.detectChanges());
      return;
    }

    const selectedProject = this.projects.find(p => (p.id === projectId || p.Id === projectId));
    if (selectedProject) {
      const deptId = selectedProject.departmentId || selectedProject.DepartmentId || selectedProject.department?.id || selectedProject.Department?.Id;
      const catId = selectedProject.categoryId || selectedProject.CategoryId || selectedProject.category?.id || selectedProject.Category?.Id;

      this.form.patchValue({
        departmentId: deptId || null,
        categoryId: catId || ''
      }, { emitEvent: false });
    }

    this.rest.request<any, any>({ method: 'GET', url: `/api/app/project/milestones/${projectId}` }).subscribe({
      next: (res) => { 
        const rawList = Array.isArray(res) ? res : (res?.items || res?.result || []);
        this.milestones = [...rawList];
        queueMicrotask(() => this.cdr.detectChanges());
      },
      error: () => {
        this.rest.request<any, any>({ 
          method: 'GET', 
          url: `/api/app/milestone`, 
          params: { projectId: projectId, maxResultCount: 100 } 
        }).subscribe({
          next: (res) => {
            const rawList = Array.isArray(res) ? res : (res?.items || res?.result || []);
            this.milestones = [...rawList];
            queueMicrotask(() => this.cdr.detectChanges());
          },
          error: () => {
            this.milestones = [];
            queueMicrotask(() => this.cdr.detectChanges());
          }
        });
      }
    });

    this.rest.request<any, any>({ method: 'GET', url: `/api/app/project/by-project/${projectId}/members` }).subscribe({
      next: (res) => {
        const projectMembers = Array.isArray(res) ? res : (res?.items || res?.result || []);
        
        this.filteredUsers = projectMembers.map((m: any) => ({
          id: m.userId || m.UserId,
          userName: m.userName || m.UserName,
          name: m.name || m.Name,
          surname: m.surname || m.Surname,
          email: m.email || m.Email,
          role: m.role || m.Role
        }));

        if (this.filteredUsers.length === 0) {
          this.filteredUsers = [...this.users];
        }

        const currentVal = this.form?.get('assigneeId')?.value;
        if (currentVal) {
          this.form?.get('assigneeId')?.setValue(currentVal, { emitEvent: false });
        }

        queueMicrotask(() => this.cdr.detectChanges());
      },
      error: (err) => {
        console.error('Không thể tải danh sách thành viên dự án:', err);
        this.filteredUsers = [...this.users];
        queueMicrotask(() => this.cdr.detectChanges());
      }
    });
  }

  loadTaskData(id: string): void {
    this.isLoading = true;
    this.rest.request<any, any>({ method: 'GET', url: `/api/app/task/${id}` }).subscribe({
      next: (task) => {
        if (task) {
          const projId = task.projectId || task.ProjectId || task.project?.id || task.Project?.Id;
          
          if (projId) {
            this.onProjectChange(projId);

            this.rest.request<any, any>({ method: 'GET', url: `/api/app/project/milestones/${projId}` }).subscribe({
              next: (res) => {
                this.milestones = Array.isArray(res) ? res : (res?.items || res?.result || []);
                this.patchAndDisableForm(task);
              },
              error: () => {
                this.rest.request<any, any>({ 
                  method: 'GET', 
                  url: `/api/app/milestone`, 
                  params: { projectId: projId, maxResultCount: 100 } 
                }).subscribe({
                  next: (res) => {
                    this.milestones = Array.isArray(res) ? res : (res?.items || res?.result || []);
                    this.patchAndDisableForm(task);
                  },
                  error: () => {
                    this.milestones = [];
                    this.patchAndDisableForm(task);
                  }
                });
              }
            });
          } else {
            this.patchAndDisableForm(task);
          }
        }
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Không thể tải thông tin công việc.');
        this.router.navigate(['/tasks']);
      }
    });
  }

  private patchAndDisableForm(task: any): void {
    const projId = task.projectId || task.ProjectId || task.project?.id || task.Project?.Id;
    const catId = task.categoryId || task.CategoryId || task.category?.id || task.Category?.Id;
    const deptId = task.departmentId || task.DepartmentId || task.department?.id || task.Department?.Id;

    this.form.patchValue({
      title: task.title || task.Title,
      description: task.description || task.Description,
      categoryId: catId || '',
      projectId: projId || null,
      departmentId: deptId || null,
      milestoneId: task.milestoneId || task.MilestoneId,
      assigneeId: task.assigneeId || task.AssigneeId,
      dueDate: (task.dueDate || task.DueDate) ? (task.dueDate || task.DueDate).split('T')[0] : '',
      priority: task.priority !== undefined ? task.priority : task.Priority,
      status: task.status !== undefined ? task.status : task.Status,
      isRecurring: task.isRecurring !== undefined ? task.isRecurring : task.IsRecurring || false,
      frequency: task.frequency !== undefined ? task.frequency : task.Frequency || 0
    }, { emitEvent: false });
    
    this.form.get('categoryId')?.disable({ emitEvent: false });
    this.form.get('projectId')?.disable({ emitEvent: false });
    this.form.get('departmentId')?.disable({ emitEvent: false });

    this.isLoading = false;
    
    queueMicrotask(() => {
      this.cdr.detectChanges();
    });
  }

  onFileSelect(event: any): void {
    const files: FileList = event.target.files;
    if (files) {
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (file.size > 10 * 1024 * 1024) {
          this.toaster.warn(`Tệp ${file.name} vượt quá dung lượng cho phép (10MB).`);
          continue;
        }
        this.selectedFiles.push(file);
      }
    }
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toaster.warn('Vui lòng điền đầy đủ các thông tin bắt buộc.');
      return;
    }

    this.isSubmitting = true;

    const filePromises = this.selectedFiles.map(file => {
      return new Promise<TaskAttachmentDto>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
          resolve({
            fileName: file.name,
            fileContent: reader.result as string
          });
        };
        reader.onerror = error => reject(error);
        reader.readAsDataURL(file);
      });
    });

    Promise.all(filePromises).then(attachments => {
      const requestBody = {
        ...this.form.getRawValue(),
        attachments: attachments
      };

      const requestMethod = this.isEditMode ? 'PUT' : 'POST';
      const requestUrl = this.isEditMode ? `/api/app/task/${this.taskId}` : '/api/app/task';

      this.rest.request<any, any>({
        method: requestMethod,
        url: requestUrl,
        body: requestBody
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.toaster.success(this.isEditMode ? 'Cập nhật công việc thành công!' : 'Tạo mới công việc thành công!');
          this.router.navigate(['/tasks']);
        },
        error: (err) => {
          this.isSubmitting = false;
          const errorObj = err?.error?.error;
          
          if (errorObj?.validationErrors && errorObj.validationErrors.length > 0) {
            const messages = errorObj.validationErrors.map((e: any) => e.message).join('\n');
            this.toaster.error(messages, 'Lỗi dữ liệu đầu vào', { life: 5000 });
          } else {
            const serverMessage = errorObj?.message || 'Đã có lỗi xảy ra khi lưu công việc.';
            this.toaster.error(serverMessage, 'Thông báo', { life: 5000 });
          }
          this.cdr.detectChanges();
        }
      });
    }).catch(() => {
      this.isSubmitting = false;
      this.toaster.error('Lỗi khi xử lý tệp đính kèm.');
      this.cdr.detectChanges();
    });
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
  }
}