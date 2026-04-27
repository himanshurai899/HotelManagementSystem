import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { CompanyProfileComponent } from './company-profile.component';
import { ApiService } from '../../services/api.service';
import { CompanyProfileDTO } from '../../model/api.models';

describe('CompanyProfileComponent', () => {
  let apiSpy: jasmine.SpyObj<ApiService>;

  const mockProfile: CompanyProfileDTO = {
    id: 1,
    companyName: 'Grand Hotel',
    logoUrl: '/uploads/GrandHotel/assets/logo.png',
    address: '123 Main St, Mumbai',
    gstinNumber: '27AABCU9603R1ZX',
    phoneNumber: '+91 9876543210',
    email: 'info@grandhotel.com',
    website: 'https://grandhotel.com',
    primaryColor: '#1a73e8',
    accentColor: '#f5f5f5',
    fontFamily: 'Arial'
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiService', ['get', 'put', 'post']);
    apiSpy.get.and.returnValue(of(null));

    await TestBed.configureTestingModule({
      imports: [CompanyProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ApiService, useValue: apiSpy }
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CompanyProfileComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should call the correct API endpoint on init', () => {
    apiSpy.get.and.returnValue(of(mockProfile));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    fixture.detectChanges();

    expect(apiSpy.get).toHaveBeenCalledWith('companyprofile');
  });

  it('should populate profile signal after data loads', () => {
    apiSpy.get.and.returnValue(of(mockProfile));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.profile()?.companyName).toBe('Grand Hotel');
    expect(fixture.componentInstance.profile()?.gstinNumber).toBe('27AABCU9603R1ZX');
  });

  it('should display loading state before data arrives', () => {
    apiSpy.get.and.returnValue(of(null));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    const component = fixture.componentInstance;

    expect(component.loading()).toBeTrue();
  });

  it('should show logo preview when logoUrl is present in profile', () => {
    apiSpy.get.and.returnValue(of(mockProfile));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    fixture.detectChanges();

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.company-logo');
    expect(img).not.toBeNull();
    expect(img!.src).toContain('logo.png');
  });

  it('should expose all customization fields in the form', () => {
    apiSpy.get.and.returnValue(of(mockProfile));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    fixture.detectChanges();

    // All branding fields must be bound to the reactive form
    const form = fixture.componentInstance.profileForm;
    expect(form.contains('companyName')).toBeTrue();
    expect(form.contains('address')).toBeTrue();
    expect(form.contains('gstinNumber')).toBeTrue();
    expect(form.contains('phoneNumber')).toBeTrue();
    expect(form.contains('email')).toBeTrue();
    expect(form.contains('website')).toBeTrue();
    expect(form.contains('primaryColor')).toBeTrue();
    expect(form.contains('accentColor')).toBeTrue();
    expect(form.contains('fontFamily')).toBeTrue();
  });

  it('should call PUT on save with updated profile data', () => {
    apiSpy.get.and.returnValue(of(mockProfile));
    apiSpy.put.and.returnValue(of(mockProfile));

    const fixture = TestBed.createComponent(CompanyProfileComponent);
    fixture.detectChanges();

    fixture.componentInstance.save();

    expect(apiSpy.put).toHaveBeenCalledWith('companyprofile', jasmine.any(Object));
  });
});
