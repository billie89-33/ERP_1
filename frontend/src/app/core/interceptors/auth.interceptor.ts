import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // แนบ withCredentials: true ไปกับทุก request เพื่อให้ browser ส่ง HttpOnly Cookie (JWT) ไปด้วย
  const token = localStorage.getItem('jamine_token');
  const headers: any = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const clonedRequest = req.clone({
    withCredentials: true,
    setHeaders: headers
  });
  return next(clonedRequest);
};
