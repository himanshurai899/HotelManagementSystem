import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { AuthResponse } from '../../../model/api.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');
  hidePassword = signal(true);

  form = this.fb.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');

    this.api.post<AuthResponse>('account/login', this.form.value).subscribe({
      next: res => {
        if (!res.token) {
          this.error.set('Login succeeded but no token was returned.');
          this.loading.set(false);
          return;
        }
        this.auth.setToken(res.token);
        const roles = this.auth.getRoles();
        if (roles.includes('Administrator') || roles.includes('SuperAdmin')) this.router.navigate(['/admin-dashboard']);
        else if (roles.includes('Customer')) this.router.navigate(['/customer-dashboard']);
        else this.router.navigate(['/guest-dashboard']);
      },
      error: () => {
        this.error.set('Invalid username or password. Please try again.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
