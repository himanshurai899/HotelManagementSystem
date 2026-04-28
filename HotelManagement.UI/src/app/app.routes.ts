import { Routes } from '@angular/router';
import { DashboardComponent } from './component/dashboard/dashboard.component';
import { ErrorComponent } from './component/error/error.component';
import { LoginComponent } from './component/auth/login/login.component';
import { RegisterComponent } from './component/auth/register/register.component';
import { AdminDashboardComponent } from './component/dashboards/admin-dashboard/admin-dashboard.component';
import { CustomerDashboardComponent } from './component/dashboards/customer-dashboard/customer-dashboard.component';
import { GuestDashboardComponent } from './component/dashboards/guest-dashboard/guest-dashboard.component';
import { RoomsComponent } from './component/rooms/rooms.component';
import { RoomTypesComponent } from './component/room-types/room-types.component';
import { AmenitiesComponent } from './component/amenities/amenities.component';
import { StaffComponent } from './component/staff/staff.component';
import { UsersComponent } from './component/users/users.component';
import { AccessControlComponent } from './component/access-control/access-control.component';
import { BookingsComponent } from './component/bookings/bookings.component';
import { BrowseRoomsComponent } from './component/browse-rooms/browse-rooms.component';
import { UnauthorizedComponent } from './component/unauthorized/unauthorized.component';
import { ProfileComponent } from './component/profile/profile.component';
import { InvoicesComponent } from './component/invoices/invoices.component';
import { PaymentsComponent } from './component/payments/payments.component';
import { CompanyProfileComponent } from './component/company-profile/company-profile.component';
import { TenantsComponent } from './component/tenants/tenants.component';
import { BookingCalendarComponent } from './component/booking-calendar/booking-calendar.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

export const routes: Routes = [
  // Public
  { path: 'login',       component: LoginComponent },
  { path: 'register',    component: RegisterComponent },
  { path: 'browse-rooms', component: BrowseRoomsComponent },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: 'unauthorized', component: UnauthorizedComponent },
  { path: 'error',       component: ErrorComponent },

  // Role dispatch hub — redirects to correct dashboard
  { path: '', component: DashboardComponent },

  // Admin-only dashboards & pages
  { path: 'admin-dashboard', component: AdminDashboardComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },
  { path: 'rooms',      component: RoomsComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },
  { path: 'room-types', component: RoomTypesComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },
  { path: 'amenities',  component: AmenitiesComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },
  { path: 'staff',      component: StaffComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },

  // Unified Access Control — Users + Roles + Permissions in one screen
  { path: 'access-control', component: AccessControlComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },
  // Legacy alias — keeps any bookmarked /users links working
  { path: 'users', component: UsersComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },

  // Customer dashboard
  { path: 'customer-dashboard', component: CustomerDashboardComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Customer', 'Administrator', 'SuperAdmin'] } },

  // Guest dashboard
  { path: 'guest-dashboard', component: GuestDashboardComponent,
    canActivate: [authGuard] },

  // Shared: bookings (Customer + Admin + SuperAdmin)
  { path: 'bookings', component: BookingsComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Customer', 'Administrator', 'SuperAdmin'] } },

  // Booking Calendar — Admin/SuperAdmin room availability view
  { path: 'booking-calendar', component: BookingCalendarComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },

  // Billing & Payments (Phase 8)
  { path: 'invoices', component: InvoicesComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Customer', 'Administrator', 'SuperAdmin'] } },
  { path: 'payments', component: PaymentsComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Customer', 'Administrator', 'SuperAdmin'] } },
  { path: 'company-profile', component: CompanyProfileComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['Administrator', 'SuperAdmin'] } },

  // SuperAdmin — Tenant / Company management
  { path: 'tenants', component: TenantsComponent,
    canActivate: [authGuard, roleGuard], data: { roles: ['SuperAdmin'] } },

  // Fallback
  { path: '**', redirectTo: '' }
];
