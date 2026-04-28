import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { RoleDTO, TenantDTO, CreateUserRequest, ClaimDTO } from '../../../model/api.models';

export interface UserFormDialogData {
  /** Pre-selected tenantId — when opened from the Tenants page */
  preselectedTenantId?: number;
}

export interface UserFormDialogResult {
  user: CreateUserRequest;
  tenantIds: number[];
}

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatChipsModule, MatDividerModule,
    MatProgressSpinnerModule, MatTooltipModule, MatSnackBarModule
  ],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss'
})
export class UserFormDialogComponent implements OnInit {
  private fb      = inject(FormBuilder);
  private api     = inject(ApiService);
  private snack   = inject(MatSnackBar);
  readonly auth   = inject(AuthService);
  readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  readonly data: UserFormDialogData = inject(MAT_DIALOG_DATA) ?? {};

  roles   = signal<RoleDTO[]>([]);
  tenants = signal<TenantDTO[]>([]);
  loading = signal(true);
  showPassword = signal(false);

  /** Claims being built before submission */
  claims        = signal<ClaimDTO[]>([]);
  pendingClaim  = signal<ClaimDTO>({ type: '', value: '' });

  form = this.fb.group({
    firstName:   ['', Validators.required],
    lastName:    ['', Validators.required],
    username:    ['', Validators.required],
    email:       ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    password:    ['', [Validators.required, Validators.minLength(8)]],
    roles:       [[] as string[]],
    tenantIds:   [this.data.preselectedTenantId ? [this.data.preselectedTenantId] : [] as number[]]
  });

  ngOnInit() {
    let done = 0;
    const finish = () => { if (++done === 2) this.loading.set(false); };
    this.api.get<RoleDTO[]>('roles').subscribe({ next: r => { this.roles.set(r); finish(); }, error: finish });
    if (this.auth.isSuperAdmin()) {
      this.api.get<TenantDTO[]>('tenants').subscribe({ next: t => { this.tenants.set(t); finish(); }, error: finish });
    } else {
      finish();
    }
  }

  setPendingType(val: string)  { this.pendingClaim.update(c => ({ ...c, type: val })); }
  setPendingValue(val: string) { this.pendingClaim.update(c => ({ ...c, value: val })); }

  addClaim() {
    const { type, value } = this.pendingClaim();
    if (!type.trim() || !value.trim()) {
      this.snack.open('Both claim type and value are required.', 'Dismiss', { duration: 3000 });
      return;
    }
    if (this.claims().some(c => c.type === type.trim() && c.value === value.trim())) {
      this.snack.open('This claim already exists.', 'Dismiss', { duration: 2500 });
      return;
    }
    this.claims.update(list => [...list, { type: type.trim(), value: value.trim() }]);
    this.pendingClaim.set({ type: '', value: '' });
  }

  removeClaim(index: number) {
    this.claims.update(list => list.filter((_, i) => i !== index));
  }

  submit() {
    if (this.form.invalid) return;
    if (this.claims().length === 0) {
      this.snack.open('At least one claim is required.', 'Dismiss', { duration: 3000 });
      return;
    }
    const v = this.form.value;
    const result: UserFormDialogResult = {
      user: {
        firstName:   v.firstName!,
        lastName:    v.lastName!,
        username:    v.username!,
        email:       v.email!,
        phoneNumber: v.phoneNumber!,
        password:    v.password!,
        roles:       v.roles ?? [],
        claims:      this.claims()
      },
      tenantIds: v.tenantIds ?? []
    };
    this.dialogRef.close(result);
  }

  cancel() { this.dialogRef.close(null); }
}

export interface UserFormDialogData {
  /** Pre-selected tenantId — when opened from the Tenants page */
  preselectedTenantId?: number;
}

export interface UserFormDialogResult {
  user: CreateUserRequest;
  tenantIds: number[];
}

// old class stub removed
