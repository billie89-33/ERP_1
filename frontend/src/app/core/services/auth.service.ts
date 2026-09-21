import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { throwError, Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5243/api/auth';

  currentUser = signal<{id: string, username: string, role: string} | null>(null);

  login(credentials: any) {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        this.currentUser.set(res.user);
        localStorage.setItem('user', JSON.stringify(res.user));
      }),
      catchError(err => {
        return throwError(() => err.error?.message || 'Login failed');
      })
    );
  }

  logout() {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => {
        this.currentUser.set(null);
        localStorage.removeItem('user');
      }),
      catchError(() => {
        this.currentUser.set(null);
        localStorage.removeItem('user');
        return of(null);
      })
    ).subscribe();
  }

  checkSession(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.currentUser.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      }),
      catchError(() => {
        this.currentUser.set(null);
        localStorage.removeItem('user');
        return of(null);
      })
    );
  }

  loadUserFromStorage() {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        this.currentUser.set(JSON.parse(userStr));
      } catch (e) {
        this.currentUser.set(null);
      }
    }
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.role === role;
  }
}
