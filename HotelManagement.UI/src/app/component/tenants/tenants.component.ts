import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../services/api.service';
import { TenantCurrencyService, LOCALE_CURRENCY_LIST } from '../../services/tenant-currency.service';
import { TenantDTO, UserTenantDTO, UserDTO } from '../../model/api.models';
import { UserFormDialogComponent, UserFormDialogResult } from '../shared/user-form-dialog/user-form-dialog.component';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe,
    MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule,
    MatProgressSpinnerModule, MatChipsModule, MatExpansionModule, MatDividerModule,
    MatSnackBarModule, MatTooltipModule
  ],
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss'
})
export class TenantsComponent implements OnInit {
  private api    = inject(ApiService);
  private fb     = inject(FormBuilder);
  private snack  = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  items    = signal<TenantDTO[]>([]);
  loading  = signal(true);
  showForm = signal(false);
  editId   = signal<number | null>(null);

  /** Per-tenant user lists (loaded lazily on expand) */
  tenantUsers        = signal<Record<number, UserTenantDTO[] | undefined>>({});
  loadingTenantUsers = signal<Record<number, boolean | undefined>>({});

  /** All platform users — for the "add user" dropdown */
  allUsers     = signal<UserDTO[]>([]);
  addUserFor   = signal<Record<number, number | null>>({});

  displayedColumns = ['name', 'subdomain', 'plan', 'status', 'createdAt', 'actions'];
  readonly plans = ['Free', 'Pro', 'Enterprise'];
  readonly localeCurrencyList = LOCALE_CURRENCY_LIST;

  form = this.fb.group({
    name:         ['', Validators.required],
    subdomain:    ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    plan:         ['Free', Validators.required],
    isActive:     [true],
    locale:       ['en-IN', Validators.required],
    currencyCode: ['INR',   Validators.required]
  });

  ngOnInit() {
    this.load();
    this.api.get<UserDTO[]>('users').subscribe({ next: u => this.allUsers.set(u) });
  }

  load() {
    this.loading.set(true);
    this.api.get<TenantDTO[]>('tenants').subscribe({
      next: d => this.items.set(d),
      complete: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
  }

  loadTenantUsers(tenantId: number) {
    if (this.tenantUsers()[tenantId]) return;
    this.loadingTenantUsers.update(m => ({ ...m, [tenantId]: true }));
    this.api.get<UserTenantDTO[]>(`tenants/${tenantId}/users`).subscribe({
      next: u => {
        this.tenantUsers.update(m => ({ ...m, [tenantId]: u }));
        this.loadingTenantUsers.update(m => ({ ...m, [tenantId]: false }));
      },
      error: () => {
        this.tenantUsers.update(m => ({ ...m, [tenantId]: [] }));
        this.loadingTenantUsers.update(m => ({ ...m, [tenantId]: false }));
      }
    });
  }

  /** Users not yet in this tenant */
  availableUsers(tenantId: number): UserDTO[] {
    const memberIds = (this.tenantUsers()[tenantId] ?? []).map(ut => ut.userId);
    return this.allUsers().filter(u => !memberIds.includes(u.id));
  }

  addUserToTenant(tenantId: number, userId: number | null) {
    if (!userId) return;
    const user = this.allUsers().find(u => u.id === userId);
    const userName = user?.username ?? '';
    this.api.post(`tenants/${tenantId}/users`, { userId, userName, tenantId }).subscribe({
      next: () => {
        this.snack.open('User added to tenant!', '', { duration: 2000 });
        this.addUserFor.update(m => ({ ...m, [tenantId]: null }));
        // Invalidate cache so next expand re-fetches
        this.tenantUsers.update(m => { const copy = { ...m }; delete copy[tenantId]; return copy; });
        this.loadTenantUsers(tenantId);
      },
      error: (e) => this.snack.open(e?.error?.message ?? 'Error adding user.', 'Dismiss', { duration: 3000 })
    });
  }

  removeUserFromTenant(tenantId: number, userId: number, userName: string) {
    if (!confirm(`Remove "${userName}" from this tenant?`)) return;
    this.api.delete(`tenants/${tenantId}/users/${userId}`).subscribe({
      next: () => {
        this.snack.open('User removed from tenant.', '', { duration: 2000 });
        this.tenantUsers.update(m => ({ ...m, [tenantId]: (m[tenantId] ?? []).filter(u => u.userId !== userId) }));
      },
      error: () => this.snack.open('Error removing user.', 'Dismiss', { duration: 3000 })
    });
  }

  openCreateUserDialog(tenantId: number) {
    const ref = this.dialog.open(UserFormDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: { preselectedTenantId: tenantId },
      disableClose: true
    });
    ref.afterClosed().subscribe((result: UserFormDialogResult | null) => {
      if (!result) return;
      this.api.post<any>('users', result.user).subscribe({
        next: (created) => {
          if (created?.id) {
            const userName = created.userName ?? result.user.username ?? '';
            this.api.post(`tenants/${tenantId}/users`, { userId: created.id, userName, tenantId }).subscribe({
              next: () => {
                this.tenantUsers.update(m => { const c = { ...m }; delete c[tenantId]; return c; });
                this.loadTenantUsers(tenantId);
              }
            });
          }
          this.snack.open('User created & added to tenant!', '', { duration: 2500 });
        },
        error: (e) => this.snack.open(e?.error?.[0]?.description ?? 'Error creating user.', 'Dismiss', { duration: 4000 })
      });
    });
  }

  openForm(item?: TenantDTO) {
    this.editId.set(item?.id ?? null);
    if (item) {
      this.form.patchValue({
        name: item.name, subdomain: item.subdomain, plan: item.plan, isActive: item.isActive,
        locale: item.locale ?? 'en-IN', currencyCode: item.currencyCode ?? 'INR'
      });
    } else {
      this.form.reset({ plan: 'Free', isActive: true, locale: 'en-IN', currencyCode: 'INR' });
    }
    this.showForm.set(true);
  }

  /** When the locale dropdown changes, auto-fill the matching currency code. */
  onLocaleChange(locale: string) {
    const entry = this.localeCurrencyList.find(e => e.locale === locale);
    if (entry) this.form.patchValue({ currencyCode: entry.currencyCode });
  }

  save() {
    if (this.form.invalid) return;
    const payload = { ...this.form.value, id: this.editId() ?? 0 };
    const id = this.editId();
    const call = id ? this.api.put(`tenants/${id}`, payload) : this.api.post('tenants', payload);
    call.subscribe({
      next: () => {
        this.snack.open(id ? 'Tenant updated!' : 'Tenant created!', '', { duration: 2000 });
        this.showForm.set(false);
        this.load();
      },
      error: (e) => this.snack.open(e?.error?.message ?? 'Error saving tenant.', 'Dismiss', { duration: 4000 })
    });
  }

  delete(id: number, name: string) {
    if (!confirm(`Delete tenant "${name}"? This cannot be undone.`)) return;
    this.api.delete(`tenants/${id}`).subscribe({
      next: () => { this.snack.open('Tenant deleted', '', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Error deleting tenant.', 'Dismiss', { duration: 3000 })
    });
  }

  planColor(plan: string): string {
    return plan === 'Enterprise' ? 'primary' : plan === 'Pro' ? 'accent' : '';
  }

  getAddUser(tenantId: number) { return this.addUserFor()[tenantId] ?? null; }
  setAddUser(tenantId: number, val: number | null) { this.addUserFor.update(m => ({ ...m, [tenantId]: val })); }
}
