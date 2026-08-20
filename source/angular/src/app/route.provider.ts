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
      path: '/books',
      name: '::Menu:Books',
      iconClass: 'fas fa-book',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Books',
    },
    {
      path: '/categories',
      name: 'Quản lý Danh mục',
      iconClass: 'fas fa-folder',
      order: 3,
      layout: eLayoutType.application,
    },
    {
      path: '/tags',
      name: 'Quản lý Thẻ (Tag)',
      iconClass: 'fas fa-tags',
      order: 4,
      layout: eLayoutType.application,
    },
    {
      path: '/departments',
      name: 'Quản lý Phòng ban',
      iconClass: 'fas fa-sitemap',
      order: 5,
      layout: eLayoutType.application,
    },
    {
      path: '/users',
      name: 'Quản lý Người dùng',
      iconClass: 'fas fa-users',
      order: 6,
      layout: eLayoutType.application,
    },
    {
      path: '/roles',
      name: 'Vai trò & Phân quyền',
      iconClass: 'fas fa-user-shield',
      order: 7,
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/create',
      name: 'Quản lý Nhiệm vụ',
      iconClass: 'fas fa-tasks',
      order: 8,
      layout: eLayoutType.application,
      
    },
    {
      path: '/tasks',
      name: 'Danh sách Nhiệm vụ',
      iconClass: 'fas fa-list-check',
      order: 9,
      layout: eLayoutType.application,
    },
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