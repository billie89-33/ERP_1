import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink, Router } from '@angular/router';
import { StorefrontAuthService } from '../../../core/services/storefront-auth.service';

@Component({
  selector: 'app-customer-portal',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './customer-portal.component.html'
})
export class CustomerPortalComponent implements OnInit {
  authService = inject(StorefrontAuthService);
  http = inject(HttpClient);
  router = inject(Router);

  orders: any[] = [];
  isLoading = true;

  ngOnInit() {
    if (!this.authService.currentUser()) {
      this.router.navigate(['/shop/login']);
      return;
    }

    const token = localStorage.getItem('storefront_user') 
      ? JSON.parse(localStorage.getItem('storefront_user') || '{}')?.token // wait, our auth service saves 'user' but not raw token. Let's fix this in auth service. 
      // Actually AuthController returns { token, user: {...} }. StorefrontAuthService saves the whole thing or just user?
      // Let's use standard withCredentials or get token from localstorage.
      : null;
      
    this.fetchOrders();
  }

  fetchOrders() {
    // If not using interceptor, we might need to send token manually, but we can just let interceptor handle it if we have one.
    // Wait, the ERP uses cookies! StorefrontAuthController also uses SetJwtCookie!
    // So withCredentials: true is all we need!
    
    this.http.get<any[]>('/api/Storefront/my-orders', { withCredentials: true }).subscribe({
      next: (data) => {
        this.orders = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  logout() {
    this.authService.logout().subscribe(() => {
      this.router.navigate(['/']);
    });
  }
}
