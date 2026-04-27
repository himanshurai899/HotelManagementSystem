import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { UserDTO, RoleDTO, CreateUserRequest } from '../../../model/api.models';

@Component({
  selector: 'app-users-tab',
  standalone: true,
  imports: [
    MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatChipsModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatDialogModule, MatSnackBarModule,
    MatTooltipModule, MatExpansionModule, FormsModule
  ],
  templateUrl: './users-tab.component.html',
  styleUrl: './users-tab.component.scss'
})
export class UsersTabComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  auth = inject(AuthService);

  users = signal<UserDTO[]>([]);
  roles = signal<RoleDTO[]>([]);
  loading = signal(true);
  showCreate = signal(false);
  editingUser = signal<UserDTO | null>(null);

  displayedColumns = ['username', 'email', 'phone', 'roles', 'claims', 'actions'];

  newUser: CreateUserRequest = { username: '', email: '', phoneNumber: '', password: '', firstName: '', lastName: '', roles: [] };

  isSuperAdmin = computed(() => this.auth.isSuperAdmin());

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

  createUser() {
    this.api.post<any>('users', this.newUser).subscribe({
      next: () => {
        this.snack.open('User created!', '', { duration: 2000 });
        this.newUser = { username: '', email: '', phoneNumber: '', password: '', firstName: '', lastName: '', roles: [] };
        this.showCreate.set(false);
        this.load();
      },
      error: (e) => this.snack.open(e?.error?.[0]?.description ?? 'Error creating user.', 'Dismiss', { duration: 4000 })
    });
  }

  deleteUser(id: number) {
    if (!confirm('Delete this user?')) return;
    this.api.delete(`users/${id}`).subscribe({
      next: () => { this.snack.open('User deleted.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error deleting user.', 'Dismiss', { duration: 3000 })
    });
  }

  addRole(userId: number, role: string) {
    this.api.post(`users/${userId}/roles`, JSON.stringify(role)).subscribe({
      next: () => { this.snack.open(`Role '${role}' assigned`, '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error assigning role.', 'Dismiss', { duration: 3000 })
    });
  }

  removeRole(userId: number, role: string) {
    this.api.delete(`users/${userId}/roles/${role}`).subscribe({
      next: () => { this.snack.open(`Role '${role}' removed`, '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error removing role.', 'Dismiss', { duration: 3000 })
    });
  }

  addClaim(userId: number, type: string, value: string) {
    this.api.post(`users/${userId}/claims`, { type, value }).subscribe({
      next: () => { this.snack.open('Claim added.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error adding claim.', 'Dismiss', { duration: 3000 })
    });
  }

  removeClaim(userId: number, type: string, value: string) {
    this.api.delete(`users/${userId}/claims/${type}/${value}`).subscribe({
      next: () => { this.snack.open('Claim removed.', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error removing claim.', 'Dismiss', { duration: 3000 })
    });
  }

  availableRoles(user: UserDTO) {
    return this.roles().map(r => r.name).filter(r => !user.roles?.includes(r));
  }
}
