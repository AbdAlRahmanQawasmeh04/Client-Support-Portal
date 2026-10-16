import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const clientGuard: CanActivateFn = () => {
  const user = inject(AuthService).user();

  if (user?.accessToken && user.role === 'Client') {
    return true;
  }

  return inject(Router).createUrlTree(['/login']);
};