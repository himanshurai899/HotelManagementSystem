import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  template: '' // Immediately redirects — no UI needed here
})
export class DashboardComponent implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);

  ngOnInit() {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    if (this.auth.hasAnyRole(['Administrator', 'SuperAdmin'])) this.router.navigate(['/admin-dashboard']);
    else if (this.auth.hasRole('Customer'))                    this.router.navigate(['/customer-dashboard']);
    else                                                        this.router.navigate(['/guest-dashboard']);
  }
}
