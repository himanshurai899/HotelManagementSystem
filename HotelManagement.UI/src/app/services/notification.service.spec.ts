import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;
  let snackBar: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    snackBar = jasmine.createSpyObj<MatSnackBar>('MatSnackBar', ['open']);
    TestBed.configureTestingModule({
      providers: [
        NotificationService,
        { provide: MatSnackBar, useValue: snackBar },
      ],
    });
    service = TestBed.inject(NotificationService);
  });

  it('opens snackbar with error panel class on error()', () => {
    service.error('Something broke');
    expect(snackBar.open).toHaveBeenCalled();
    const args = snackBar.open.calls.mostRecent().args;
    expect(args[0]).toBe('Something broke');
    const config = args[2] as { duration?: number; panelClass?: string | string[] };
    expect(config.duration).toBe(5000);
    expect(config.panelClass).toContain('snackbar-error');
  });

  it('opens snackbar with success panel class on success()', () => {
    service.success('Saved');
    const config = snackBar.open.calls.mostRecent().args[2] as { panelClass?: string | string[] };
    expect(config.panelClass).toContain('snackbar-success');
  });

  it('opens snackbar with info panel class on info()', () => {
    service.info('Heads up');
    const config = snackBar.open.calls.mostRecent().args[2] as { panelClass?: string | string[] };
    expect(config.panelClass).toContain('snackbar-info');
  });
});
