import { ApplicationConfig, APP_INITIALIZER, inject, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from './core/interceptors/credentials.interceptor';

import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideRouterStore } from '@ngrx/router-store';

import { authReducer } from './features/auth/store/auth.reducer';
import { AuthEffects } from './features/auth/store/auth.effects';
import { Store } from '@ngrx/store';
import { authActions } from './features/auth/store/auth.actions';

import { roomsReducer } from './features/lobby/store/rooms.reducer';
import { RoomsEffects } from './features/lobby/store/rooms.effects';
import { roomReducer } from './features/room/store/room.reducer';
import { RoomEffects } from './features/room/store/room.effects';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([credentialsInterceptor])),

    provideStore({
      auth: authReducer,
      rooms: roomsReducer,
      room: roomReducer,
    }),
    provideEffects([AuthEffects, RoomsEffects, RoomEffects]),
    provideRouterStore(),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),

    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => {
        return () => {
          const store = inject(Store);
          store.dispatch(authActions.loadProfile());
          return Promise.resolve();
        };
      },
    },
  ],
};
