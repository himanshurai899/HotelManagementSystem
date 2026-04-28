import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { RoomDTO, BookingDTO, StaffDTO, TenantDTO } from '../../../model/api.models';

interface StatCard { label: string; value: number; icon: string; color: string; route: string; }

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private api  = inject(ApiService);
  private auth = inject(AuthService);

  stats      = signal<StatCard[]>([]);
  loading    = signal(true);
  isSuperAdmin = computed(() => this.auth.isSuperAdmin());

  ngOnInit() {
    let done = 0;
    const counts = { rooms: 0, bookings: 0, staff: 0, tenants: 0 };
    const total  = this.isSuperAdmin() ? 4 : 3;
    const finish = () => { if (++done === total) { this.buildStats(counts); this.loading.set(false); } };

    this.api.get<RoomDTO[]>('rooms').subscribe({ next: d => { counts.rooms = d.length; finish(); }, error: finish });
    this.api.get<BookingDTO[]>('bookings').subscribe({ next: d => { counts.bookings = d.length; finish(); }, error: finish });
    this.api.get<StaffDTO[]>('staff').subscribe({ next: d => { counts.staff = d.length; finish(); }, error: finish });

    if (this.isSuperAdmin()) {
      this.api.get<TenantDTO[]>('tenants').subscribe({ next: d => { counts.tenants = d.length; finish(); }, error: finish });
    }
  }

  private buildStats(c: { rooms: number; bookings: number; staff: number; tenants: number }) {
    const cards: StatCard[] = [
      { label: 'Total Rooms',    value: c.rooms,    icon: 'hotel',       color: '#1976d2', route: '/rooms' },
      { label: 'Total Bookings', value: c.bookings, icon: 'book_online', color: '#388e3c', route: '/bookings' },
      { label: 'Staff Members',  value: c.staff,    icon: 'badge',       color: '#f57c00', route: '/staff' },
    ];
    if (this.isSuperAdmin()) {
      cards.push({ label: 'Tenants', value: c.tenants, icon: 'domain', color: '#7b1fa2', route: '/tenants' });
    }
    this.stats.set(cards);
  }
}
