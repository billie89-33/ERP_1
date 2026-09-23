import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StorefrontAuthService {
  private http = inject(HttpClient);
  
  currentUser = signal<any>(null);
  
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

  login(credentials: any) {
    return this.http.post<any>('http://localhost:5243/api/StorefrontAuth/login', credentials, { withCredentials: true }).pipe(
      tap(res => {
        localStorage.setItem('storefront_user', JSON.stringify(res.user));
        this.currentUser.set(res.user);
      })
    );
  }

  register(data: any) {
    return this.http.post<any>('http://localhost:5243/api/StorefrontAuth/register', data, { withCredentials: true }).pipe(
      tap(res => {
        localStorage.setItem('storefront_user', JSON.stringify(res.user));
        this.currentUser.set(res.user);
      })
    );
  }

  logout() {
    localStorage.removeItem('storefront_user');
    this.currentUser.set(null);
    return this.http.post('http://localhost:5243/api/StorefrontAuth/logout', {}, { withCredentials: true });
  }
}
