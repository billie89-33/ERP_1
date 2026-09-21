import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService } from './core/services/auth.service';
import { Observable } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export function initializeApp(authService: AuthService): () => Observable<any> {
  return () => authService.checkSession();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }), 
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [AuthService],
      multi: true
    }
  ]
};
