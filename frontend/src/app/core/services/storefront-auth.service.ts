import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { StorefrontUser, LoginCredentials, StorefrontRegisterData, StorefrontAuthResponse } from '../models/erp.model';

@Injectable({
  providedIn: 'root'
})
export class StorefrontAuthService {
  private http = inject(HttpClient);
  
  currentUser = signal<StorefrontUser | null>(null);
  
  constructor() {
    this.checkSession();
  }

  checkSession() {
    const userStr = localStorage.getItem('storefront_user');
    if (userStr) {
      try {
        this.currentUser.set(JSON.parse(userStr));
      } catch {
        this.logout();
      }
    }
  }

  login(credentials: LoginCredentials) {
    return this.http.post<StorefrontAuthResponse>('/api/StorefrontAuth/login', credentials, { withCredentials: true }).pipe(
      tap(res => {
        localStorage.setItem('storefront_user', JSON.stringify(res.user));
        if (res.token) localStorage.setItem('jamine_token', res.token);
        this.currentUser.set(res.user);
      })
    );
  }

  register(data: StorefrontRegisterData) {
    return this.http.post<StorefrontAuthResponse>('/api/StorefrontAuth/register', data, { withCredentials: true }).pipe(
      tap(res => {
        localStorage.setItem('storefront_user', JSON.stringify(res.user));
        if (res.token) localStorage.setItem('jamine_token', res.token);
        this.currentUser.set(res.user);
      })
    );
  }

  logout() {
    localStorage.removeItem('storefront_user');
    localStorage.removeItem('jamine_token');
    this.currentUser.set(null);
    return this.http.post('/api/StorefrontAuth/logout', {}, { withCredentials: true });
  }
}
