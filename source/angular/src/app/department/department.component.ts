import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RestService } from '@abp/ng.core';
import { ToasterService, ConfirmationService, Confirmation, ThemeSharedModule } from '@abp/ng.theme.shared';
import { PageModule } from '@abp/ng.components/page';
import { 
  DepartmentService, 
  DepartmentDto, 
  DepartmentTreeDto, 
  CreateUpdateDepartmentDto, 
  AssignUserToDepartmentDto 
} from '../proxy/departments';

export interface IdentityUserDto {
  id: string;
  userName: string;
  email: string;
  name?: string;
  surname?: string;
}

@Component({
  selector: 'app-department',
  standalone: true,
  imports: [CommonModule, FormsModule, PageModule, ThemeSharedModule],
  templateUrl: './department.component.html'
})
export class DepartmentComponent implements OnInit {
  private readonly departmentService = inject(DepartmentService);
  private readonly restService = inject(RestService);
  private readonly noti = inject(ToasterService);
  private readonly confirmation = inject(ConfirmationService);

  departments: DepartmentTreeDto[] = [];
  selectedDepartment: DepartmentTreeDto | null = null;

  isModalOpen = false;
  isEditMode = false;
  parentDepartmentName = '';
  formData: CreateUpdateDepartmentDto = { code: '', name: '', description: '', parentId: undefined, isActive: true };

  isAssignModalOpen = false;
  availableUsers: IdentityUserDto[] = [];
  assignData: AssignUserToDepartmentDto = { userId: '', departmentId: '', isManager: false };

  ngOnInit(): void {
    this.loadDepartmentTree();
  }

  loadDepartmentTree(preserveSelectionId?: string): void {
    this.departmentService.getTree({ skipHandleError: true }).subscribe({
      next: (data: DepartmentTreeDto[]) => {
        this.departments = data || [];

        if (this.departments.length > 0) {
          const targetId = preserveSelectionId || this.selectedDepartment?.id;

          if (targetId) {
            const foundNode = this.findDepartmentInTree(this.departments, targetId);
            if (foundNode) {
              const existingMembers = this.selectedDepartment?.members || [];
              this.selectedDepartment = foundNode;
              this.selectedDepartment.members = existingMembers;
            } else {
              this.selectDepartment(this.departments[0]);
            }
          } else {
            this.selectDepartment(this.departments[0]);
          }
        } else {
          this.selectedDepartment = null;
        }
      },
      error: (err) => {
        this.noti.error(err?.error?.error?.message || 'Không thể tải cây phòng ban');
      }
    });
  }

  selectDepartment(node: DepartmentTreeDto): void {
    this.selectedDepartment = node;
    if (node && node.id) {
      this.fetchDepartmentDetail(node.id);
    }
  }

  private fetchDepartmentDetail(id: string): void {
    this.restService.request<any, DepartmentDto>({
      method: 'GET',
      url: `/api/app/department/${id}`
    }, { apiName: 'default' }).subscribe({
      next: (detailRes) => {
        if (detailRes && this.selectedDepartment && this.selectedDepartment.id === id) {
          this.selectedDepartment.members = detailRes.members || [];
        }
      },
      error: (err) => {
        console.error('Không thể tải chi tiết phòng ban', err);
      }
    });
  }

  findDepartmentInTree(list: DepartmentTreeDto[], id: string): DepartmentTreeDto | null {
    for (const node of list) {
      if (node.id === id) return node;
      if (node.children && node.children.length > 0) {
        const found = this.findDepartmentInTree(node.children, id);
        if (found) return found;
      }
    }
    return null;
  }

  openCreateModal(parentId?: string | null): void {
    this.isEditMode = false;
    this.formData = { code: '', name: '', description: '', parentId: parentId ?? undefined, isActive: true };
    
    if (parentId) {
      const parentNode = this.findDepartmentInTree(this.departments, parentId);
      this.parentDepartmentName = parentNode?.name || '';
    } else {
      this.parentDepartmentName = '';
    }

    this.isModalOpen = true;
  }

