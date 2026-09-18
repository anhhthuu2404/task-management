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
      name: '::Menu:Categories',
      iconClass: 'fas fa-folder',
      order: 2,
      layout: eLayoutType.application,
    },
    {
      path: '/tags',
      name: '::Menu:Tags',
      iconClass: 'fas fa-tags',
      order: 3,
      layout: eLayoutType.application,
    },
    {
      path: '/departments',
      name: '::Menu:Departments',
      iconClass: 'fas fa-sitemap',
      order: 7,
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
      name: '::Menu:Users',
      iconClass: 'fas fa-users',
      order: 6,
      layout: eLayoutType.application,
    },
    {
      path: '/roles',
      name: '::Menu:Roles',
      iconClass: 'fas fa-user-shield',
      order: 4,
      layout: eLayoutType.application,
    },
    {
      path: '/projects',
      name: '::Menu:Projects',
      iconClass: 'fas fa-project-diagram',
      order: 8,
      layout: eLayoutType.application,
    },
    {
      path: '/tasks/list',
      name: '::Menu:Tasks',
      iconClass: 'fas fa-tasks',
      order: 9, // Đã đưa ra ngoài cấp cao nhất, bỏ parentName
      layout: eLayoutType.application,
    },
   /* {
      path: '/tasks/create',
      name: '::Tạo công việc mới',
      iconClass: 'fas fa-plus',
      order: 10, // Đã đưa ra ngoài cấp cao nhất, bỏ parentName
      layout: eLayoutType.application,
    },*/
    {
      path: '/reports',
      name: '::Menu:Reports',
      iconClass: 'fas fa-chart-bar',
      order: 10,
      layout: eLayoutType.application,
    },
    {
      path: '/dashboard',
      name: '::Menu:SystemOverview',
      iconClass: 'fas fa-chart-pie',
      order: 11, 
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