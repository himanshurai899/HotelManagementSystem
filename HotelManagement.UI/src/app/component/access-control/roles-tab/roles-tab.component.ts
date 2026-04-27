import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { RoleDTO, PermissionDTO } from '../../../model/api.models';

@Component({
  selector: 'app-roles-tab',
  standalone: true,
  imports: [
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSnackBarModule,
    MatProgressSpinnerModule, MatExpansionModule, MatTooltipModule, FormsModule
  ],
  templateUrl: './roles-tab.component.html',
  styleUrl: './roles-tab.component.scss'
})
export class RolesTabComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  auth = inject(AuthService);

  roles = signal<RoleDTO[]>([]);
  allPermissions = signal<PermissionDTO[]>([]);
  loading = signal(true);
  showCreate = signal(false);
  newRoleName = '';

  isSuperAdmin = computed(() => this.auth.isSuperAdmin());

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<RoleDTO[]>('roles').subscribe({
      next: r => { this.roles.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.api.get<PermissionDTO[]>('permissions').subscribe({
      next: p => this.allPermissions.set(p)
    });
  }

  createRole() {
    if (!this.newRoleName.trim()) return;
    this.api.post<any>('roles', JSON.stringify(this.newRoleName)).subscribe({
      next: () => {
        this.snack.open(`Role '${this.newRoleName}' created!`, '', { duration: 2000 });
        this.newRoleName = '';
        this.showCreate.set(false);
        this.load();
      },
      error: (e) => this.snack.open(e?.error ?? 'Error creating role.', 'Dismiss', { duration: 4000 })
    });
  }

  deleteRole(id: number, name: string) {
    if (!confirm(`Delete role '${name}'? This cannot be undone.`)) return;
    this.api.delete(`roles/${id}`).subscribe({
      next: () => { this.snack.open('Role deleted.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error deleting role.', 'Dismiss', { duration: 3000 })
    });
  }

  addPermission(roleId: number, perm: string) {
    this.api.post(`roles/${roleId}/permissions`, JSON.stringify(perm)).subscribe({
      next: () => { this.snack.open(`Permission '${perm}' added.`, '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error adding permission.', 'Dismiss', { duration: 3000 })
    });
  }

  removePermission(roleId: number, perm: string) {
    this.api.delete(`roles/${roleId}/permissions/${perm}`).subscribe({
      next: () => { this.snack.open(`Permission '${perm}' removed.`, '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error removing permission.', 'Dismiss', { duration: 3000 })
    });
  }

  availablePermissions(role: RoleDTO) {
    return this.allPermissions().map(p => p.name).filter(p => !role.permissions.includes(p));
  }

  isProtectedRole(name: string) {
    return ['SuperAdmin', 'Administrator', 'Customer', 'Guest'].includes(name);
  }
}
