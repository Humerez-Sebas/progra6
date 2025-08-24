import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { roomsActions } from './store/rooms.actions';
import { selectRooms, selectRoomsError, selectRoomsLoading } from './store/rooms.selectors';
import { CreateRoomFormComponent } from './create-room-form/create-room-form.component';
import { JoinRoomFormComponent } from './join-room-form/join-room-form.component';
import { RoomsListComponent } from './rooms-list/rooms-list.component';

@Component({
  standalone: true,
  selector: 'app-lobby',
  imports: [CommonModule, CreateRoomFormComponent, JoinRoomFormComponent, RoomsListComponent],
  templateUrl: './lobby.component.html',
  styleUrls: ['./lobby.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LobbyComponent implements OnInit {
  private store = inject(Store);

  rooms    = toSignal(this.store.select(selectRooms), { initialValue: [] });
  loading  = toSignal(this.store.select(selectRoomsLoading), { initialValue: false });
  error    = toSignal(this.store.select(selectRoomsError), { initialValue: null });

  ngOnInit(): void {
    this.store.dispatch(roomsActions.loadRooms());
  }
}
