import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../../services/api.service';
import { RoomDTO, RoomTypeDTO } from '../../../model/api.models';

@Component({
  selector: 'app-guest-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatChipsModule],
  templateUrl: './guest-dashboard.component.html',
  styleUrl: './guest-dashboard.component.scss'
})
export class GuestDashboardComponent implements OnInit {
  private api = inject(ApiService);

  rooms = signal<RoomDTO[]>([]);
  roomTypes = signal<RoomTypeDTO[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.api.get<RoomTypeDTO[]>('roomtypes').subscribe({ next: d => this.roomTypes.set(d) });
    this.api.get<RoomDTO[]>('rooms').subscribe({
      next: d => this.rooms.set(d.filter(r => r.isAvailable).slice(0, 6)),
      complete: () => this.loading.set(false)
    });
  }

  getTypeName(id: number): string {
    return this.roomTypes().find(t => t.id === id)?.name ?? '';
  }
}
