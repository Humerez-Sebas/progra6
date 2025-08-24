import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-join-room-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './join-room-form.component.html',
  styleUrls: ['./join-room-form.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JoinRoomFormComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);

  form = this.fb.nonNullable.group({
    roomCode: ['', [Validators.required, Validators.minLength(4)]],
  });

  join() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { roomCode } = this.form.getRawValue();
    // Navegación a la sala (ruta se implementará después)
    this.router.navigateByUrl(`/rooms/${encodeURIComponent(roomCode)}`);
  }

  get f() { return this.form.controls; }
}
