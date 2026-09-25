import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
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

  loginData = { email: '', password: '' };
  registerData = { firstName: '', lastName: '', email: '', phone: '', password: '' };

  toggleMode() {
    this.isLoginMode = !this.isLoginMode;
    this.error = '';
  }

  onSubmit() {
    this.isLoading = true;
    this.error = '';
    
    // ดึง returnUrl ถ้ามี ถ้าไม่มีให้กลับไปหน้าแรก
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
      this.authService.register(this.registerData).subscribe({
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
