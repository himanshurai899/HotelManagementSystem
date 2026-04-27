import { Component, inject, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../services/api.service';
import { PermissionDTO, RoleDTO } from '../../../model/api.models';

@Component({
  selector: 'app-permissions-tab',
  standalone: true,
  imports: [
    MatCardModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatTableModule, MatTooltipModule
  ],
  templateUrl: './permissions-tab.component.html',
  styleUrl: './permissions-tab.component.scss'
})
export class PermissionsTabComponent implements OnInit {
  private api = inject(ApiService);

  permissions = signal<PermissionDTO[]>([]);
  roles = signal<RoleDTO[]>([]);
  loading = signal(true);

  displayedColumns = ['name', 'description', 'grantedTo'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<PermissionDTO[]>('permissions').subscribe({
      next: p => { this.permissions.set(p); this.tryFinish(); }
    });
    this.api.get<RoleDTO[]>('roles').subscribe({
      next: r => { this.roles.set(r); this.tryFinish(); }
    });
  }

  private _done = 0;
  private tryFinish() { if (++this._done >= 2) this.loading.set(false); }

  rolesWithPermission(permName: string): string[] {
    return this.roles()
      .filter(r => r.permissions.includes(permName))
      .map(r => r.name);
  }
}
