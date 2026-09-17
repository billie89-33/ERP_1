import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  credentials = { username: '', password: '' };
  errorMessage = '';
  isLoading = false;

  private authService = inject(AuthService);
  private router = inject(Router);

  onSubmit() {
    if (!this.credentials.username || !this.credentials.password) return;
    
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.login(this.credentials).subscribe({
      next: (res) => {
        const role = res.user.role;
        // Smart Redirect (ระบบคัดแยกคน)
        if (role === 'Customer') {
          this.router.navigate(['/']);
        } else {
          this.router.navigate(['/admin']);
        }
      },
      error: (err) => {
        this.errorMessage = err;
        this.isLoading = false;
      }
    });
  }
}
