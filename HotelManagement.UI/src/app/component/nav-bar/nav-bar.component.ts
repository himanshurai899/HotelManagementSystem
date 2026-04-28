import { Component, inject, computed, signal, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, Router } from '@angular/router';
import { Menu, menus } from '../../model/menus';
import { AuthService } from '../../services/auth.service';
import { ApiService } from '../../services/api.service';
import { TenantDTO } from '../../model/api.models';

/** A single item in the user-dropdown Settings section. */
export interface UserSettingsItem {
  /** Label shown in the menu. */
  label: string;
  /** Material icon ligature. */
  icon: string;
  /** Internal Angular route (e.g. '/profile'). Takes priority over externalUrl. */
  route?: string;
  /** Full external URL opened in a new tab when route is not set. */
  externalUrl?: string;
}

/**
 * Dropdown settings items shown above the Sign-Out divider.
 * Add, remove or reorder entries here — the template renders them automatically.
 */
export const userDropdownSettings: UserSettingsItem[] = [
  { label: 'My Profile',        icon: 'manage_accounts', route: '/profile' },
  { label: 'Account Settings',  icon: 'settings',        route: '/settings' },
  { label: 'Help & Support',    icon: 'help_outline',    externalUrl: 'https://support.example.com' },
];

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [
    MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule,
    MatDividerModule, MatSelectModule, MatFormFieldModule, MatTooltipModule,
    RouterLink
  ],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent implements OnInit {
  auth   = inject(AuthService);
  router = inject(Router);
  private api = inject(ApiService);

  /** All tenants — loaded once for SuperAdmin so the switcher shows names */
  tenants = signal<TenantDTO[]>([]);

  ngOnInit() {
    if (this.auth.hasRole('SuperAdmin')) {
      this.api.get<TenantDTO[]>('tenants').subscribe({
        next: t => this.tenants.set(t),
        error: () => this.tenants.set([])   // keep signal at [] on failure — dropdown still renders
      });
    }
  }

  /** Exposed to template so it can iterate the config array. */
  readonly settingsItems: UserSettingsItem[] = userDropdownSettings;

  /** Navigate to an internal route or open an external URL. */
  handleSettingsItem(item: UserSettingsItem): void {
    if (item.route) {
      this.router.navigate([item.route]);
    } else if (item.externalUrl) {
      window.open(item.externalUrl, '_blank', 'noopener,noreferrer');
    }
  }

  /** Filter nav items based on the current user's roles */
  visibleMenus = computed<Menu[]>(() => {
    this.auth.token(); // track token changes
    if (!this.auth.isLoggedIn()) return [];
    return menus.filter(m =>
      m.requiredRoles.length === 0 || this.auth.hasAnyRole(m.requiredRoles)
    );
  });

  username = computed(() => {
    this.auth.token();
    return this.auth.getUsername();
  });

  profilePhotoUrl = computed(() => {
    this.auth.token();
    return this.auth.profilePhotoUrl();
  });

  /** Primary role label shown beneath username in the dropdown */
  userRole = computed(() => {
    this.auth.token();
    const roles = this.auth.getRoles();
    if (roles.includes('SuperAdmin'))    return 'Super Admin';
    if (roles.includes('Administrator')) return 'Administrator';
    if (roles.includes('Customer'))      return 'Customer';
    if (roles.includes('Guest'))         return 'Guest';
    return roles[0] ?? '';
  });

  /** Single-letter initials fallback when no profile photo is set */
  avatarInitials = computed(() => {
    const name = this.auth.getUsername();
    return name ? name[0].toUpperCase() : '?';
  });

  logout() { this.auth.logout(); }

  isSuperAdmin = computed(() => { this.auth.token(); return this.auth.hasRole('SuperAdmin'); });

  /** The currently active tenant id (null = all tenants / no scope) */
  activeTenantId = computed(() => this.auth.selectedTenantId());

  /** Name of the active tenant for the banner */
  activeTenantName = computed(() => {
    const id = this.auth.selectedTenantId();
    const list = this.tenants();
    if (!list.length) return null;
    if (id == null) return list[0]?.name ?? null;
    const match = list.find(t => Number(t.id) === Number(id));
    return (match ?? list[0])?.name ?? null;
  });

  selectTenant(tenantId: number | null) { this.auth.setSelectedTenant(tenantId); }

  /** Clear the active tenant context from the banner */
  clearTenant() { this.auth.clearSelectedTenant(); }
}
