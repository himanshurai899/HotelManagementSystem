import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Router } from '@angular/router';

import { errorInterceptor } from './error.interceptor';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let notify: jasmine.SpyObj<NotificationService>;
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    notify = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'error',
      'success',
      'info',
    ]);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['logout', 'getToken']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notify },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function trigger(status: number, body: object | string = {}, statusText = 'Error') {
    http.get('/test').subscribe({ next: () => {}, error: () => {} });
    httpMock.expectOne('/test').flush(body, { status, statusText });
  }

  it('shows friendly message and logs out on 401', () => {
    trigger(401);
    expect(notify.error).toHaveBeenCalled();
    expect(auth.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('does NOT logout/redirect on 401 from /account/login', () => {
    http.post('https://localhost:7204/api/account/login', {}).subscribe({
      next: () => {},
      error: () => {},
    });
    httpMock
      .expectOne('https://localhost:7204/api/account/login')
      .flush({ detail: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });
    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does NOT logout/redirect on 401 from /account/register', () => {
    http.post('https://localhost:7204/api/account/register', {}).subscribe({
      next: () => {},
      error: () => {},
    });
    httpMock
      .expectOne('https://localhost:7204/api/account/register')
      .flush({ detail: 'Email already used' }, { status: 401, statusText: 'Unauthorized' });
    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('redirects to /unauthorized on 403', () => {
    trigger(403);
    expect(notify.error).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/unauthorized']);
  });

  it('uses ProblemDetails detail message on 404', () => {
    trigger(404, { title: 'Resource not found', detail: 'Booking 5 not found.' });
    expect(notify.error).toHaveBeenCalledWith(jasmine.stringMatching(/Booking 5 not found/));
  });

  it('uses ProblemDetails detail message on 400', () => {
    trigger(400, { title: 'Validation failed', detail: 'Check-in must be in the future.' });
    expect(notify.error).toHaveBeenCalledWith(jasmine.stringMatching(/Check-in must be in the future/));
  });

  it('shows generic message on 500', () => {
    trigger(500, { detail: 'oops' });
    expect(notify.error).toHaveBeenCalled();
    const msg = (notify.error.calls.mostRecent().args[0] as string).toLowerCase();
    expect(msg).toContain('unexpected');
  });

  it('shows network message on status 0', () => {
    trigger(0, '', 'Unknown Error');
    expect(notify.error).toHaveBeenCalled();
    const msg = (notify.error.calls.mostRecent().args[0] as string).toLowerCase();
    expect(msg).toMatch(/network|server|connect/);
  });

  it('re-throws the error so callers can still react', (done) => {
    http.get('/test').subscribe({
      next: () => done.fail('should have errored'),
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(404);
        done();
      },
    });
    httpMock.expectOne('/test').flush({ detail: 'x' }, { status: 404, statusText: 'Not Found' });
  });
});
