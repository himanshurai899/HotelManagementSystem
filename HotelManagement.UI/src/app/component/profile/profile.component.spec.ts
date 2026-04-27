import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ProfileComponent } from './profile.component';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

const mockProfile = {
  id: 1,
  username: 'testuser',
  firstName: 'Test',
  lastName: 'User',
  email: 'test@test.com',
  phoneNumber: '1234567890',
  profilePhotoUrl: null,
  roleId: 3,
  roleName: 'Customer',
  roles: ['Customer'],
  claims: [],
};

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let fixture: ComponentFixture<ProfileComponent>;
  let apiSpy: jasmine.SpyObj<ApiService>;

  const mockAuthService = {
    profilePhotoUrl: signal<string | null>(null),
    token: signal<string | null>('mock'),
    setProfilePhotoUrl: jasmine.createSpy('setProfilePhotoUrl'),
    isLoggedIn: () => true,
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiService', ['get', 'put', 'post']);
    apiSpy.get.and.returnValue(of(mockProfile));

    await TestBed.configureTestingModule({
      imports: [ProfileComponent, NoopAnimationsModule],
      providers: [
        { provide: ApiService,  useValue: apiSpy },
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture   = TestBed.createComponent(ProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call GET profile on init', () => {
    expect(apiSpy.get).toHaveBeenCalledWith('profile');
  });

  it('should populate profile signal after load', () => {
    expect(component.profile()).toBeTruthy();
    expect(component.profile()?.username).toBe('testuser');
    expect(component.profile()?.firstName).toBe('Test');
    expect(component.profile()?.lastName).toBe('User');
  });

  it('should patch profileForm with loaded data', () => {
    expect(component.profileForm.value.userName).toBe('testuser');
    expect(component.profileForm.value.email).toBe('test@test.com');
  });

  it('should return initials from firstName + lastName', () => {
    expect(component.initials).toBe('TU');
  });

  it('should not call PUT when profileForm is invalid', () => {
    component.profileForm.get('email')?.setValue('not-an-email');
    component.saveProfile();
    expect(apiSpy.put).not.toHaveBeenCalled();
  });

  it('should call PUT profile on saveProfile', () => {
    apiSpy.put.and.returnValue(of(mockProfile));
    component.profileForm.setValue({
      userName: 'testuser', firstName: 'Test',
      lastName: 'User', email: 'test@test.com', phoneNumber: '1234567890',
    });
    component.saveProfile();
    expect(apiSpy.put).toHaveBeenCalledWith('profile', jasmine.any(Object));
  });

  it('should set passwordError when passwords do not match', () => {
    component.passwordForm.setValue({
      currentPassword: 'Old@1', newPassword: 'New@123A', confirmNewPassword: 'Different@1',
    });
    component.changePassword();
    expect(component.passwordError()).toBeTruthy();
  });

  it('should call PUT profile/change-password on changePassword', () => {
    apiSpy.put.and.returnValue(of({ message: 'Password changed successfully.' }));
    component.passwordForm.setValue({
      currentPassword: 'Old@1', newPassword: 'New@1A', confirmNewPassword: 'New@1A',
    });
    component.changePassword();
    expect(apiSpy.put).toHaveBeenCalledWith('profile/change-password', jasmine.any(Object));
  });

  it('should show loading spinner before data arrives', () => {
    component.loading.set(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('mat-spinner')).toBeTruthy();
  });
});
