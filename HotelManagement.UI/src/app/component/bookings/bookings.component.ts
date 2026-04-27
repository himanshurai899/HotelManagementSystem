import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { BookingDTO, RoomDTO, BOOKING_STATUS_LABELS } from '../../model/api.models';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe, DecimalPipe, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatChipsModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './bookings.component.html',
  styleUrl: './bookings.component.scss'
})
export class BookingsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  isAdmin = this.auth.hasRole('Administrator') || this.auth.hasRole('SuperAdmin');
  /**
   * Admins/SuperAdmins can modify bookings only when they hold the
   * 'ManageBookings' permission claim. SuperAdmin gets it via role seed;
   * regular Administrators must be granted it per-user.
   * Customers can always cancel their own bookings (server enforces ownership).
   */
  canModify = !this.isAdmin || this.auth.hasClaim('Permission', 'ManageBookings');
  bookings = signal<BookingDTO[]>([]);
  rooms = signal<RoomDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  statusLabels = BOOKING_STATUS_LABELS;
  displayedColumns = ['roomNumber', 'checkIn', 'checkOut', 'total', 'status', 'actions'];

  form = this.fb.group({
    roomId: [0, Validators.required],
    checkInDate: ['', Validators.required],
    checkOutDate: ['', Validators.required]
  });

  ngOnInit() {
    this.loadBookings();
    this.api.get<RoomDTO[]>('rooms').subscribe({ next: d => this.rooms.set(d.filter(r => r.isAvailable)) });
  }

  loadBookings() {
    this.loading.set(true);
    const endpoint = this.isAdmin ? 'bookings' : 'bookings/my';
    this.api.get<BookingDTO[]>(endpoint).subscribe({
      next: d => this.bookings.set(d),
      complete: () => this.loading.set(false)
    });
  }

  save() {
    if (this.form.invalid) return;
    this.api.post<BookingDTO>('bookings', { ...this.form.value, id: 0, status: 0 }).subscribe({
      next: () => { this.snack.open('Booking created!', '', { duration: 2000 }); this.showForm.set(false); this.loadBookings(); },
      error: () => this.snack.open('Error creating booking.', 'Dismiss', { duration: 3000 })
    });
  }

  cancel(id: number) {
    if (this.isAdmin && !this.canModify) {
      this.snack.open('You do not have permission to modify bookings.', 'Dismiss', { duration: 3000 });
      return;
    }
    if (!confirm('Cancel this booking?')) return;
    this.api.delete(`bookings/${id}`).subscribe({
      next: () => { this.snack.open('Booking cancelled', '', { duration: 2000 }); this.loadBookings(); }
    });
  }

  getStatusColor(s: number) { return ['warn', 'primary', '', 'accent'][s] ?? ''; }
}
