import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { roomsActions } from '../store/rooms.actions';
import { selectRoomsCreating } from '../store/rooms.selectors';

@Component({
  standalone: true,
  selector: 'app-create-room-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-room-form.component.html',
  styleUrls: ['./create-room-form.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateRoomFormComponent {
  private fb = inject(FormBuilder);
  private store = inject(Store);

  creating = toSignal(this.store.select(selectRoomsCreating), { initialValue: false });

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    maxPlayers: [8, [Validators.required, Validators.min(2), Validators.max(32)]],
    isPublic: [true, [Validators.required]],
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.store.dispatch(roomsActions.createRoom({ dto: this.form.getRawValue() }));
  }

  get f() { return this.form.controls; }
}
