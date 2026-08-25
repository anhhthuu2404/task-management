import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';

export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'tenant-management',
    loadChildren: () => import('@abp/ng.tenant-management').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
  {
    path: 'books',
    loadComponent: () => import('./book/book.component').then(c => c.BookComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    path: 'language-texts',
    loadComponent: () => import('./language-management/language-texts/language-text.component').then(c => c.LanguageTextComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    path: 'sys-master-lists',
    loadComponent: () => import('./sys-master-lists/sys-mastert-list.component').then(c => c.SysMasterListComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    path: 'categories',
    loadComponent: () => import('./category/category.component').then(m => m.CategoryComponent), 
    canActivate: [authGuard],
  },
  {
    path: 'tags',
    loadComponent: () => import('./tag/tag.component').then(m => m.TagComponent),
    canActivate: [authGuard],
  },
  {
    path: 'departments',
    loadComponent: () => import('./department/department.component').then(m => m.DepartmentComponent),
    canActivate: [authGuard], 
  },
  {
    path: 'users',
    loadComponent: () => import('./user/user.component').then(m => m.UserComponent),
    canActivate: [authGuard],
  },
  {
    path: 'roles',
    loadComponent: () => import('./role/role.component').then(m => m.RoleComponent), 
    canActivate: [authGuard],
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'list',
        pathMatch: 'full',
      },
      {
        path: 'list', 
        loadComponent: () => import('./tasks/task-list.component').then(m => m.TaskListComponent),
      },
      {
        path: 'create',
        loadComponent: () => import('./tasks/create-task.component').then(m => m.CreateTaskComponent),
      },
      {
        path: 'edit/:id',
        loadComponent: () => import('./tasks/task-form.component').then(m => m.TaskFormComponent),
      },
      {
        path: 'detail/:id',
        loadComponent: () => import('./tasks/task-detail.component').then(m => m.TaskDetailComponent),
      },
      
    ],
  },
];