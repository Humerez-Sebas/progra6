import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';

import { authActions } from '../store/auth.actions';
import { selectAuthError, selectAuthLoading, selectUser } from '../store/auth.selectors';
import { LoginDto } from '../../../core/models/auth.models';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private store = inject(Store);

  loading = toSignal(this.store.select(selectAuthLoading), { initialValue: false });
  error   = toSignal(this.store.select(selectAuthError),   { initialValue: null });
  user    = toSignal(this.store.select(selectUser),        { initialValue: null });

  // Toggle de visibilidad
  showPassword = signal(false);

  // Modal de éxito
  private attemptedLogin = signal(false);
  showSuccessModal = signal(false);

  form = this.fb.nonNullable.group({
    usernameOrEmail: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  modalEffect = effect(() => {
    const tried = this.attemptedLogin();
    const isLoading = this.loading();
    const err = this.error();
    const u = this.user();

    if (tried && !isLoading && !err && u) {
      this.showSuccessModal.set(true);
      this.attemptedLogin.set(false);
      setTimeout(() => this.showSuccessModal.set(false), 1500);
    }
  }, { allowSignalWrites: true });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { usernameOrEmail, password } = this.form.getRawValue();
    const dto: LoginDto = { usernameOrEmail, password };
    this.attemptedLogin.set(true);
    this.store.dispatch(authActions.clearError());
    this.store.dispatch(authActions.login({ dto }));
  }

  toggleShowPassword() {
    this.showPassword.update(v => !v);
  }

  get f() { return this.form.controls; }
}
