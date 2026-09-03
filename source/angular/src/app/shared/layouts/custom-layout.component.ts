import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { NavbarNotificationsComponent } from '../components/navbar-notifications.component';

@Component({
  selector: 'app-custom-layout',
  standalone: true,
  imports: [CommonModule, DynamicLayoutComponent, NavbarNotificationsComponent],
  template: `
    <!-- Khung Navbar tùy chỉnh hiển thị cố định ở đầu trang để chứa chuông thông báo -->
    <nav class="navbar navbar-expand navbar-light bg-white border-bottom px-3 justify-content-between shadow-sm" style="height: 60px; position: sticky; top: 0; z-index: 1000;">
      <a class="navbar-brand fw-bold text-primary" href="javascript:void(0)">Task Management</a>
      
      <ul class="navbar-nav ms-auto align-items-center mb-0">
        <!-- Nhúng trực tiếp component chuông thông báo vào góc phải Navbar -->
        <app-navbar-notifications></app-navbar-notifications>
      </ul>
    </nav>

    <!-- Khu vực load nội dung các trang tính năng động của ABP -->
    <abp-dynamic-layout />
  `
})
export class CustomLayoutComponent {}