import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'public' },
  {
    path: 'public',
    title: 'Workvivo',
    loadComponent: () => import('./features/public/public-page').then((m) => m.PublicPage),
  },
  {
    path: 'login',
    title: 'Sign in - Workvivo',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'home',
        title: 'Home - Workvivo',
        loadComponent: () => import('./features/home/home').then((m) => m.Home),
      },
      {
        path: 'feed',
        title: 'Feed - Workvivo',
        canActivate: [permissionGuard('Post.View')],
        loadComponent: () => import('./features/feed/feed-page').then((m) => m.FeedPage),
      },
      {
        path: 'employees',
        title: 'Directory - Workvivo',
        canActivate: [permissionGuard('Employee.View')],
        loadComponent: () =>
          import('./features/employees/employee-directory/employee-directory').then(
            (m) => m.EmployeeDirectory,
          ),
      },
      {
        // withComponentInputBinding (already configured) binds :employeeId straight to
        // the component's input, so no ActivatedRoute plumbing is needed.
        path: 'employees/:employeeId',
        title: 'Profile - Workvivo',
        canActivate: [permissionGuard('Employee.View')],
        loadComponent: () =>
          import('./features/employees/employee-profile/employee-profile').then(
            (m) => m.EmployeeProfilePage,
          ),
      },
      {
        // No permission guard: every employee manages their own notifications, and
        // gating that behind a grantable permission would let a misconfigured role
        // switch somebody's bell off with no way for them to turn it back on.
        path: 'settings/notifications',
        title: 'Notification settings - Workvivo',
        loadComponent: () =>
          import('./features/settings/notification-settings/notification-settings').then(
            (m) => m.NotificationSettings,
          ),
      },
      {
        path: 'profile',
        title: 'My profile - Workvivo',
        loadComponent: () =>
          import('./features/employees/employee-profile/employee-profile').then(
            (m) => m.EmployeeProfilePage,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'public' },
];
