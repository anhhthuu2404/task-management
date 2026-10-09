import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoreModule, RestService, ListService, LocalizationService } from '@abp/ng.core';
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

export interface IdentityRoleDto {
  id: string;
  name: string;
  isDefault?: boolean;
}

@Component({
  selector: 'app-department',
  standalone: true,
  imports: [CommonModule, CoreModule, FormsModule, PageModule, ThemeSharedModule],
  providers: [ListService],
  templateUrl: './department.component.html'
})
export class DepartmentComponent implements OnInit {
  private readonly departmentService = inject(DepartmentService);
  private readonly restService = inject(RestService);
  private readonly noti = inject(ToasterService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly localizationService = inject(LocalizationService);

  departments: DepartmentTreeDto[] = [];
  selectedDepartment: DepartmentTreeDto | null = null;

  isModalOpen = false;
  isEditMode = false;
  parentDepartmentName = '';
  formData: CreateUpdateDepartmentDto = { code: '', name: '', description: '', parentId: undefined, isActive: true };

  isAssignModalOpen = false;
  availableUsers: IdentityUserDto[] = [];
  availableRoles: IdentityRoleDto[] = [];
  assignData: AssignUserToDepartmentDto = { userId: '', departmentId: '', isManager: false };

  // Biến phục vụ phân trang danh sách nhân sự
  page: number = 1;
  pageSize: number = 5;
  mathCeil = Math.ceil;

  ngOnInit(): void {
    this.loadDepartmentTree();
  }

  loadDepartmentTree(preserveSelectionId?: string): void {
    // Lấy ngôn ngữ chuẩn từ đối tượng global `abp` của hệ thống
    const currentCulture = (window as any).abp?.localization?.currentCulture?.name || 'vi';

    // Gọi đúng cú pháp nhận 1 tham số truyền vào service proxy của ABP
    this.departmentService.getTree({ culture: currentCulture } as any).subscribe({
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
        const isEn = this.localizationService.currentLang?.startsWith('en');
        this.noti.error(err?.error?.error?.message || (isEn ? 'Could not load department tree' : 'Không thể tải cây phòng ban'));
      }
    });
  }

  selectDepartment(node: DepartmentTreeDto): void {
    this.selectedDepartment = node;
    this.page = 1; // Reset về trang 1 khi chọn phòng ban mới
    if (node && node.id) {
      this.fetchDepartmentDetail(node.id);
    }
  }

  // Getter tính toán nhân sự hiển thị trên trang hiện tại
  get paginatedMembers() {
    if (!this.selectedDepartment || !this.selectedDepartment.members) {
      return [];
    }
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.selectedDepartment.members.slice(start, end);
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
  }

