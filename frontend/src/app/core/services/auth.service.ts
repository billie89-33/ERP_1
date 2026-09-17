import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { throwError } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/auth'; // Hardcode for now, can move to env later

  // Using Angular Signal to store current user globally
  currentUser = signal<{id: number, username: string, role: string} | null>(null);

  login(credentials: any) {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        // Save user info in Signal and LocalStorage (just for UI state, token is in HttpOnly Cookie)
        this.currentUser.set(res.user);
        localStorage.setItem('user', JSON.stringify(res.user));
      }),
      catchError(err => {
        return throwError(() => err.error?.message || 'Login failed');
      })
    );
  }

  logout() {
    this.currentUser.set(null);
    localStorage.removeItem('user');
    // We should ideally call a /logout API to clear the cookie, but for now we'll just clear local state
  }

  loadUserFromStorage() {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        this.currentUser.set(JSON.parse(userStr));
      } catch (e) {
        this.logout();
      }
    }
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.role === role;
  }
}
