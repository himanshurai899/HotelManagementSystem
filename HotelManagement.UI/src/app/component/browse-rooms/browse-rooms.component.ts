import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../services/api.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { RoomDTO, RoomTypeDTO } from '../../model/api.models';

@Component({
  selector: 'app-browse-rooms',
  standalone: true,
  imports: [
    RouterLink, FormsModule, CurrencyPipe, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './browse-rooms.component.html',
  styleUrl: './browse-rooms.component.scss'
})
export class BrowseRoomsComponent implements OnInit {
  private api = inject(ApiService);
  readonly currencySvc = inject(TenantCurrencyService);

  allRooms = signal<RoomDTO[]>([]);
  rooms = signal<RoomDTO[]>([]);
  roomTypes = signal<RoomTypeDTO[]>([]);
  loading = signal(true);
  filterType = signal<number>(0);
  filterAvailable = signal<boolean>(false);

  ngOnInit() {
    this.api.get<RoomTypeDTO[]>('roomtypes').subscribe({ next: d => this.roomTypes.set(d) });
    this.api.get<RoomDTO[]>('rooms').subscribe({
      next: d => { this.allRooms.set(d); this.rooms.set(d); },
      complete: () => this.loading.set(false)
    });
  }

  applyFilter() {
    let result = this.allRooms();
    if (this.filterType() > 0) result = result.filter(r => r.roomTypeId === this.filterType());
    if (this.filterAvailable()) result = result.filter(r => r.isAvailable);
    this.rooms.set(result);
  }

  getTypeName(id: number) {
    return this.roomTypes().find(t => t.id === id)?.name ?? '';
  }

  getBasePrice(id: number) {
    return this.roomTypes().find(t => t.id === id)?.basePrice ?? 0;
  }
}
