import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, UpperCasePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { UserDTO, RoleDTO, UserTenantDTO } from '../../../model/api.models';
import { UserFormDialogComponent, UserFormDialogResult } from '../../shared/user-form-dialog/user-form-dialog.component';

@Component({
  selector: 'app-users-tab',
  standalone: true,
  imports: [
    FormsModule, DatePipe, UpperCasePipe,
    MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatChipsModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatDialogModule, MatSnackBarModule,
    MatTooltipModule, MatExpansionModule, MatDividerModule
  ],
  templateUrl: './users-tab.component.html',
  styleUrl: './users-tab.component.scss'
})
export class UsersTabComponent implements OnInit {
  private api    = inject(ApiService);
  private snack  = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  auth = inject(AuthService);

  users   = signal<UserDTO[]>([]);
  roles   = signal<RoleDTO[]>([]);
  loading = signal(true);

  userTenants        = signal<Record<number, UserTenantDTO[] | undefined>>({});
  loadingTenants     = signal<Record<number, boolean | undefined>>({});
  addRoleFor         = signal<Record<number, string>>({});
  addClaimFor        = signal<Record<number, { type: string; value: string }>>({});
  brokenPhotos       = signal<Set<number>>(new Set());

  isSuperAdmin = computed(() => this.auth.isSuperAdmin());

  /** Falls back to initials if the photo URL is broken or absent */
  showPhoto(u: UserDTO): boolean {
    return !!u.profilePhotoUrl && !this.brokenPhotos().has(u.id);
  }
  markPhotoBroken(id: number) {
    this.brokenPhotos.update(s => new Set([...s, id]));
  }
  /** Prepends API base URL for relative photo paths */
  resolvePhotoUrl(url: string | null): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    const base = environment.apiBaseUrl.replace('/api', '');
    return `${base}/${url.replace(/^\//, '')}`;
  }

  ngOnInit() {
    this.load();
    this.api.get<RoleDTO[]>('roles').subscribe({ next: r => this.roles.set(r) });
  }

  load() {
    this.loading.set(true);
    this.api.get<UserDTO[]>('users').subscribe({
      next: u => { this.users.set(u); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreateDialog() {
    const ref = this.dialog.open(UserFormDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: {},
      disableClose: true
    });
    ref.afterClosed().subscribe((result: UserFormDialogResult | null) => {
      if (!result) return;
      this.api.post<any>('users', result.user).subscribe({
        next: (created) => {
          const tenantIds: number[] = result.tenantIds ?? [];
          if (tenantIds.length > 0 && created?.id) {
            let pending = tenantIds.length;
            tenantIds.forEach(tid => {
              const userName = created.userName ?? result.user.username ?? '';
              this.api.post(`tenants/${tid}/users`, { userId: created.id, userName, tenantId: tid }).subscribe({
                complete: () => { if (--pending === 0) this.load(); },
                error:    () => { if (--pending === 0) this.load(); }
              });
            });
          } else {
            this.load();
          }
          this.snack.open('User created successfully!', '', { duration: 2500 });
        },
        error: (e) => this.snack.open(e?.error?.[0]?.description ?? 'Error creating user.', 'Dismiss', { duration: 4000 })
      });
    });
  }

  loadUserTenants(userId: number) {
    if (this.userTenants()[userId]) return;
    this.loadingTenants.update(m => ({ ...m, [userId]: true }));
    this.api.get<UserTenantDTO[]>(`users/${userId}/tenants`).subscribe({
      next: t => {
        this.userTenants.update(m => ({ ...m, [userId]: t }));
        this.loadingTenants.update(m => ({ ...m, [userId]: false }));
      },
      error: () => {
        this.userTenants.update(m => ({ ...m, [userId]: [] }));
        this.loadingTenants.update(m => ({ ...m, [userId]: false }));
      }
    });
  }

  deleteUser(id: number) {
    if (!confirm('Delete this user? This action cannot be undone.')) return;
    this.api.delete(`users/${id}`).subscribe({
      next: () => { this.snack.open('User deleted.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error deleting user.', 'Dismiss', { duration: 3000 })
    });
  }

  addRole(userId: number, role: string) {
    if (!role) return;
    this.api.post(`users/${userId}/roles`, JSON.stringify(role)).subscribe({
      next: () => {
        this.snack.open(`Role '${role}' assigned`, '', { duration: 2000 });
        this.addRoleFor.update(m => ({ ...m, [userId]: '' }));
        this.load();
      },
      error: () => this.snack.open('Error assigning role.', 'Dismiss', { duration: 3000 })
    });
  }

  removeRole(userId: number, role: string) {
    this.api.delete(`users/${userId}/roles/${role}`).subscribe({
      next: () => { this.snack.open(`Role '${role}' removed`, '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error removing role.', 'Dismiss', { duration: 3000 })
    });
  }

  removeClaim(userId: number, type: string, value: string) {
    this.api.delete(`users/${userId}/claims/${type}/${value}`).subscribe({
      next: () => { this.snack.open('Claim removed.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error removing claim.', 'Dismiss', { duration: 3000 })
    });
  }

  addClaim(userId: number) {
    const c = this.addClaimFor()[userId];
    if (!c?.type?.trim() || !c?.value?.trim()) {
      this.snack.open('Both claim type and value are required.', 'Dismiss', { duration: 3000 });
      return;
    }
    this.api.post(`users/${userId}/claims`, { type: c.type.trim(), value: c.value.trim() }).subscribe({
      next: () => {
        this.snack.open(`Claim '${c.type}' added.`, '', { duration: 2000 });
        this.addClaimFor.update(m => ({ ...m, [userId]: { type: '', value: '' } }));
        this.load();
      },
      error: () => this.snack.open('Error adding claim.', 'Dismiss', { duration: 3000 })
    });
  }

  getAddClaim(userId: number) { return this.addClaimFor()[userId] ?? { type: '', value: '' }; }
  setAddClaimType(userId: number, type: string)  { this.addClaimFor.update(m => ({ ...m, [userId]: { ...this.getAddClaim(userId), type } })); }
  setAddClaimValue(userId: number, value: string){ this.addClaimFor.update(m => ({ ...m, [userId]: { ...this.getAddClaim(userId), value } })); }

  availableRoles(user: UserDTO) {
    return this.roles().map(r => r.name).filter(r => !user.roles?.includes(r));
  }

  getAddRole(userId: number) { return this.addRoleFor()[userId] ?? ''; }
  setAddRole(userId: number, val: string) { this.addRoleFor.update(m => ({ ...m, [userId]: val })); }
}
