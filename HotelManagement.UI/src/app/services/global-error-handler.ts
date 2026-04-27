import { ErrorHandler, Injectable, inject } from '@angular/core';
import { NotificationService } from './notification.service';

/**
 * Catches uncaught client-side errors (template/runtime) that don't originate
 * from HTTP. HTTP errors are handled by errorInterceptor. This handler ensures
 * the user always sees a friendly toast instead of a silent failure.
 */
@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
  private readonly notify = inject(NotificationService);

  handleError(error: unknown): void {
    // Always log to console so devs can still see the original stack.
    // eslint-disable-next-line no-console
    console.error('Unhandled client error:', error);
    this.notify.error('Something went wrong. Please try again.');
  }
}
