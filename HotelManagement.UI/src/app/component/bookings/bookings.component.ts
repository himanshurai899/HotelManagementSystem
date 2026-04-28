import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, CurrencyPipe } from '@angular/common';
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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { BookingDTO, RoomDTO, BOOKING_STATUS_LABELS } from '../../model/api.models';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe, CurrencyPipe, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatChipsModule, MatProgressSpinnerModule, MatSnackBarModule,
    MatDatepickerModule, MatNativeDateModule
  ],
  templateUrl: './bookings.component.html',
  styleUrl: './bookings.component.scss'
})
export class BookingsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);
  readonly currencySvc = inject(TenantCurrencyService);

  isAdmin = this.auth.hasRole('Administrator') || this.auth.hasRole('SuperAdmin');
  /**
   * Admins/SuperAdmins can modify bookings only when they hold the
   * 'ManageBookings' permission claim. SuperAdmin gets it via role seed;
   * regular Administrators must be granted it per-user.
   * Customers can always cancel their own bookings (server enforces ownership).
   */
  canModify = !this.isAdmin || this.auth.hasClaim('Permission', 'ManageBookings');
  /** Approve/Reject require both Admin role AND ManageBookings policy */
  canApprove = this.isAdmin && this.auth.hasClaim('Permission', 'ManageBookings');
  bookings = signal<BookingDTO[]>([]);
  rooms = signal<RoomDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  statusLabels = BOOKING_STATUS_LABELS;
  displayedColumns = this.isAdmin
    ? ['roomNumber', 'customer', 'checkIn', 'checkOut', 'total', 'status', 'actions']
    : ['roomNumber', 'checkIn', 'checkOut', 'total', 'status', 'actions'];

  /** Minimum selectable check-in date — today (no past dates) */
  readonly minCheckInDate = new Date();

  /** Default check-in date value pre-filled to today */
  readonly defaultCheckInDate = new Date();

  /** Minimum check-out date — updated reactively when room or check-in changes */
  minCheckOutDate = signal<Date>(this._tomorrowFrom(new Date()));

  form = this.fb.group({
    roomId: [0, Validators.required],
    checkInDate: [this.defaultCheckInDate, Validators.required],
    checkOutDate: [null as Date | null, Validators.required]
  });

  ngOnInit() {
    this.loadBookings();
    this.api.get<RoomDTO[]>('rooms').subscribe({
      next: d => {
        this.rooms.set(d.filter(r => r.isAvailable));
        // Set initial default check-in to next full hour
        this.form.patchValue({ checkInDate: this._defaultCheckIn() });
      }
    });

    // When room selection changes, recompute checkout min date
    this.form.get('roomId')!.valueChanges.subscribe(roomId => {
      this._updateCheckOutMin(roomId ?? 0);
    });

    // When check-in changes, push checkout min forward accordingly
    this.form.get('checkInDate')!.valueChanges.subscribe(checkIn => {
      const roomId = this.form.get('roomId')!.value ?? 0;
      this._updateCheckOutMin(roomId, checkIn as Date | null);
      // Clear checkout if it's now before the new min
      const currentOut = this.form.get('checkOutDate')!.value as Date | null;
      if (currentOut && currentOut < this.minCheckOutDate()) {
        this.form.patchValue({ checkOutDate: null });
      }
    });
  }

  /** Returns today at 11:00 AM as the default check-in date (tomorrow if already past 11 PM) */
  private _defaultCheckIn(): Date {
    const d = new Date();
    if (d.getHours() >= 23) { d.setDate(d.getDate() + 1); }
    d.setHours(11, 0, 0, 0);
    return d;
  }

  /** Returns tomorrow at midnight relative to a given date */
  private _tomorrowFrom(date: Date): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + 1);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  /** Today's date at midnight */
  private _today(): Date {
    const d = new Date();
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private _updateCheckOutMin(roomId: number, checkIn?: Date | null) {
    const room = this.rooms().find(r => r.id === roomId);
    const baseDate = checkIn ?? (this.form.get('checkInDate')!.value as Date | null) ?? new Date();
    if (room?.allowHourlyStay) {
      // Hourly stays: checkout can be same day — set min to check-in date itself (today)
      const min = new Date(baseDate);
      min.setHours(0, 0, 0, 0);
      this.minCheckOutDate.set(min);
    } else {
      // Nightly stays: checkout must be at least the next day
      this.minCheckOutDate.set(this._tomorrowFrom(baseDate));
    }
  }

  /** True if the selected room supports hourly stay */
  get selectedRoomIsHourly(): boolean {
    const roomId = this.form.get('roomId')!.value ?? 0;
    return this.rooms().find(r => r.id === roomId)?.allowHourlyStay ?? false;
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
    const val = this.form.value;
    const checkIn = val.checkInDate ? new Date(val.checkInDate) : null;
    if (checkIn) checkIn.setHours(11, 0, 0, 0);   // 11:00 AM default check-in
    const checkOut = val.checkOutDate ? new Date(val.checkOutDate) : null;
    if (checkOut) checkOut.setHours(10, 0, 0, 0);  // 10:00 AM default check-out
    this.api.post<BookingDTO>('bookings', {
      id: 0,
      status: 0,
      roomId: val.roomId,
      checkInDate: checkIn?.toISOString(),
      checkOutDate: checkOut?.toISOString()
    }).subscribe({
      next: () => {
        this.snack.open('Booking created!', '', { duration: 2000 });
        this.showForm.set(false);
        this.loadBookings();
      },
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

  approve(id: number) {
    if (!confirm('Approve this booking? The room will be blocked for these dates.')) return;
    this.api.put(`bookings/${id}/approve`, {}).subscribe({
      next: () => { this.snack.open('Booking approved ✓', '', { duration: 2000 }); this.loadBookings(); },
      error: (err) => this.snack.open(err?.error?.message ?? 'Approval failed.', 'Dismiss', { duration: 3000 })
    });
  }

  reject(id: number) {
    if (!confirm('Reject this booking? The customer will be notified.')) return;
    this.api.put(`bookings/${id}/reject`, {}).subscribe({
      next: () => { this.snack.open('Booking rejected.', '', { duration: 2000 }); this.loadBookings(); },
      error: (err) => this.snack.open(err?.error?.message ?? 'Rejection failed.', 'Dismiss', { duration: 3000 })
    });
  }

  // 0=Pending(warn), 1=Confirmed(primary), 2=Cancelled(no color), 3=Completed(accent)
  getStatusColor(s: number) { return ['warn', 'primary', '', 'accent'][s] ?? ''; }
}
