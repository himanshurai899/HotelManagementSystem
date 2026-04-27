import { Component, computed, inject } from '@angular/core';
import {
    Router, RouterModule, RouterLink, RouterLinkActive, NavigationEnd
} from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, filter, startWith } from 'rxjs/operators';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { Menu, MenuGroup, menus, categoryMeta } from '../../model/menus';
import { AuthService } from '../../services/auth.service';

const AUTH_ROUTES = ['/login', '/register'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterModule, RouterLink, RouterLinkActive,
    MatSidenavModule, MatToolbarModule, MatButtonModule,
    MatIconModule, MatMenuModule, MatDividerModule
  ],
  templateUrl: './root.component.html',
  styleUrl: './root.component.scss'
})
export class RootComponent {
  auth = inject(AuthService);
  private bp = inject(BreakpointObserver);
  private router = inject(Router);

  /** True when the current route is login or register */
  isAuthRoute = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map((e: NavigationEnd) => AUTH_ROUTES.some(r => e.urlAfterRedirects.startsWith(r))),
      startWith(AUTH_ROUTES.some(r => this.router.url.startsWith(r)))
    ),
    { initialValue: AUTH_ROUTES.some(r => this.router.url.startsWith(r)) }
  );

  /** True on mobile/tablet — sidenav becomes overlay mode */
  isHandset = toSignal(
    this.bp.observe([Breakpoints.Handset, Breakpoints.Tablet])
      .pipe(map(r => r.matches)),
    { initialValue: false }
  );

  /** Filter nav items by current user roles */
  visibleMenus = computed<Menu[]>(() => {
    this.auth.token(); // track token changes
    if (!this.auth.isLoggedIn()) return [];
    return menus.filter(m =>
      m.requiredRoles.length === 0 || this.auth.hasAnyRole(m.requiredRoles)
    );
  });

  /** Menus grouped by category, preserving definition order */
  groupedMenus = computed<MenuGroup[]>(() => {
    const visible = this.visibleMenus();
    const order: string[] = [];
    const map = new Map<string, Menu[]>();
    for (const m of visible) {
      if (!map.has(m.category)) { map.set(m.category, []); order.push(m.category); }
      map.get(m.category)!.push(m);
    }
    return order.map(label => ({
      label,
      icon: categoryMeta[label]?.icon ?? 'folder',
      items: map.get(label)!
    }));
  });

  username = computed(() => {
    this.auth.token();
    return this.auth.getUsername() ?? '';
  });

  /** Displays FirstName + LastName when available, falls back to username */
  displayName = computed(() => {
    this.auth.token();
    this.auth.fullName(); // track changes
    return this.auth.getFullName() || this.auth.getUsername() || '';
  });

  profilePhotoUrl = computed(() => {
    this.auth.token();
    return this.auth.profilePhotoUrl();
  });

  initials = computed(() => {
    const name = this.displayName();
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  });

  primaryRole = computed(() => {
    this.auth.token();
    const roles = this.auth.getRoles();
    if (roles.includes('SuperAdmin')) return 'SuperAdmin';
    if (roles.includes('Administrator')) return 'Administrator';
    if (roles.includes('Customer')) return 'Customer';
    if (roles.includes('Guest')) return 'Guest';
    return roles[0] ?? '';
  });

  logout() { this.auth.logout(); }

  closeOnHandset(sidenav: MatSidenav) {
    if (this.isHandset()) sidenav.close();
  }
}
