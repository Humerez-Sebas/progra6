import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { authActions } from './auth.actions';
import { AuthService } from '../../../core/services/auth.service';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { Router } from '@angular/router';

const LOCAL_KEY = 'auth_user_snapshot';

@Injectable()
export class AuthEffects {
  private actions$ = inject(Actions);
  private auth = inject(AuthService);
  private router = inject(Router);

  // POST /auth/register
  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(authActions.register),
      switchMap(({ dto }) =>
        this.auth.register(dto).pipe(
          map((user) => authActions.registerSuccess({ user })),
          catchError((err) => of(authActions.registerFailure({ error: err?.error?.message ?? 'No se pudo registrar' })))
        )
      )
    )
  );

  // Navega y persiste snapshot
  registerSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(authActions.registerSuccess),
        tap(({ user }) => {
          localStorage.setItem(LOCAL_KEY, JSON.stringify(user));
          this.router.navigateByUrl('/lobby');
        })
      ),
    { dispatch: false }
  );

  // GET /auth/profile al boot (APP_INITIALIZER disparará esta acción)
  loadProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(authActions.loadProfile),
      switchMap(() =>
        this.auth.profile().pipe(
          map((user) => authActions.loadProfileSuccess({ user })),
          catchError((err) => {
            localStorage.removeItem(LOCAL_KEY);
            return of(authActions.loadProfileFailure({ error: err?.error?.message ?? 'No autenticado' }));
          })
        )
      )
    )
  );

  login$ = createEffect(() =>
  this.actions$.pipe(
    ofType(authActions.login),
    switchMap(({ dto }) =>
      this.auth.login(dto).pipe(
        map((user) => authActions.loginSuccess({ user })),
        catchError((err) =>
          of(authActions.loginFailure({ error: err?.error?.message ?? 'No se pudo iniciar sesión' }))
        )
      )
    )
  )
);

  loginSuccessNavigate$ = createEffect(
  () =>
    this.actions$.pipe(
      ofType(authActions.loginSuccess),
      tap(() => {
        // persistimos ya en persistOnSuccess$; acá solo navegamos
        this.router.navigateByUrl('/lobby');
      })
    ),
  { dispatch: false }
);

  // Persistir cuando cargamos perfil o hagamos login en el futuro
  persistOnSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(authActions.loadProfileSuccess, authActions.loginSuccess),
        tap(({ user }) => localStorage.setItem(LOCAL_KEY, JSON.stringify(user)))
      ),
    { dispatch: false }
  );

  // Cleanup + redirect
  logoutSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(authActions.logoutSuccess),
        tap(() => {
          localStorage.removeItem(LOCAL_KEY);
          this.router.navigateByUrl('/login');
        })
      ),
    { dispatch: false }
  );
}