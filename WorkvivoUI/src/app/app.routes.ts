import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';

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
    ],
  },
  { path: '**', redirectTo: 'public' },
];