  private fetchDepartmentDetail(id: string): void {
    const currentCulture = (window as any).abp?.localization?.currentCulture?.name || 'vi';

    this.restService.request<any, DepartmentDto>({
      method: 'GET',
      url: `/api/app/department/${id}`,
      params: { culture: currentCulture }
    }, { apiName: 'default' }).subscribe({
      next: (detailRes) => {
        if (detailRes && this.selectedDepartment && this.selectedDepartment.id === id) {
          this.selectedDepartment.members = detailRes.members || [];
        }
      },
      error: (err) => {
        const isEn = this.localizationService.currentLang?.startsWith('en');
        console.error(isEn ? 'Could not load department details' : 'Không thể tải chi tiết phòng ban', err);
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

  private loadRoles(): Promise<void> {
    return new Promise((resolve) => {
      this.restService.request<{ items: IdentityRoleDto[] }, any>({
        method: 'GET',
        url: '/api/identity/roles',
        params: { maxResultCount: '100' }
      }, { apiName: 'default' }).subscribe({
        next: (res) => {
          this.availableRoles = res.items || [];
          resolve();
        },
        error: (err) => {
          const isEn = this.localizationService.currentLang?.startsWith('en');
          console.error(isEn ? 'Could not load roles list' : 'Không thể tải danh sách vai trò', err);
          resolve();
        }
      });
    });
  }

  async openCreateModal(parentId?: string | null): Promise<void> {
    await this.loadRoles();
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

  async openEditModal(node: DepartmentTreeDto): Promise<void> {
    await this.loadRoles();
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
    const isEn = this.localizationService.currentLang?.startsWith('en');

    const req = this.isEditMode && this.selectedDepartment?.id
      ? this.departmentService.update(this.selectedDepartment.id, this.formData)
      : this.departmentService.create(this.formData);

    req.subscribe({
      next: () => {
        const successMsg = this.isEditMode 
          ? (isEn ? 'Department updated successfully' : 'Cập nhật phòng ban thành công')
          : (isEn ? 'Department created successfully' : 'Thêm mới phòng ban thành công');
        this.noti.success(successMsg);
        this.closeModal();
        this.loadDepartmentTree(this.selectedDepartment?.id);
      },
      error: (err) => {
        const defaultMsg = isEn ? 'An error occurred while saving the department!' : 'Có lỗi xảy ra khi lưu phòng ban!';
        this.noti.error(err.error?.error?.message || defaultMsg);
      }
    });
  }

  deleteDepartment(id: string): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');

    this.confirmation
      .warn(
        isEn ? 'Are you sure you want to delete this department and all its child departments?' : 'Bạn có chắc chắn muốn xóa phòng ban này cùng tất cả phòng ban con?', 
        isEn ? 'Delete confirmation' : 'Xác nhận xóa'
      )
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.departmentService.delete(id).subscribe({
            next: () => {
              this.noti.success(isEn ? 'Department deleted successfully' : 'Xóa phòng ban thành công');
              this.selectedDepartment = null;
              this.loadDepartmentTree();
            },
            error: (err) => {
              this.noti.error(err?.error?.error?.message || (isEn ? 'Could not delete department' : 'Không thể xóa phòng ban'));
            }
          });
        }
      });
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  async openAssignModal(): Promise<void> {
    const isEn = this.localizationService.currentLang?.startsWith('en');

    if (!this.selectedDepartment || !this.selectedDepartment.code) {
      this.noti.warn(isEn ? 'This department does not have a code (Role Code) to filter personnel!' : 'Phòng ban này chưa có mã (Role Code) để lọc nhân sự!');
      return;
    }

    this.assignData = { userId: '', departmentId: this.selectedDepartment.id, isManager: false };
    const roleName = this.selectedDepartment.code.trim();

    try {
      const roleRes: any = await this.restService.request({
        method: 'GET',
        url: '/api/identity/roles',
        params: { maxResultCount: '100' }
      }, { apiName: 'default' }).toPromise();

      const targetRole = (roleRes?.items || []).find((r: IdentityRoleDto) => r.name === roleName);
      
      if (!targetRole) {
        this.noti.warn(isEn ? `System role with code/name "${roleName}" was not found!` : `Không tìm thấy vai trò hệ thống có mã/tên là "${roleName}"!`);
        this.availableUsers = [];
        return;
      }

      const userRes: any = await this.restService.request({
        method: 'GET',
        url: '/api/identity/users',
        params: { maxResultCount: '100' }
      }, { apiName: 'default' }).toPromise();

      const allUsers: IdentityUserDto[] = userRes?.items || [];
      const filteredUsers: IdentityUserDto[] = [];

      for (const user of allUsers) {
        try {
          const rolesRes: any = await this.restService.request({
            method: 'GET',
            url: `/api/identity/users/${user.id}/roles`
          }, { apiName: 'default' }).toPromise();

          const userRoles = rolesRes?.items || rolesRes || [];
          const isInRole = userRoles.some((r: any) => (typeof r === 'string' ? r === roleName : r.name === roleName));

          if (isInRole) {
            filteredUsers.push(user);
          }
        } catch (e) {
          // Bỏ qua lỗi ngầm của user lẻ
        }
      }

      const currentMemberIds = (this.selectedDepartment?.members || []).map((m: any) => m.userId);
      this.availableUsers = filteredUsers.filter((u: IdentityUserDto) => !currentMemberIds.includes(u.id));
      
      this.isAssignModalOpen = true;
    } catch (err: any) {
      this.noti.error(err?.error?.error?.message || (isEn ? 'Could not load users by role!' : 'Không thể tải danh sách người dùng theo vai trò!'));
    }
  }

  saveAssignUser(): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');

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
        this.noti.success(isEn ? 'Member added to department successfully' : 'Thêm thành viên vào phòng ban thành công');
        this.closeAssignModal();
        const currentId = this.selectedDepartment?.id;
        if (currentId) {
          this.loadDepartmentTree(currentId);
          this.fetchDepartmentDetail(currentId);
        }
      },
      error: (err) => {
        this.noti.error(err.error?.error?.message || (isEn ? 'Could not add member!' : 'Không thể thêm thành viên!'));
      }
    });
  }

  toggleManager(member: any): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');

    if (!this.selectedDepartment?.id || !member.userId) return;

    const newManagerStatus = !member.isManager;
    const actionText = newManagerStatus 
      ? (isEn ? 'designate as manager' : 'chỉ định làm trưởng phòng') 
      : (isEn ? 'remove manager role' : 'bỏ vai trò trưởng phòng');

    const confirmMsg = isEn 
      ? `Are you sure you want to ${actionText} for account "${member.userName}"?` 
      : `Bạn có chắc chắn muốn ${actionText} cho tài khoản "${member.userName}"?`;

    this.confirmation
      .warn(confirmMsg, isEn ? 'Authorization confirmation' : 'Xác nhận phân quyền')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          const payload: AssignUserToDepartmentDto = {
            userId: member.userId,
            departmentId: this.selectedDepartment!.id,
            isManager: newManagerStatus
          };

          this.restService.request<AssignUserToDepartmentDto, void>({
            method: 'POST',
            url: '/api/app/department/assign-user',
            body: payload
          }, { apiName: 'default' }).subscribe({
            next: () => {
              this.noti.success(isEn ? 'Manager permission updated successfully' : 'Cập nhật quyền trưởng phòng thành công');
              const currentId = this.selectedDepartment?.id;
              if (currentId) {
                this.loadDepartmentTree(currentId);
                this.fetchDepartmentDetail(currentId);
              }
            },
            error: (err) => {
              this.noti.error(err.error?.error?.message || (isEn ? 'Could not update manager permission!' : 'Không thể cập nhật quyền trưởng phòng!'));
            }
          });
        }
      });
  }

  removeUserFromDept(userId: string): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');

    if (!this.selectedDepartment?.id) return;

    this.confirmation
      .warn(
        isEn ? 'Are you sure you want to remove this staff member from the department?' : 'Bạn có chắc chắn muốn xóa nhân sự này khỏi phòng ban?', 
        isEn ? 'Confirm staff removal' : 'Xác nhận gỡ nhân sự'
      )
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          const departmentId = this.selectedDepartment!.id;

          this.restService.request<void, void>({
            method: 'DELETE',
            url: `/api/app/department/user?departmentId=${departmentId}&userId=${userId}`
          }, { apiName: 'default' }).subscribe({
            next: () => {
              this.noti.success(isEn ? 'Staff removed from department' : 'Đã gỡ nhân sự khỏi phòng ban');
              const currentSelectedId = this.selectedDepartment?.id;
              if (currentSelectedId) {
                this.loadDepartmentTree(currentSelectedId);
                this.fetchDepartmentDetail(currentSelectedId);
              }
            },
            error: (err) => {
              this.noti.error(err.error?.error?.message || (isEn ? 'Could not remove member!' : 'Không thể xóa thành viên!'));
            }
          });
        }
      });
  }

  closeAssignModal(): void {
    this.isAssignModalOpen = false;
  }
}