import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

/**
 * Centralized user-feedback service. Wraps MatSnackBar so toast styling,
 * duration, and position stay consistent across the app.
 * Inject this — never call MatSnackBar directly from components.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly defaultDuration = 5000;

  error(message: string): void {
    this.show(message, 'snackbar-error');
  }

  success(message: string): void {
    this.show(message, 'snackbar-success');
  }

  info(message: string): void {
    this.show(message, 'snackbar-info');
  }

  private show(message: string, panelClass: string): void {
    const config: MatSnackBarConfig = {
      duration: this.defaultDuration,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: [panelClass],
    };
    this.snackBar.open(message, 'Dismiss', config);
  }
}
