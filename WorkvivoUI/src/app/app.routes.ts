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
        path: 'admin',
        title: 'Administration - Workvivo',
        canActivate: [permissionGuard('Role.Manage')],
        loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
      },
      {
        path: 'analytics',
        title: 'Analytics - Workvivo',
        canActivate: [permissionGuard('Analytics.View')],
        loadComponent: () =>
          import('./features/analytics/analytics-page').then((m) => m.AnalyticsPage),
      },
      {
        // ?q= binds straight to the component's input, so the URL is the state and a
        // search result page can be shared or bookmarked.
        path: 'search',
        title: 'Search - Workvivo',
        loadComponent: () => import('./features/search/search-page').then((m) => m.SearchPage),
      },
      {
        path: 'documents',
        title: 'Documents - Workvivo',
        canActivate: [permissionGuard('Document.View')],
        loadComponent: () =>
          import('./features/documents/documents-page').then((m) => m.DocumentsPage),
      },
      {
        path: 'events',
        title: 'Events - Workvivo',
        loadComponent: () => import('./features/events/events-page').then((m) => m.EventsPage),
      },
      {
        path: 'pulse',
        title: 'Polls and surveys - Workvivo',
        loadComponent: () => import('./features/pulse/pulse-page').then((m) => m.PulsePage),
      },
      {
        path: 'recognition',
        title: 'Recognition - Workvivo',
        loadComponent: () =>
          import('./features/recognition/recognition-page').then((m) => m.RecognitionPage),
      },
      {
        path: 'communities',
        title: 'Communities - Workvivo',
        loadComponent: () =>
          import('./features/communities/community-list/community-list').then(
            (m) => m.CommunityList,
          ),
      },
      {
        path: 'communities/:communityId',
        title: 'Community - Workvivo',
        loadComponent: () =>
          import('./features/communities/community-detail/community-detail').then(
            (m) => m.CommunityDetailPage,
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
