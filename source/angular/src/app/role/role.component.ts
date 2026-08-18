import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-role',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './role.component.html'
})
export class RoleComponent implements OnInit {
  roles: any[] = [];
  selectedRole: any = null;
  permissions: any[] = [];
  
  isRoleModalOpen = false;
  roleName = '';

  constructor(private http: HttpClient, private cd: ChangeDetectorRef) {}

  ngOnInit(): void { 
    this.loadRoles(); 
  }

  loadRoles(): void {
    this.http.get<any>('/api/identity/roles').subscribe({
      next: (res) => {
        this.roles = res.items || [];
        if (this.roles.length > 0 && !this.selectedRole) {
          this.selectRole(this.roles[0]);
        }
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách vai trò:', err)
    });
  }

  selectRole(role: any): void {
    this.selectedRole = role;
    this.http.get<any>(`/api/permission-management/permissions?providerName=R&providerKey=${role.name}`).subscribe({
      next: (res) => {
        const group = res.groups?.[0];
        this.permissions = group ? group.permissions : [];
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách quyền:', err)
    });
  }

  savePermissions(): void {
    if (!this.selectedRole) return;
    const payload = {
      permissions: this.permissions.map(p => ({ name: p.name, isGranted: p.isGranted }))
    };
    this.http.put(`/api/permission-management/permissions?providerName=R&providerKey=${this.selectedRole.name}`, payload).subscribe({
      next: () => {
        alert('Cập nhật ma trận phân quyền thành công!');
      },
      error: (err) => alert(err.error?.error?.message || 'Có lỗi khi lưu phân quyền!')
    });
  }

  openRoleModal(): void { 
    this.roleName = ''; 
    this.isRoleModalOpen = true; 
    this.cd.detectChanges(); 
  }

  saveRole(): void {
    if (!this.roleName || !this.roleName.trim()) {
      alert('Vui lòng nhập tên vai trò!');
      return;
    }
    
    const payload = {
      name: this.roleName.trim(),
      isDefault: false,
      isPublic: true
    };

    this.http.post('/api/identity/roles', payload).subscribe({
      next: () => {
        this.closeRoleModal();
        this.loadRoles();
      },
      error: (err) => {
        const errorMsg = err.error?.error?.message || err.error?.error?.details || 'Tên vai trò không hợp lệ hoặc đã tồn tại!';
        alert(errorMsg);
      }
    });
  }

  deleteRole(id: string): void {
    if (confirm('Xóa vai trò này khỏi hệ thống?')) {
      this.http.delete(`/api/identity/roles/${id}`).subscribe({
        next: () => {
          this.selectedRole = null;
          this.loadRoles();
        },
        error: (err) => alert(err.error?.error?.message || 'Không thể xóa vai trò này!')
      });
    }
  }

  closeRoleModal(): void { 
    this.isRoleModalOpen = false; 
    this.cd.detectChanges(); 
  }
}