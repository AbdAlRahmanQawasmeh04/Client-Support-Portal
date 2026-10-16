import { inject } from '@angular/core';

import {
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';

import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { API_URL, AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.user()?.accessToken;

  const isProtectedApi =
    request.url.startsWith(`${API_URL}/`) &&
    !request.url.startsWith(`${API_URL}/auth/`);

  if (!isProtectedApi || !token) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status === 401 &&
        auth.user()?.accessToken === token
      ) {
        auth.logout();
        void router.navigateByUrl('/login');
      }

      return throwError(() => error);
    })
  );
};