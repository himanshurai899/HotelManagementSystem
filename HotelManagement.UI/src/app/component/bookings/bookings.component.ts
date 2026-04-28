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
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { BookingDTO, BookingRoomDTO, RoomDTO, BOOKING_STATUS_LABELS, BookingType, RebookSuggestionDTO } from '../../model/api.models';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe, CurrencyPipe, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatChipsModule, MatProgressSpinnerModule, MatSnackBarModule,
    MatDatepickerModule, MatNativeDateModule, MatTooltipModule, MatButtonToggleModule
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
  bookings    = signal<BookingDTO[]>([]);
  suggestions = signal<RebookSuggestionDTO[]>([]);
  rooms = signal<RoomDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  /** Expose const for template use */
  readonly BookingType = BookingType;
  /** Currently selected booking type in the new-booking form */
  bookingType = signal<BookingType>(BookingType.NightStay);
  statusLabels = BOOKING_STATUS_LABELS;
  displayedColumns = this.isAdmin
    ? ['roomNumber', 'customer', 'checkIn', 'checkOut', 'total', 'status', 'actions']
    : ['roomNumber', 'checkIn', 'checkOut', 'total', 'status', 'actions'];

  /** Non-null when the chosen room+dates overlap an existing Pending/Confirmed booking */
  dateConflictMessage = signal<string | null>(null);

  /** Minimum selectable check-in date — today (no past dates) */
  readonly minCheckInDate = new Date();

  /** Default check-in date value pre-filled to today */
  readonly defaultCheckInDate = new Date();

  /** Minimum check-out date — updated reactively when room or check-in changes */
  minCheckOutDate = signal<Date>(this._tomorrowFrom(new Date()));

  form = this.fb.group({
    // Phase 12a — a booking can span multiple rooms; the array is authoritative.
    roomIds:      [[] as number[], Validators.required],
    checkInDate:  [this.defaultCheckInDate, Validators.required],
    checkOutDate: [null as Date | null, Validators.required],
    // Phase 12c — time fields; required only for Hourly type.
    checkInTime:  [null as string | null],
    checkOutTime: [null as string | null]
  });

  ngOnInit() {
    this.loadBookings();
    if (!this.isAdmin) {
      this.api.get<RebookSuggestionDTO[]>('bookings/history').subscribe({
        next: d => this.suggestions.set(d)
      });
    }
    this.api.get<RoomDTO[]>('rooms').subscribe({
      next: d => {
        this.rooms.set(d.filter(r => r.isAvailable));
        // Set initial default check-in to next full hour
        this.form.patchValue({ checkInDate: this._defaultCheckIn() });
      }
    });

    // When room selection changes, recompute checkout min date
    this.form.get('roomIds')!.valueChanges.subscribe(roomIds => {
      this._updateCheckOutMin(roomIds ?? []);
      // Auto-switch to Hourly if all selected rooms only support hourly
      const selected = this.rooms().filter(r => (roomIds ?? []).includes(r.id));
      if (selected.length > 0 && selected.every(r => r.allowHourlyStay && r.hourlyRate != null
                                                   && !r.pricePerNight)) {
        this.bookingType.set(BookingType.Hourly);
      }
      this._checkDateConflict();
    });

    // When check-in changes, push checkout min forward accordingly
    this.form.get('checkInDate')!.valueChanges.subscribe(checkIn => {
      const roomIds = this.form.get('roomIds')!.value ?? [];
      this._updateCheckOutMin(roomIds, checkIn as Date | null);
      // Clear checkout if it's now before the new min
      const currentOut = this.form.get('checkOutDate')!.value as Date | null;
      if (currentOut && currentOut < this.minCheckOutDate()) {
        this.form.patchValue({ checkOutDate: null });
      }
      this._checkDateConflict();
    });

    // Recheck conflict whenever checkout date changes
    this.form.get('checkOutDate')!.valueChanges.subscribe(() => {
      this._checkDateConflict();
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

  private _updateCheckOutMin(roomIds: number[], checkIn?: Date | null) {
    const selected = this.rooms().filter(r => roomIds.includes(r.id));
    const baseDate = checkIn ?? (this.form.get('checkInDate')!.value as Date | null) ?? new Date();
    // Hourly checkout is only allowed when every selected room supports hourly stay.
    const allHourly = selected.length > 0 && selected.every(r => r.allowHourlyStay);
    if (allHourly) {
      // Hourly stays: checkout can be same day — set min to check-in date itself (today)
      const min = new Date(baseDate);
      min.setHours(0, 0, 0, 0);
      this.minCheckOutDate.set(min);
    } else {
      // Nightly stays: checkout must be at least the next day
      this.minCheckOutDate.set(this._tomorrowFrom(baseDate));
    }
  }

  /** True when every selected room supports hourly stay. */
  get selectedRoomIsHourly(): boolean {
    const roomIds = this.form.get('roomIds')!.value ?? [];
    if (!roomIds.length) return false;
    const selected = this.rooms().filter(r => roomIds.includes(r.id));
    return selected.length > 0 && selected.every(r => r.allowHourlyStay);
  }

  /** Checks the current form rooms+dates against existing Pending/Confirmed bookings. */
  private _checkDateConflict(): void {
    const roomIds = (this.form.get('roomIds')!.value ?? []) as number[];
    const checkIn  = this.form.get('checkInDate')!.value  as Date | null;
    const checkOut = this.form.get('checkOutDate')!.value as Date | null;

    if (!roomIds.length || !checkIn || !checkOut) {
      this.dateConflictMessage.set(null);
      return;
    }

    const cin  = new Date(checkIn);  cin.setHours(0, 0, 0, 0);
    const cout = new Date(checkOut); cout.setHours(23, 59, 59, 999);

    // Find the first conflicting (room, booking) pair across all selected rooms.
    for (const roomId of roomIds) {
      const conflict = this.bookings().find(b =>
        (b.status === 0 || b.status === 1) &&                 // Pending or Confirmed
        (b.rooms?.some(br => br.roomId === roomId) ?? false) &&
        new Date(b.checkInDate) < cout &&
        new Date(b.checkOutDate) > cin
      );
      if (conflict) {
        const room     = this.rooms().find(r => r.id === roomId);
        const label    = room ? `Room ${room.roomNumber}` : 'A selected room';
        const inLabel  = new Date(conflict.checkInDate).toLocaleDateString();
        const outLabel = new Date(conflict.checkOutDate).toLocaleDateString();
        const status   = this.statusLabels[conflict.status]?.toLowerCase() ?? 'booked';
        this.dateConflictMessage.set(
          `${label} is already ${status} from ${inLabel} to ${outLabel}. Please choose different dates or remove this room.`
        );
        return;
      }
    }
    this.dateConflictMessage.set(null);
  }

  rebook(s: RebookSuggestionDTO) {
    const checkIn  = new Date();
    checkIn.setDate(checkIn.getDate() + 1);
    checkIn.setHours(11, 0, 0, 0);
    const checkOut = new Date(checkIn);
    checkOut.setDate(checkOut.getDate() + s.durationDays);
    checkOut.setHours(10, 0, 0, 0);

    this.bookingType.set(BookingType.NightStay);
    this.form.patchValue({
      roomIds:      s.roomIds,
      checkInDate:  checkIn,
      checkOutDate: checkOut,
      checkInTime:  null,
      checkOutTime: null
    });
    this.showForm.set(true);
    // Scroll to form after next render tick
    setTimeout(() => {
      document.querySelector('.form-card')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 50);
  }

  loadBookings() {
    this.loading.set(true);
    const endpoint = this.isAdmin ? 'bookings' : 'bookings/my';
    this.api.get<BookingDTO[]>(endpoint).subscribe({
      next: d => {
        this.bookings.set(d);
        this._checkDateConflict(); // refresh conflict state after bookings reload
      },
      complete: () => this.loading.set(false)
    });
  }

  save() {
    if (this.form.invalid) return;
    const val = this.form.value;
    const isHourly = this.bookingType() === BookingType.Hourly;

    const checkIn  = val.checkInDate  ? new Date(val.checkInDate)  : null;
    const checkOut = val.checkOutDate ? new Date(val.checkOutDate) : null;

    if (!isHourly) {
      if (checkIn)  checkIn.setHours(11, 0, 0, 0);  // 11:00 AM default check-in
      if (checkOut) checkOut.setHours(10, 0, 0, 0); // 10:00 AM default check-out
    }

    const roomIds = (val.roomIds ?? []) as number[];
    const rooms: Partial<BookingRoomDTO>[] = roomIds.map(id => ({ roomId: id }));

    this.api.post<BookingDTO>('bookings', {
      id: 0,
      status: 0,
      bookingType: this.bookingType(),
      rooms,
      checkInDate:  checkIn?.toISOString(),
      checkOutDate: checkOut?.toISOString(),
      checkInTime:  isHourly ? (val.checkInTime  ?? null) : null,
      checkOutTime: isHourly ? (val.checkOutTime ?? null) : null
    }).subscribe({
      next: () => {
        this.snack.open('Booking created!', '', { duration: 2000 });
        this.showForm.set(false);
        this.bookingType.set(BookingType.NightStay);
        this.loadBookings();
      },
      error: (err) => this.snack.open(err?.error?.message ?? 'Error creating booking.', 'Dismiss', { duration: 3000 })
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
