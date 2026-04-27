import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { PaymentsComponent } from './payments.component';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { PaymentDTO } from '../../model/api.models';

describe('PaymentsComponent', () => {
  let apiSpy:  jasmine.SpyObj<ApiService>;
  let authSpy: jasmine.SpyObj<AuthService>;

  const mockPayments: PaymentDTO[] = [
    { id: 1, bookingId: 1, amount: 300, paymentDate: '2026-04-01', status: 'Completed' },
    { id: 2, bookingId: 2, amount: 450, paymentDate: '2026-04-05', status: 'Pending'   }
  ];

  beforeEach(async () => {
    apiSpy  = jasmine.createSpyObj('ApiService',  ['get', 'post', 'put', 'delete']);
    authSpy = jasmine.createSpyObj('AuthService', ['hasRole', 'getUserId']);

    apiSpy.get.and.returnValue(of([]));
    authSpy.hasRole.and.returnValue(false);
    authSpy.getUserId.and.returnValue(1);

    await TestBed.configureTestingModule({
      imports: [PaymentsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ApiService,  useValue: apiSpy  },
        { provide: AuthService, useValue: authSpy }
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(PaymentsComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should call the correct API endpoint on init for admin', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockPayments));

    const fixture = TestBed.createComponent(PaymentsComponent);
    fixture.detectChanges();

    expect(apiSpy.get).toHaveBeenCalledWith('payments');
  });

  it('should call my-payments endpoint for customer', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Customer');
    apiSpy.get.and.returnValue(of(mockPayments));

    const fixture = TestBed.createComponent(PaymentsComponent);
    fixture.detectChanges();

    expect(apiSpy.get).toHaveBeenCalledWith('payments/my');
  });

  it('should display loading state before data arrives', () => {
    apiSpy.get.and.returnValue(of([]));

    const fixture = TestBed.createComponent(PaymentsComponent);
    const component = fixture.componentInstance;

    expect(component.loading()).toBeTrue();
  });

  it('should populate payments signal after data loads', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockPayments));

    const fixture = TestBed.createComponent(PaymentsComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.payments().length).toBe(2);
    expect(fixture.componentInstance.payments()[0].amount).toBe(300);
  });

  it('should render a row per payment in the table', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockPayments));

    const fixture = TestBed.createComponent(PaymentsComponent);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('mat-row, tr[mat-row]');
    expect(rows.length).toBe(mockPayments.length);
  });
});
