import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, map, of, tap } from 'rxjs';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.verify().pipe(
    map(res => !!res.authenticated),
    tap(ok => { if (!ok) router.navigateByUrl('/login'); }),
    catchError(() => {
      router.navigateByUrl('/login');
      return of(false);
    })
  );
};
