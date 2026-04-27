import { Component, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ChangePasswordRequest, UpdateProfileRequest, UserDTO } from '../../model/api.models';

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPwd     = group.get('newPassword')?.value;
  const confirmPwd = group.get('confirmNewPassword')?.value;
  return newPwd && confirmPwd && newPwd !== confirmPwd
    ? { passwordMismatch: true }
    : null;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private api   = inject(ApiService);
  private auth  = inject(AuthService);
  private fb    = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  profile      = signal<UserDTO | null>(null);
  loading      = signal(true);
  saving       = signal(false);
  uploading    = signal(false);
  previewUrl   = signal<string | null>(null);
  passwordError = signal<string | null>(null);
  passwordSaving = signal(false);

  profileForm = this.fb.group({
    userName:    ['', [Validators.required, Validators.minLength(3)]],
    firstName:   [''],
    lastName:    [''],
    email:       ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.pattern(/^\d{10}$/)]],
  });

  /** Password rules mirror IdentityOptions in Program.cs:
   *  min 6 chars · at least 1 uppercase · at least 1 digit */
  passwordForm = this.fb.group(
    {
      currentPassword:    ['', [Validators.required]],
      newPassword:        ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/),
      ]],
      confirmNewPassword: ['', [Validators.required]],
    },
    { validators: passwordMatchValidator }
  );

  ngOnInit() { this.loadProfile(); }

  loadProfile() {
    this.loading.set(true);
    this.api.get<UserDTO>('profile').subscribe({
      next: p => {
        this.profile.set(p);
        this.previewUrl.set(p.profilePhotoUrl ?? null);
        this.profileForm.patchValue({
          userName:    p.username,
          firstName:   p.firstName,
          lastName:    p.lastName,
          email:       p.email,
          phoneNumber: p.phoneNumber,
        });
      },
      error: () => this.snack.open('Failed to load profile.', 'Dismiss', { duration: 3000 }),
      complete: () => this.loading.set(false),
    });
  }

  saveProfile() {
    if (this.profileForm.invalid) return;
    this.saving.set(true);
    const payload: UpdateProfileRequest = {
      userName:    this.profileForm.value.userName    ?? '',
      firstName:   this.profileForm.value.firstName   ?? '',
      lastName:    this.profileForm.value.lastName    ?? '',
      email:       this.profileForm.value.email       ?? '',
      phoneNumber: this.profileForm.value.phoneNumber ?? '',
    };
    this.api.put<UserDTO>('profile', payload).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.snack.open('Profile updated!', '', { duration: 2500 });
      },
      error: () => this.snack.open('Failed to update profile.', 'Dismiss', { duration: 3000 }),
      complete: () => this.saving.set(false),
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      this.snack.open('File must be under 2 MB.', 'Dismiss', { duration: 3000 });
      return;
    }
    if (!['image/jpeg', 'image/png'].includes(file.type)) {
      this.snack.open('Only JPG and PNG files are allowed.', 'Dismiss', { duration: 3000 });
      return;
    }

    // Instant local preview while uploading
    const reader  = new FileReader();
    reader.onload = () => this.previewUrl.set(reader.result as string);
    reader.readAsDataURL(file);

    this.uploading.set(true);
    const formData = new FormData();
    formData.append('photo', file);

    // Do NOT set Content-Type manually — Angular sets multipart/form-data automatically
    this.api.post<{ profilePhotoUrl: string }>('profile/photo', formData).subscribe({
      next: res => {
        this.auth.setProfilePhotoUrl(res.profilePhotoUrl);
        this.snack.open('Photo uploaded!', '', { duration: 2500 });
      },
      error: () => this.snack.open('Upload failed.', 'Dismiss', { duration: 3000 }),
      complete: () => this.uploading.set(false),
    });
  }

  changePassword() {
    this.passwordError.set(null);
    if (this.passwordForm.hasError('passwordMismatch')) {
      this.passwordError.set('New password and confirmation do not match.');
      this.passwordForm.markAllAsTouched();
      return;
    }
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.passwordSaving.set(true);
    const payload: ChangePasswordRequest = {
      currentPassword:    this.passwordForm.value.currentPassword    ?? '',
      newPassword:        this.passwordForm.value.newPassword        ?? '',
      confirmNewPassword: this.passwordForm.value.confirmNewPassword ?? '',
    };
    this.api.put<{ message: string }>('profile/change-password', payload).subscribe({
      next: res => {
        this.snack.open(res.message, '', { duration: 2500 });
        this.passwordForm.reset();
      },
      error: err => {
        const msg = err?.error?.[0]?.description
          ?? err?.error?.message
          ?? 'Password change failed.';
        this.passwordError.set(msg);
      },
      complete: () => this.passwordSaving.set(false),
    });
  }

  /** Derived initials for the avatar fallback */
  get initials(): string {
    const p     = this.profile();
    if (!p) return '?';
    const first = (p.firstName || p.username || '?')[0] ?? '?';
    const last  = (p.lastName || '')[0] ?? '';
    return (first + last).toUpperCase();
  }
}
