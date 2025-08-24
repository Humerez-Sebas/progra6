import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.verify().pipe(
    map((res) => {
      if (res.authenticated) {
        router.navigateByUrl('/lobby');
        return false;
      }
      return true;
    }),
    catchError(() => of(true))
  );
};
