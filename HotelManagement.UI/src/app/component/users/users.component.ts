import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { UserInfo } from '../../model/api.models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatChipsModule, MatSnackBarModule
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  users = signal<(UserInfo & { roles?: string[] })[]>([]);
  loading = signal(true);
  displayedColumns = ['userName', 'email', 'roles', 'actions'];
  allRoles = ['Administrator', 'Customer', 'Guest'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<UserInfo[]>('users').subscribe({
      next: users => {
        this.users.set(users);
        users.forEach(u => {
          this.api.get<string[]>(`users/${u.id}/roles`).subscribe({
            next: roles => {
              const updated = this.users().map(x => x.id === u.id ? { ...x, roles } : x);
              this.users.set(updated);
            }
          });
        });
      },
      complete: () => this.loading.set(false)
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
}
