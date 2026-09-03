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
      requiredPolicy: 'TaskManagement.Categories',
    },
    {
      path: '/tags',
      name: '::Quản lý Thẻ (Tag)',
      iconClass: 'fas fa-tags',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Tags',
    },
    {
      path: '/departments',
      name: '::Quản lý Phòng ban',
      iconClass: 'fas fa-sitemap',
      order: 4,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Departments',
    },
    {
      path: '/books',
      name: '::Menu:Books',
      iconClass: 'fas fa-book',
      order: 5,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Books',
    },
    {
      path: '/users',
      name: '::Quản lý Người dùng',
      iconClass: 'fas fa-users',
      order: 6,
      layout: eLayoutType.application,
      requiredPolicy: 'AbpIdentity.Users',
    },
    {
      path: '/roles',
      name: '::Vai trò & Phân quyền',
      iconClass: 'fas fa-user-shield',
      order: 7,
      layout: eLayoutType.application,
      requiredPolicy: 'AbpIdentity.Roles',
    },
    // --- THÊM MỚI: QUẢN LÝ DỰ ÁN ---
    {
      path: '/projects',
      name: '::Quản lý Dự án',
      iconClass: 'fas fa-project-diagram',
      order: 8,
      layout: eLayoutType.application,
    },
    // ---------------------------------
    // --- MENU QUẢN LÝ CÔNG VIỆC CHÍNH ---
    {
      path: '/tasks',
      name: '::Quản lý Công việc',
      iconClass: 'fas fa-tasks',
      order: 9,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Tasks', 
    },
    {
      path: '/tasks/list',
      name: '::Danh sách công việc',
      iconClass: 'fas fa-list-check',
      parentName: '::Quản lý Công việc',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/create',
      name: '::Tạo công việc mới',
      iconClass: 'fas fa-plus',
      parentName: '::Quản lý Công việc',
      order: 2, 
      layout: eLayoutType.application,
    },
    // ------------------------------------
    {
      path: '/language-texts',
      name: '::Menu:LanguageTexts',
      iconClass: 'fa fa-language',
      layout: eLayoutType.application,
      parentName: 'AbpUiNavigation::Menu:Administration',
      requiredPolicy: 'TaskManagement.LanguageTexts',
      order: 100,
    },
    {
      path: '/sys-master-lists',
      name: '::Menu:SysMasterLists',
      iconClass: 'fa fa-list',
      layout: eLayoutType.application,
      parentName: 'AbpUiNavigation::Menu:Administration',
      requiredPolicy: 'TaskManagement.SysMasterLists',
      order: 101,
    },
  ]);
}