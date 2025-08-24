import { HttpEvent, HttpHandlerFn, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { authActions } from '../../features/auth/store/auth.actions';

export function credentialsInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
  const cloned = req.clone({ withCredentials: true });
  const router = inject(Router);
  const store = inject(Store);

  return next(cloned).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        store.dispatch(authActions.logoutSuccess());
        router.navigateByUrl('/login');
      }
      throw err;
    })
  );
}
