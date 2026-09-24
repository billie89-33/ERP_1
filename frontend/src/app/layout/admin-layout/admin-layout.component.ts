import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, Router, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit {
  authService = inject(AuthService);
  private router = inject(Router);
  private http = inject(HttpClient);

  pendingSlipCount = 0;

  ngOnInit() {
    this.fetchNotifications();
    setInterval(() => this.fetchNotifications(), 30000);
  }

  fetchNotifications() {
    this.http.get<any>('http://localhost:5243/api/Dashboard/stats', { withCredentials: true }).subscribe({
      next: (data) => {
        this.pendingSlipCount = data.pendingSlipVerifications || 0;
      },
      error: () => {}
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  hasRole(allowedRoles: string[]): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    return allowedRoles.includes(user.role);
  }
}
