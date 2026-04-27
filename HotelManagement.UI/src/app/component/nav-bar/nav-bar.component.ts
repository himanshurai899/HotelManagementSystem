import { Component, inject, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatMenuModule } from '@angular/material/menu';
import { RouterLink } from '@angular/router';
import { Menu, menus } from '../../model/menus';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, RouterLink],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent {
  auth = inject(AuthService);

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

  /** Single-letter initials fallback when no profile photo is set */
  avatarInitials = computed(() => {
    const name = this.auth.getUsername();
    return name ? name[0].toUpperCase() : '?';
  });

  logout() { this.auth.logout(); }
}
