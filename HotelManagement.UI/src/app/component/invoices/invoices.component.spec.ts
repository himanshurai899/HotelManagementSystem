import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { InvoicesComponent } from './invoices.component';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { InvoiceDTO } from '../../model/api.models';

describe('InvoicesComponent', () => {
  let apiSpy: jasmine.SpyObj<ApiService>;
  let authSpy: jasmine.SpyObj<AuthService>;

  const mockInvoices: InvoiceDTO[] = [
    {
      id: 1,
      bookingId: 1,
      guestName: 'John Doe',
      roomNumber: '101',
      issuedDate: '2026-04-01',
      dueDate: '2026-04-15',
      totalAmount: 500,
      status: 'Unpaid',
      items: []
    },
    {
      id: 2,
      bookingId: 2,
      guestName: 'Jane Smith',
      roomNumber: '202',
      issuedDate: '2026-04-05',
      dueDate: '2026-04-20',
      totalAmount: 800,
      status: 'Paid',
      items: []
    }
  ];

  beforeEach(async () => {
    apiSpy  = jasmine.createSpyObj('ApiService',  ['get', 'post', 'put', 'delete']);
    authSpy = jasmine.createSpyObj('AuthService', ['hasRole', 'getUserId']);

    apiSpy.get.and.returnValue(of([]));
    authSpy.hasRole.and.returnValue(false);
    authSpy.getUserId.and.returnValue(1);

    await TestBed.configureTestingModule({
      imports: [InvoicesComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ApiService,  useValue: apiSpy  },
        { provide: AuthService, useValue: authSpy }
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(InvoicesComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should call the correct API endpoint on init for admin', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockInvoices));

    const fixture = TestBed.createComponent(InvoicesComponent);
    fixture.detectChanges(); // triggers ngOnInit

    expect(apiSpy.get).toHaveBeenCalledWith('invoices');
  });

  it('should call my-invoices endpoint for customer', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Customer');
    apiSpy.get.and.returnValue(of(mockInvoices));

    const fixture = TestBed.createComponent(InvoicesComponent);
    fixture.detectChanges();

    expect(apiSpy.get).toHaveBeenCalledWith('invoices/my');
  });

  it('should display loading state before data arrives', () => {
    apiSpy.get.and.returnValue(of([]));

    const fixture = TestBed.createComponent(InvoicesComponent);
    const component = fixture.componentInstance;

    // Before ngOnInit resolves, loading should be true
    expect(component.loading()).toBeTrue();
  });

  it('should populate invoices signal after data loads', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockInvoices));

    const fixture = TestBed.createComponent(InvoicesComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.invoices().length).toBe(2);
    expect(fixture.componentInstance.invoices()[0].guestName).toBe('John Doe');
  });

  it('should render a row per invoice in the table', () => {
    authSpy.hasRole.and.callFake((role: string) => role === 'Administrator');
    apiSpy.get.and.returnValue(of(mockInvoices));

    const fixture = TestBed.createComponent(InvoicesComponent);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('mat-row, tr[mat-row]');
    expect(rows.length).toBe(mockInvoices.length);
  });
});