  openEditModal(node: DepartmentTreeDto): void {
    this.isEditMode = true;
    this.selectedDepartment = node;
    this.formData = {
      code: node.code ?? '',
      name: node.name ?? '',
      description: node.description ?? '',
      parentId: node.parentId ?? undefined,
      isActive: node.isActive ?? true
    };
    this.parentDepartmentName = '';
    this.isModalOpen = true;
  }

  saveDepartment(): void {
    const req = this.isEditMode && this.selectedDepartment?.id
      ? this.departmentService.update(this.selectedDepartment.id, this.formData, { skipHandleError: true })
      : this.departmentService.create(this.formData, { skipHandleError: true });

    req.subscribe({
      next: () => {
        this.noti.success(this.isEditMode ? 'Cập nhật phòng ban thành công' : 'Thêm mới phòng ban thành công');
        this.closeModal();
        this.loadDepartmentTree(this.selectedDepartment?.id);
      },
      error: (err) => {
        this.noti.error(err.error?.error?.message || 'Có lỗi xảy ra khi lưu phòng ban!');
      }
    });
  }

  deleteDepartment(id: string): void {
    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa phòng ban này cùng tất cả phòng ban con?', 'Xác nhận xóa')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.departmentService.delete(id, { skipHandleError: true }).subscribe({
            next: () => {
              this.noti.success('Xóa phòng ban thành công');
              this.selectedDepartment = null;
              this.loadDepartmentTree();
            },
            error: (err) => {
              this.noti.error(err?.error?.error?.message || 'Không thể xóa phòng ban');
            }
          });
        }
      });
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  openAssignModal(): void {
    if (!this.selectedDepartment) return;
    this.assignData = { userId: '', departmentId: this.selectedDepartment.id, isManager: false };

    this.restService.request<{ items: IdentityUserDto[] }, any>({
      method: 'GET',
      url: '/api/identity/users',
      params: { maxResultCount: '100' }
    }, { apiName: 'default' }).subscribe({
      next: (res) => {
        this.availableUsers = res.items || [];
        this.isAssignModalOpen = true;
      },
      error: (err) => {
        this.noti.error(err.error?.error?.message || 'Không thể tải danh sách người dùng!');
      }
    });
  }

  saveAssignUser(): void {
    if (!this.selectedDepartment?.id || !this.assignData.userId) return;

    const payload: AssignUserToDepartmentDto = {
      userId: this.assignData.userId,
      departmentId: this.selectedDepartment.id,
      isManager: Boolean(this.assignData.isManager)
    };

    this.restService.request<AssignUserToDepartmentDto, void>({
      method: 'POST',
      url: '/api/app/department/assign-user',
      body: payload
    }, { apiName: 'default' }).subscribe({
      next: () => {
        this.noti.success('Thêm thành viên vào phòng ban thành công');
        this.closeAssignModal();
        const currentId = this.selectedDepartment?.id;
        if (currentId) {
          this.loadDepartmentTree(currentId);
          this.fetchDepartmentDetail(currentId);
        }
      },
      error: (err) => {
        this.noti.error(err.error?.error?.message || 'Không thể thêm thành viên!');
      }
    });
  }

  removeUserFromDept(userId: string): void {
    if (!this.selectedDepartment?.id) return;

    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa nhân sự này khỏi phòng ban?', 'Xác nhận gỡ nhân sự')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          const departmentId = this.selectedDepartment!.id;

          this.restService.request<void, void>({
            method: 'DELETE',
            url: `/api/app/department/user?departmentId=${departmentId}&userId=${userId}`
          }, { apiName: 'default' }).subscribe({
            next: () => {
              this.noti.success('Đã gỡ nhân sự khỏi phòng ban');
              const currentSelectedId = this.selectedDepartment?.id;
              if (currentSelectedId) {
                this.loadDepartmentTree(currentSelectedId);
                this.fetchDepartmentDetail(currentSelectedId);
              }
            },
            error: (err) => {
              this.noti.error(err.error?.error?.message || 'Không thể xóa thành viên!');
            }
          });
        }
      });
  }

  closeAssignModal(): void {
    this.isAssignModalOpen = false;
  }
}