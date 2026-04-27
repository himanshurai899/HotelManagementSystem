import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ApiService } from '../../services/api.service';
import { CompanyProfileDTO } from '../../model/api.models';

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './company-profile.component.html',
  styleUrl: './company-profile.component.scss',
})
export class CompanyProfileComponent implements OnInit {
  private readonly api   = inject(ApiService);
  private readonly fb    = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly profile  = signal<CompanyProfileDTO | null>(null);
  readonly loading  = signal(true);
  readonly saving   = signal(false);
  readonly logoPreview = signal<string | null>(null);

  readonly fontOptions = ['Arial', 'Helvetica', 'Times New Roman', 'Georgia', 'Courier New', 'Verdana'];

  readonly profileForm: FormGroup = this.fb.group({
    companyName:  [''],
    address:      [''],
    gstinNumber:  [''],
    phoneNumber:  [''],
    email:        [''],
    website:      [''],
    primaryColor: ['#1a73e8'],
    accentColor:  ['#f5f5f5'],
    fontFamily:   ['Arial'],
  });

  ngOnInit(): void {
    this.api.get<CompanyProfileDTO>('companyprofile').subscribe({
      next: (data) => {
        this.profile.set(data);
        if (data) {
          this.profileForm.patchValue(data);
          this.logoPreview.set(data.logoUrl);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  save(): void {
    if (this.profileForm.invalid) return;
    this.saving.set(true);

    const payload: CompanyProfileDTO = {
      ...this.profile(),
      id: this.profile()?.id ?? 0,
      ...this.profileForm.value,
      logoUrl: this.profile()?.logoUrl ?? null,
    };

    this.api.put<CompanyProfileDTO>('companyprofile', payload).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.saving.set(false);
        this.snack.open('Company profile saved.', 'Close', { duration: 2500 });
      },
      error: () => {
        this.saving.set(false);
        this.snack.open('Failed to save profile.', 'Close', { duration: 3000 });
      }
    });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];

    // Show local preview immediately
    const reader = new FileReader();
    reader.onload = (e) => this.logoPreview.set(e.target?.result as string);
    reader.readAsDataURL(file);

    // Upload to server
    const formData = new FormData();
    formData.append('file', file);

    this.api.post<{ logoUrl: string }>('companyprofile/logo', formData).subscribe({
      next: (res) => {
        this.profile.update(p => p ? { ...p, logoUrl: res.logoUrl } : p);
        this.snack.open('Logo uploaded.', 'Close', { duration: 2500 });
      },
      error: () => this.snack.open('Logo upload failed.', 'Close', { duration: 3000 })
    });
  }
}
