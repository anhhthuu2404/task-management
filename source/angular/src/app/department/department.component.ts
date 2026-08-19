import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RestService } from '@abp/ng.core';
import { DepartmentService, DepartmentDto, CreateUpdateDepartmentDto } from '../proxy/departments';

export interface IdentityUserDto { id: string; userName: string; email: string; name?: string; surname?: string; }
export interface AssignUserDto { userId: string; departmentId: string; isManager: boolean; }

@Component({
  selector: 'app-department',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './department.component.html'
})
export class DepartmentComponent implements OnInit {
  departments: DepartmentDto[] = [];
  selectedDepartment: any | null = null;
  
  isModalOpen = false;
  isEditMode = false;
  formData: CreateUpdateDepartmentDto = { code: '', name: '', description: '', parentId: undefined, isActive: true };
  
  isAssignModalOpen = false;
  availableUsers: IdentityUserDto[] = [];
  assignData = { userId: '', isManager: false };

  constructor(
    private departmentService: DepartmentService,
    private restService: RestService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDepartmentTree();
  }

  loadDepartmentTree(preserveSelectionId?: string): void {
    this.departmentService.getTree({ skipHandleError: true }).subscribe({
      next: (data: any) => {
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

        this.cd.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  selectDepartment(node: any): void {
    this.selectedDepartment = node;
    if (node && node.id) {
      this.fetchDepartmentDetail(node.id);
    } else {
      this.cd.detectChanges();
    }
  }

  private fetchDepartmentDetail(id: string): void {
    this.restService.request<any, any>({
      method: 'GET',
      url: `/api/app/department/${id}`
    }, { apiName: 'default' }).subscribe({
      next: (detailRes) => {
        if (detailRes && this.selectedDepartment && this.selectedDepartment.id === id) {
          this.selectedDepartment.members = detailRes.members || [];
        }
        this.cd.detectChanges();
      },
      error: (err) => {
        console.error('Không thể tải chi tiết phòng ban', err);
      }
    });
  }

  private findDepartmentInTree(list: any[], id: string): any | null {
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
    this.isModalOpen = true;
    this.cd.detectChanges();
  }

  openEditModal(node: DepartmentDto): void {
    this.isEditMode = true;
    this.selectedDepartment = node;
    this.formData = { code: node.code ?? '', name: node.name ?? '', description: node.description ?? '', parentId: node.parentId ?? undefined, isActive: true };
    this.isModalOpen = true;
    this.cd.detectChanges();
  }

  saveDepartment(): void {
    const req = this.isEditMode && this.selectedDepartment?.id && !this.formData.parentId
      ? this.departmentService.update(this.selectedDepartment.id, this.formData, { skipHandleError: true })
      : this.departmentService.create(this.formData, { skipHandleError: true });

    req.subscribe({
      next: () => { 
        this.closeModal(); 
        this.loadDepartmentTree(); 
      },
      error: (err) => alert(err.error?.error?.message || 'Có lỗi xảy ra!')
    });
  }

  deleteDepartment(id: string): void {
    if (confirm('Bạn có chắc chắn muốn xóa phòng ban này cùng các phòng ban con?')) {
      this.departmentService.delete(id, { skipHandleError: true }).subscribe(() => {
        this.loadDepartmentTree();
      });
    }
  }

  closeModal(): void { 
    this.isModalOpen = false; 
    this.cd.detectChanges(); 
  }

  openAssignModal(): void {
    if (!this.selectedDepartment) return;
    this.assignData = { userId: '', isManager: false };
    
    this.restService.request<{ items: IdentityUserDto[] }, any>({
      method: 'GET',
      url: '/api/identity/users',
      params: { maxResultCount: '100' }
    }, { apiName: 'default' }).subscribe({
      next: (res) => {
        this.availableUsers = res.items || [];
        this.isAssignModalOpen = true;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.error('Lỗi khi tải danh sách người dùng:', err);
        alert(err.error?.error?.message || 'Không thể tải danh sách người dùng. Vui lòng kiểm tra lại quyền!');
      }
    });
  }

  saveAssignUser(): void {
    if (!this.selectedDepartment?.id || !this.assignData.userId) return;
    
    const payload: AssignUserDto = { 
      userId: String(this.assignData.userId), 
      departmentId: String(this.selectedDepartment.id), 
      isManager: Boolean(this.assignData.isManager) 
    };

    this.restService.request<AssignUserDto, void>({ 
      method: 'POST', 
      url: '/api/app/department/assign-user', 
      body: payload 
    }, { apiName: 'default' }).subscribe({
      next: () => {
        this.closeAssignModal();
        const currentId = this.selectedDepartment?.id;
        if (currentId) {
          this.loadDepartmentTree(currentId);
          this.fetchDepartmentDetail(currentId);
        }
      },
      error: (err) => {
        console.error('Lỗi API gán user:', err);
        alert(err.error?.error?.message || 'Không thể thêm thành viên!');
      }
    });
  }

  removeUserFromDept(userId: string): void {
    if (!this.selectedDepartment?.id) return;
    
    if (confirm('Bạn có chắc chắn muốn xóa nhân sự này khỏi phòng ban?')) {
      const departmentId = this.selectedDepartment.id;

      // Theo quy ước định tuyến của ABP, action DeleteUserAsync sẽ mapping sang endpoint dạng delete-user
      this.restService.request<void, void>({ 
        method: 'DELETE', 
        url: `/api/app/department/user?departmentId=${departmentId}&userId=${userId}`
      }, { apiName: 'default' }).subscribe({
        next: () => {
          const currentSelectedId = this.selectedDepartment?.id;
          if (currentSelectedId) {
            this.loadDepartmentTree(currentSelectedId);
            this.fetchDepartmentDetail(currentSelectedId);
          }
        },
        error: (err) => {
          console.error('Lỗi khi xóa thành viên:', err);
          alert(err.error?.error?.message || 'Không thể xóa thành viên!');
        }
      });
    }
  }

  closeAssignModal(): void { 
    this.isAssignModalOpen = false; 
    this.cd.detectChanges(); 
  }
}