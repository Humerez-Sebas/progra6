import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { RegisterDto, LoginDto, UserDto } from '../../../core/models/auth.models';

export const authActions = createActionGroup({
  source: 'Auth',
  events: {
    'Register': props<{ dto: RegisterDto }>(),
    'Register Success': props<{ user: UserDto }>(),
    'Register Failure': props<{ error: string }>(),

    'Login': props<{ dto: LoginDto }>(),
    'Login Success': props<{ user: UserDto }>(),
    'Login Failure': props<{ error: string }>(),

    'Load Profile': emptyProps(),
    'Load Profile Success': props<{ user: UserDto }>(),
    'Load Profile Failure': props<{ error: string }>(),

    'Logout': emptyProps(),
    'Logout Success': emptyProps(),

    'Clear Error': emptyProps(),
  },
});
