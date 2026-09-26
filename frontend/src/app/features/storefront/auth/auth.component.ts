import { Component, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { StorefrontAuthService } from '../../../core/services/storefront-auth.service';

@Component({
  selector: 'app-storefront-auth',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth.component.html'
})
export class AuthComponent {
  isLoginMode = true;
  isLoading = false;
  error = '';
  
  authService = inject(StorefrontAuthService);
  router = inject(Router);
  route = inject(ActivatedRoute);
  location = inject(Location);

  loginData = { email: '', password: '' };
  registerData = { username: '', email: '', password: '', confirmPassword: '' };

  toggleMode(mode: 'login' | 'register') {
    this.isLoginMode = mode === 'login';
    this.error = '';
  }

  goBack() {
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl) {
      this.router.navigateByUrl(returnUrl);
    } else {
      this.location.back();
    }
  }

  onSubmit() {
    this.isLoading = true;
    this.error = '';
    
    const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    
    if (this.isLoginMode) {
      this.authService.login(this.loginData).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          this.isLoading = false;
          this.error = err.error?.message || 'Login failed';
        }
      });
    } else {
      if (this.registerData.password !== this.registerData.confirmPassword) {
        this.error = 'Passwords do not match.';
        this.isLoading = false;
        return;
      }
      
      this.authService.register({
        username: this.registerData.username,
        email: this.registerData.email,
        password: this.registerData.password
      }).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          this.isLoading = false;
          this.error = err.error?.message || 'Registration failed';
        }
      });
    }
  }
}

