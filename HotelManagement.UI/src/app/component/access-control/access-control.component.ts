import { Component, inject, computed } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../services/auth.service';
import { UsersTabComponent } from './users-tab/users-tab.component';
import { RolesTabComponent } from './roles-tab/roles-tab.component';
import { PermissionsTabComponent } from './permissions-tab/permissions-tab.component';

@Component({
  selector: 'app-access-control',
  standalone: true,
  imports: [
    MatTabsModule, MatIconModule, MatCardModule,
    UsersTabComponent, RolesTabComponent, PermissionsTabComponent
  ],
  templateUrl: './access-control.component.html',
  styleUrl: './access-control.component.scss'
})
export class AccessControlComponent {
  auth = inject(AuthService);
  isSuperAdmin = computed(() => this.auth.isSuperAdmin());
}
