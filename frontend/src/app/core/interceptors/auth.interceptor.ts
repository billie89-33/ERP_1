import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // แนบ withCredentials: true ไปกับทุก request เพื่อให้ browser ส่ง HttpOnly Cookie (JWT) ไปด้วย
  const clonedRequest = req.clone({
    withCredentials: true
  });
  return next(clonedRequest);
};
