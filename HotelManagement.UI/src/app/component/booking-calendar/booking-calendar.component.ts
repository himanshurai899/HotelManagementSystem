import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../services/api.service';
import { BookingCalendarEntry } from '../../model/api.models';

export interface CalendarDay {
  /** null for padding cells before the 1st of the month */
  date: Date | null;
  /** day-of-month number, or null for padding */
  dayNum: number | null;
  /** bookings whose range includes this day */
  bookings: BookingCalendarEntry[];
}

@Component({
  selector: 'app-booking-calendar',
  standalone: true,
  imports: [
    NgClass,
    MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule, MatTooltipModule, MatChipsModule
  ],
  templateUrl: './booking-calendar.component.html',
  styleUrl: './booking-calendar.component.scss'
})
export class BookingCalendarComponent implements OnInit {
  private api = inject(ApiService);

  viewYear  = signal(new Date().getFullYear());
  viewMonth = signal(new Date().getMonth() + 1); // 1-indexed

  loading = signal(true);
  entries = signal<BookingCalendarEntry[]>([]);

  readonly weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  monthLabel = computed(() =>
    new Date(this.viewYear(), this.viewMonth() - 1, 1)
      .toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
  );

  /**
   * Builds the 6-row × 7-col grid for the current month.
   * Leading/trailing cells are padding (date = null).
   */
  calendarWeeks = computed<CalendarDay[][]>(() => {
    const year  = this.viewYear();
    const month = this.viewMonth();          // 1-indexed
    const daysInMonth = new Date(year, month, 0).getDate();
    const firstDow    = new Date(year, month - 1, 1).getDay(); // 0=Sun

    const cells: CalendarDay[] = [];

    // Leading padding
    for (let p = 0; p < firstDow; p++) {
      cells.push({ date: null, dayNum: null, bookings: [] });
    }

    // Actual days
    for (let d = 1; d <= daysInMonth; d++) {
      const date = new Date(year, month - 1, d);
      cells.push({ date, dayNum: d, bookings: this._bookingsForDate(date) });
    }

    // Trailing padding to complete the last week
    while (cells.length % 7 !== 0) {
      cells.push({ date: null, dayNum: null, bookings: [] });
    }

    // Split into weeks
    const weeks: CalendarDay[][] = [];
    for (let i = 0; i < cells.length; i += 7) {
      weeks.push(cells.slice(i, i + 7));
    }
    return weeks;
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<BookingCalendarEntry[]>(
      `bookings/calendar?year=${this.viewYear()}&month=${this.viewMonth()}`
    ).subscribe({
      next: d => { this.entries.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  prevMonth() {
    if (this.viewMonth() === 1) { this.viewYear.update(y => y - 1); this.viewMonth.set(12); }
    else { this.viewMonth.update(m => m - 1); }
    this.load();
  }

  nextMonth() {
    if (this.viewMonth() === 12) { this.viewYear.update(y => y + 1); this.viewMonth.set(1); }
    else { this.viewMonth.update(m => m + 1); }
    this.load();
  }

  goToday() {
    this.viewYear.set(new Date().getFullYear());
    this.viewMonth.set(new Date().getMonth() + 1);
    this.load();
  }

  isToday(date: Date | null): boolean {
    if (!date) return false;
    const t = new Date();
    return date.getFullYear() === t.getFullYear()
        && date.getMonth()     === t.getMonth()
        && date.getDate()      === t.getDate();
  }

  statusClass(status: string): string {
    if (status === 'Confirmed') return 'chip-confirmed';
    if (status === 'Pending')   return 'chip-pending';
    return 'chip-enquiry';
  }

  tooltip(e: BookingCalendarEntry): string {
    return `Room ${e.roomNumber}\n${e.customerName}\n${e.checkInDate} → ${e.checkOutDate}\n[${e.status}]`;
  }

  private _bookingsForDate(date: Date): BookingCalendarEntry[] {
    return this.entries().filter(e => {
      const checkIn  = new Date(e.checkInDate  + 'T00:00:00');
      const checkOut = new Date(e.checkOutDate + 'T00:00:00');
      // Include checkout day: guest checks out at 10 AM so the room shows occupied that day
      return date >= checkIn && date <= checkOut;
    });
  }
}
