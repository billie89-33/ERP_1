import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const user = authService.currentUser();
  
  // If no user, authGuard will handle it anyway, but just in case:
  if (!user) {
    return router.createUrlTree(['/login']);
  }

  // Get allowed roles from route data
  const allowedRoles: string[] = route.data?.['roles'] || [];

  if (allowedRoles.length === 0 || allowedRoles.includes(user.role)) {
    return true; // Has permission
  }

  // No permission -> redirect to dashboard or show unauthorized
  alert('Access Denied: You do not have permission to view this page.');
  return router.createUrlTree(['/admin']);
};
