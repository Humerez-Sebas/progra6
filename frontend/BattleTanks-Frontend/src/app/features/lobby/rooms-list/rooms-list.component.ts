import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { selectRooms, selectRoomsLoading } from '../store/rooms.selectors';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-rooms-list',
  imports: [CommonModule],
  templateUrl: './rooms-list.component.html',
  styleUrls: ['./rooms-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomsListComponent {
  private store = inject(Store);
  private router = inject(Router);

  rooms   = toSignal(this.store.select(selectRooms), { initialValue: [] });
  loading = toSignal(this.store.select(selectRoomsLoading), { initialValue: false });

  enter(code: string) {
    this.router.navigateByUrl(`/rooms/${encodeURIComponent(code)}`);
  }
}
