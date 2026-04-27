import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { PaymentDTO } from '../../model/api.models';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss',
})
export class PaymentsComponent implements OnInit {
  private readonly api   = inject(ApiService);
  private readonly auth  = inject(AuthService);
  private readonly snack = inject(MatSnackBar);

  readonly payments = signal<PaymentDTO[]>([]);
  readonly loading  = signal(true);

  readonly isAdmin = computed(() =>
    this.auth.hasRole('Administrator') || this.auth.hasRole('SuperAdmin')
  );

  readonly displayedColumns = ['id', 'bookingId', 'amount', 'paymentDate', 'status'];

  readonly statusColorMap: Record<string, string> = {
    Pending:   'accent',
    Completed: 'primary',
    Failed:    'warn',
    Refunded:  '',
  };

  ngOnInit(): void {
    const endpoint = this.isAdmin() ? 'payments' : 'payments/my';
    this.api.get<PaymentDTO[]>(endpoint).subscribe({
      next: (data) => {
        this.payments.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Failed to load payments.', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }
}
