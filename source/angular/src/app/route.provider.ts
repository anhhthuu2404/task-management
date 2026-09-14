import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/',
      name: '::Menu:Home',
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/categories',
      name: '::Quản lý Danh mục',
      iconClass: 'fas fa-folder',
      order: 2,
      layout: eLayoutType.application,
    },
    {
      path: '/tags',
      name: '::Quản lý Thẻ (Tag)',
      iconClass: 'fas fa-tags',
      order: 3,
      layout: eLayoutType.application,
    },
    {
      path: '/departments',
      name: '::Quản lý Phòng ban',
      iconClass: 'fas fa-sitemap',
      order: 4,
      layout: eLayoutType.application,
    },
    {
      path: '/books',
      name: '::Menu:Books',
      iconClass: 'fas fa-book',
      order: 5,
      requiredPolicy: 'BookStore.Books',
    },
    {
      path: '/users',
      name: '::Quản lý Người dùng',
      iconClass: 'fas fa-users',
      order: 6,
      layout: eLayoutType.application,
    },
    {
      path: '/roles',
      name: '::Vai trò & Phân quyền',
      iconClass: 'fas fa-user-shield',
      order: 7,
      layout: eLayoutType.application,
    },
    {
      path: '/projects',
      name: '::Quản lý Dự án',
      iconClass: 'fas fa-project-diagram',
      order: 8,
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/list',
      name: '::Quản lý Công việc',
      iconClass: 'fas fa-tasks',
      order: 9, // Đã đưa ra ngoài cấp cao nhất, bỏ parentName
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/create',
      name: '::Tạo công việc mới',
      iconClass: 'fas fa-plus',
      order: 10, // Đã đưa ra ngoài cấp cao nhất, bỏ parentName
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/detail',
      name: '::Chi tiết công việc',
      iconClass: 'fas fa-info-circle',
      order: 11,
      layout: eLayoutType.application,
    },
    {
      path: '/reports',
      name: '::Báo cáo & Thống kê',
      iconClass: 'fas fa-chart-bar',
      order: 12,
      layout: eLayoutType.application,
    },
    {
      path: '/dashboard',
      name: '::Tổng quan hệ thống',
      iconClass: 'fas fa-chart-pie',
      order: 13, 
      layout: eLayoutType.application,
    },
    {
      path: '/language-texts',
      name: '::Menu:LanguageTexts',
      iconClass: 'fa fa-language',
      layout: eLayoutType.application,
      parentName: 'AbpUiNavigation::Menu:Administration',
      order: 100,
    },
    {
      path: '/sys-master-lists',
      name: '::Menu:SysMasterLists',
      iconClass: 'fa fa-list',
      layout: eLayoutType.application,
      parentName: 'AbpUiNavigation::Menu:Administration',
      order: 101,
    },
  ]);
}