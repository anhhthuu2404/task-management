import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RestService } from '@abp/ng.core';
import { DepartmentService, DepartmentDto, CreateUpdateDepartmentDto } from '../proxy/departments';

export interface IdentityUserDto { id: string; userName: string; email: string; }
export interface AssignUserDto { userId: string; departmentId: string; isManager: boolean; }

@Component({
  selector: 'app-department',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './department.component.html'
})
export class DepartmentComponent implements OnInit {
  departments: DepartmentDto[] = [];
  selectedDepartment: DepartmentDto | null = null;
  isModalOpen = false;
  isEditMode = false;
  formData: CreateUpdateDepartmentDto = { code: '', name: '', description: '', parentId: undefined, isActive: true };
  
  isAssignModalOpen = false;
  availableUsers: IdentityUserDto[] = [];
  assignData = { userId: '', isManager: false };

  constructor(
    private departmentService: DepartmentService,
    private http: HttpClient,
    private restService: RestService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.loadDepartmentTree(); }

  loadDepartmentTree(): void {
    this.departmentService.getTree({ skipHandleError: true }).subscribe({
      next: (data: any) => { this.departments = data || []; this.cd.detectChanges(); },
      error: (err) => console.error(err)
    });
  }

  openCreateModal(parentId?: string | null): void {
    this.isEditMode = false;
    this.formData = { code: '', name: '', parentId: parentId ?? undefined, isActive: true };
    this.isModalOpen = true;
    this.cd.detectChanges();
  }

  openEditModal(node: DepartmentDto): void {
    this.isEditMode = true;
    this.selectedDepartment = node;
    this.formData = { code: node.code ?? '', name: node.name ?? '', parentId: node.parentId ?? undefined, isActive: true };
    this.isModalOpen = true;
    this.cd.detectChanges();
  }

  saveDepartment(): void {
    const req = this.isEditMode && this.selectedDepartment?.id
      ? this.departmentService.update(this.selectedDepartment.id, this.formData, { skipHandleError: true })
      : this.departmentService.create(this.formData, { skipHandleError: true });

    req.subscribe({
      next: () => { this.closeModal(); this.loadDepartmentTree(); },
      error: (err) => alert(err.error?.error?.message || 'Có lỗi xảy ra!')
    });
  }

  deleteDepartment(id: string): void {
    if (confirm('Bạn có chắc chắn muốn xóa phòng ban này?')) {
      this.departmentService.delete(id, { skipHandleError: true }).subscribe(() => this.loadDepartmentTree());
    }
  }

  closeModal(): void { this.isModalOpen = false; this.cd.detectChanges(); }

  openAssignModal(department: DepartmentDto): void {
    this.selectedDepartment = department;
    this.assignData = { userId: '', isManager: false };
    this.http.get<{ items: IdentityUserDto[] }>('/api/identity/users?maxResultCount=100').subscribe(res => {
      this.availableUsers = res.items || [];
      this.isAssignModalOpen = true;
      this.cd.detectChanges();
    });
  }

  saveAssignUser(): void {
    if (!this.selectedDepartment?.id || !this.assignData.userId) return;
    const payload: AssignUserDto = { userId: this.assignData.userId, departmentId: this.selectedDepartment.id, isManager: this.assignData.isManager };
    this.restService.request<AssignUserDto, void>({ method: 'POST', url: '/api/app/department/assign-user', body: payload }, { apiName: 'default' }).subscribe(() => {
      this.closeAssignModal();
      this.loadDepartmentTree();
    });
  }

  removeUserFromDept(departmentId: string, userId: string): void {
    if (confirm('Xóa người dùng này khỏi phòng ban?')) {
      this.restService.request<any, void>({ method: 'DELETE', url: `/api/app/department/remove-user?departmentId=${departmentId}&userId=${userId}` }, { apiName: 'default' }).subscribe(() => this.loadDepartmentTree());
    }
  }

  closeAssignModal(): void { this.isAssignModalOpen = false; this.cd.detectChanges(); }
}