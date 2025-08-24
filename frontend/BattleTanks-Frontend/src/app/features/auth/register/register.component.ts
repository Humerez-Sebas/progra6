import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';

import { authActions } from '../store/auth.actions';
import { selectAuthError, selectAuthLoading } from '../store/auth.selectors';
import { RegisterDto } from '../../../core/models/auth.models';
import { RouterLink } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private store = inject(Store);

  loading = toSignal(this.store.select(selectAuthLoading), { initialValue: false });
  error   = toSignal(this.store.select(selectAuthError),   { initialValue: null });

  // Toggles de visibilidad
  showPassword = signal(false);
  showConfirm  = signal(false);

  form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(6)]],
  });

  passwordsMismatch = computed(() => {
    const { password, confirmPassword } = this.form.getRawValue();
    return !!password && !!confirmPassword && password !== confirmPassword;
  });

  toggleShowPassword() {
    this.showPassword.update(v => !v);
  }
  toggleShowConfirm() {
    this.showConfirm.update(v => !v);
  }

  submit() {
    if (this.form.invalid || this.passwordsMismatch()) {
      this.form.markAllAsTouched();
      return;
    }
    const { username, email, password, confirmPassword } = this.form.getRawValue();
    const dto: RegisterDto = { username, email, password, confirmPassword };
    this.store.dispatch(authActions.register({ dto }));
  }

  get f() { return this.form.controls; }
}
