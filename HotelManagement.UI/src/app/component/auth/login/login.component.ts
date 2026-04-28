import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth.service';
import { AuthResponse, UserDTO } from '../../../model/api.models';
import { environment } from '../../../../environments/environment';

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
        const destination = roles.includes('Administrator') || roles.includes('SuperAdmin')
          ? '/admin-dashboard'
          : roles.includes('Customer') ? '/customer-dashboard' : '/guest-dashboard';

        // Eagerly load the user profile so the navbar shows the correct
        // name and avatar immediately — without needing to visit /profile first.
        this.api.get<UserDTO>('profile').subscribe({
          next: p => {
            const origin = environment.apiBaseUrl.replace(/\/api\/?$/, '');
            const photoUrl = p.profilePhotoUrl
              ? (p.profilePhotoUrl.startsWith('http') ? p.profilePhotoUrl : `${origin}${p.profilePhotoUrl}`)
              : null;
            this.auth.setProfilePhotoUrl(photoUrl);
            const fullName = [p.firstName, p.lastName].filter(Boolean).join(' ') || p.username;
            this.auth.setFullName(fullName);
          },
          // ignore errors — nav proceeds regardless
          error: () => {}
        });

        this.router.navigate([destination]);
      },
      error: () => {
        this.error.set('Invalid username or password. Please try again.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
