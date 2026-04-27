import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { ApiService } from '../../services/api.service';
import { UserInfo } from '../../model/api.models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatChipsModule, MatSnackBarModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDividerModule
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private api    = inject(ApiService);
  private snack  = inject(MatSnackBar);
  private fb     = inject(FormBuilder);

  users           = signal<(UserInfo & { roles?: string[] })[]>([]);
  loading         = signal(true);
  showForm        = signal(false);
  saving          = signal(false);
  displayedColumns = ['userName', 'fullName', 'email', 'roles', 'actions'];
  allRoles         = ['Administrator', 'Customer', 'Guest'];
  idProofTypes     = ['Passport', 'Driving License', 'National ID', 'Voter ID', 'Aadhaar Card'];

  form = this.fb.group({
    username:      ['', Validators.required],
    firstName:     ['', Validators.required],
    lastName:      ['', Validators.required],
    email:         ['', [Validators.required, Validators.email]],
    phoneNumber:   [''],
    password:      ['', [Validators.required, Validators.minLength(6)]],
    roles:         [[] as string[]],
    idProofType:   [null as string | null],
    idProofNumber: [''],
  });

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

  toggleForm() {
    this.showForm.set(!this.showForm());
    if (!this.showForm()) this.form.reset({ roles: [] });
  }

  createUser() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.value;
    const payload = {
      username:      v.username      ?? '',
      firstName:     v.firstName     ?? '',
      lastName:      v.lastName      ?? '',
      email:         v.email         ?? '',
      phoneNumber:   v.phoneNumber   ?? '',
      password:      v.password      ?? '',
      roles:         v.roles         ?? [],
      idProofType:   v.idProofType   || null,
      idProofNumber: v.idProofNumber || null,
    };
    this.api.post('users', payload).subscribe({
      next: () => {
        this.snack.open('User created successfully.', '', { duration: 3000 });
        this.toggleForm();
        this.load();
      },
      error: () => this.snack.open('Failed to create user.', 'Dismiss', { duration: 4000 }),
      complete: () => this.saving.set(false)
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
