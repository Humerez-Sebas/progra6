import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
  { path: 'login',    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },

  { path: 'lobby',  canActivate: [authGuard], loadComponent: () => import('./features/lobby/lobby.component').then(m => m.LobbyComponent) },
  { path: 'rooms/:code', canActivate: [authGuard], loadComponent: () => import('./features/room/room.component').then(m => m.RoomComponent) },

  { path: '**', redirectTo: 'login' },
];
