import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const user = authService.currentUser();
  if (user) {
    if (user.role === 'Customer') {
      return router.createUrlTree(['/']); // Redirect to storefront if customer
    }
    return true; // Allow Admin, Sales, Purchasing
  }
  
  return router.createUrlTree(['/login']);
};
