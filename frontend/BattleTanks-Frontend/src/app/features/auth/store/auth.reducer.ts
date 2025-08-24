import { createReducer, on } from '@ngrx/store';
import { authActions } from './auth.actions';
import { UserDto } from '../../../core/models/auth.models';

export interface AuthState {
  user: UserDto | null;
  loading: boolean;
  error: string | null;
}

export const initialAuthState: AuthState = {
  user: null,
  loading: false,
  error: null,
};

export const authReducer = createReducer(
  initialAuthState,

  // Register
  on(authActions.register, (s) => ({ ...s, loading: true, error: null })),
  on(authActions.registerSuccess, (s, { user }) => ({ ...s, loading: false, user })),
  on(authActions.registerFailure, (s, { error }) => ({ ...s, loading: false, error })),

  // Login (lo implementamos después, ya queda el caso listo)
  on(authActions.login, (s) => ({ ...s, loading: true, error: null })),
  on(authActions.loginSuccess, (s, { user }) => ({ ...s, loading: false, user })),
  on(authActions.loginFailure, (s, { error }) => ({ ...s, loading: false, error })),

  // Load profile
  on(authActions.loadProfile, (s) => ({ ...s, loading: true, error: null })),
  on(authActions.loadProfileSuccess, (s, { user }) => ({ ...s, loading: false, user })),
  on(authActions.loadProfileFailure, (s, { error }) => ({ ...s, loading: false, error })),

  // Logout
  on(authActions.logoutSuccess, () => ({ user: null, loading: false, error: null })),

  on(authActions.clearError, (s) => ({ ...s, error: null })),
);
