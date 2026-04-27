import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { InvoiceDTO } from '../../model/api.models';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss',
})
export class InvoicesComponent implements OnInit {
  private readonly api  = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly snack = inject(MatSnackBar);

  readonly invoices = signal<InvoiceDTO[]>([]);
  readonly loading  = signal(true);

  readonly isAdmin = computed(() =>
    this.auth.hasRole('Administrator') || this.auth.hasRole('SuperAdmin')
  );

  readonly displayedColumns = computed(() =>
    this.isAdmin()
      ? ['id', 'guestName', 'roomNumber', 'issuedDate', 'dueDate', 'totalAmount', 'status', 'actions']
      : ['id', 'roomNumber', 'issuedDate', 'dueDate', 'totalAmount', 'status', 'actions']
  );

  readonly statusColorMap: Record<string, string> = {
    Unpaid:       'warn',
    Paid:         'primary',
    PartiallyPaid:'accent',
    Overdue:      'warn',
    Cancelled:    '',
  };

  ngOnInit(): void {
    const endpoint = this.isAdmin() ? 'invoices' : 'invoices/my';
    this.api.get<InvoiceDTO[]>(endpoint).subscribe({
      next: (data) => {
        this.invoices.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Failed to load invoices.', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  downloadPdf(id: number): void {
    window.open(`/api/invoices/${id}/pdf`, '_blank');
  }

  updateStatus(invoice: InvoiceDTO, status: string): void {
    this.api.put<InvoiceDTO>(`invoices/${invoice.id}`, { ...invoice, status }).subscribe({
      next: (updated) => {
        this.invoices.update(list =>
          list.map(i => i.id === updated.id ? updated : i)
        );
        this.snack.open('Invoice updated.', 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Update failed.', 'Close', { duration: 3000 })
    });
  }
}
