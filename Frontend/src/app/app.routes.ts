
import { ManagerDashboard } from './features/manager/dashboard/dashboard';
import { EmployeeLayout } from './layout/employee-layout/employee-layout';

import { managerGuard } from './core/manager.guard';
import { Register } from './features/auth/register/register';
import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';

import {
  MyTickets,
} from './features/tickets/my-tickets/my-tickets';

import {
  NewTicket,
} from './features/tickets/new-ticket/new-ticket';

import {
  EditTicket,
} from './features/tickets/edit-ticket/edit-ticket';

import {
  TicketDetailsPage,
} from './features/tickets/ticket-details/ticket-details';

import {
  AssignedTickets,
} from './features/tickets/assigned-tickets/assigned-tickets';

import {
  EmployeeTicket,
} from './features/tickets/employee-ticket/employee-ticket';

import {
  ClientLayout,
} from './layout/client-layout/client-layout';

import { clientGuard } from './core/auth.guard';
import { employeeGuard } from './core/employee.guard';

export const routes: Routes = [
  { path: 'confirm-email', loadComponent: () => import('./features/account/confirm-email').then(m => m.ConfirmEmail) },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/password-recovery/password-recovery').then(m => m.PasswordRecovery), data: { reset: false } },
  { path: 'reset-password', loadComponent: () => import('./features/auth/password-recovery/password-recovery').then(m => m.PasswordRecovery), data: { reset: true } },
  { path: 'manager/tickets/:id', loadComponent: () => import('./features/manager/ticket-details/ticket-details').then(m => m.ManagerTicketDetails), canActivate: [managerGuard] },
  { path: 'manager/products', loadComponent: () => import('./features/manager/products/products').then(m => m.ManagerProducts), canActivate: [managerGuard] },
  { path: 'manager/employees/new', loadComponent: () => import('./features/manager/new-employee/new-employee').then(m => m.NewEmployee), canActivate: [managerGuard] },
  { path: 'manager/employees', loadComponent: () => import('./features/manager/employees/employees').then(m => m.ManagerEmployees), canActivate: [managerGuard] },
  { path: 'manager/clients', loadComponent: () => import('./features/manager/clients/clients').then(m => m.ManagerClients), canActivate: [managerGuard] },
  { path: 'manager/dashboard', component: ManagerDashboard, canActivate: [managerGuard] },
  { path: 'manager/tickets', loadComponent: () => import('./features/manager/tickets/manager-tickets').then(m => m.ManagerTickets), canActivate: [managerGuard] },
  { path: 'register', component: Register },
  {
    path: '',
    redirectTo: 'my-tickets',
    pathMatch: 'full',
  },
  {
    path: 'login',
    component: Login,
  },
  {
    path: 'my-tickets',
    component: ClientLayout,
    canActivate: [clientGuard],
    canActivateChild: [clientGuard],

    children: [
      { path: 'account', loadComponent: () => import('./features/account/account').then(m => m.AccountPage) },
      { path: 'company', loadComponent: () => import('./features/company/company').then(m => m.CompanyPage) },
      { path: 'systems', loadComponent: () => import('./features/systems/systems').then(m => m.ClientSystems) },
      {
        path: '',
        component: MyTickets,
      },
      {
        path: 'new',
        component: NewTicket,
      },
      {
        path: ':id/edit',
        component: EditTicket,
      },
      {
        path: ':id',
        component: TicketDetailsPage,
      },
    ],
  },
  { path: 'assigned-tickets', component: EmployeeLayout, canActivate: [employeeGuard], canActivateChild: [employeeGuard], children: [
      { path: '', component: AssignedTickets },
      { path: ':id', component: EmployeeTicket },
    ] },
  {
    path: '**',
    redirectTo: 'my-tickets',
  },
];












