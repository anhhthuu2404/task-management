import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NotificationService, NotificationItem } from '../services/notification.service';

@Component({
  selector: 'app-navbar-notifications',
  standalone: true,
  imports: [CommonModule, NgbDropdownModule],
  template: `
    <li class="nav-item dropdown px-2" ngbDropdown>
      <a class="nav-link dropdown-toggle position-relative px-2 py-1 text-dark" href="javascript:void(0)" ngbDropdownToggle id="notificationDropdown" role="button" style="cursor: pointer;">
        <span style="font-size: 1.2rem;">🔔</span>
        <span *ngIf="unreadCount > 0" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 9px;">
          {{ unreadCount }}
        </span>
      </a>

      <div ngbDropdownMenu aria-labelledby="notificationDropdown" class="dropdown-menu dropdown-menu-end shadow-lg p-0 border-0" style="width: 320px; max-height: 400px; overflow-y: auto;">
        <div class="p-3 border-bottom d-flex justify-content-between align-items-center bg-light sticky-top">
          <h6 class="mb-0 fw-bold text-secondary" style="font-size: 14px;">Thông báo hệ thống</h6>
          <button class="btn btn-sm btn-link text-decoration-none text-muted p-0" style="font-size: 12px;" (click)="clearAll()">Xóa tất cả</button>
        </div>

        <div *ngIf="notifications.length === 0" class="text-center text-muted py-4 small">
          Không có thông báo mới
        </div>

        <div 
          *ngFor="let item of notifications" 
          class="p-3 border-bottom text-wrap position-relative bg-white" 
          [class.bg-light]="item.isRead"
          (click)="markAsRead(item.id)"
          style="cursor: pointer; transition: background 0.2s;">
          <div class="d-flex align-items-start">
            <div class="flex-grow-1">
              <div class="fw-semibold text-dark small mb-1" [class.text-muted]="item.isRead">{{ item.message }}</div>
              <div class="text-muted" style="font-size: 11px;">{{ item.time | date:'dd/MM/yyyy HH:mm' }}</div>
            </div>
            <div *ngIf="!item.isRead" class="ms-2">
              <span class="badge bg-primary rounded-circle p-1" style="width: 6px; height: 6px; display: inline-block;"></span>
            </div>
          </div>
        </div>
      </div>
    </li>
  `
})
export class NavbarNotificationsComponent {
  private readonly notificationService = inject(NotificationService);
  notifications: NotificationItem[] = [];

  constructor() {
    this.notificationService.notifications$.subscribe(res => {
      this.notifications = res || [];
    });
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  markAsRead(id?: string): void {
    if (id) {
      this.notificationService.markAsRead(id);
    }
  }

  clearAll(): void {
    this.notificationService.clearAll();
  }
}