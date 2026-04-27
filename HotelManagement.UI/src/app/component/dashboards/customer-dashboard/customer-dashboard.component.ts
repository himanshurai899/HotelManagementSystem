import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ApiService } from '../../../services/api.service';
import { BookingDTO, BOOKING_STATUS_LABELS } from '../../../model/api.models';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule,
            MatProgressSpinnerModule, MatChipsModule, DatePipe, DecimalPipe],
  templateUrl: './customer-dashboard.component.html',
  styleUrl: './customer-dashboard.component.scss'
})
export class CustomerDashboardComponent implements OnInit {
  private api = inject(ApiService);

  bookings = signal<BookingDTO[]>([]);
  loading = signal(true);
  statusLabels = BOOKING_STATUS_LABELS;

  ngOnInit() {
    this.api.get<BookingDTO[]>('bookings/my').subscribe({
      next: data => this.bookings.set(data.slice(0, 5)),
      error: () => {},
      complete: () => this.loading.set(false)
    });
  }

  getStatusColor(status: number): string {
    return ['warn', 'primary', '', 'accent'][status] ?? '';
  }
}
