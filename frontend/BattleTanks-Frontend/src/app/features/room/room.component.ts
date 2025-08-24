import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';

import { roomActions } from './store/room.actions';
import { selectHubConnected, selectJoined, selectRoomError, selectRoomId, selectPlayers } from './store/room.selectors';
import { selectUser } from './../auth/store/auth.selectors';
import { RoomCanvasComponent } from './room-canvas/room-canvas.component';
import { ChatPanelComponent } from './chat-panel/chat-panel.component';
import { RoomService } from '../../core/services/room.service';

@Component({
  standalone: true,
  selector: 'app-room',
  imports: [CommonModule, RouterLink, RoomCanvasComponent, ChatPanelComponent],
  templateUrl: './room.component.html',
  styleUrls: ['./room.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private store = inject(Store);
  private roomsHttp = inject(RoomService);

  hubConnected = toSignal(this.store.select(selectHubConnected), { initialValue: false });
  joined       = toSignal(this.store.select(selectJoined),       { initialValue: false });
  error        = toSignal(this.store.select(selectRoomError),    { initialValue: null });
  user         = toSignal(this.store.select(selectUser),         { initialValue: null });
  roomId       = toSignal(this.store.select(selectRoomId),       { initialValue: null });
  players      = toSignal(this.store.select(selectPlayers),      { initialValue: [] });

  private roomCode = signal<string | null>(null);

  // 👇 Efecto creado como *campo de clase* (válido en contexto de inyección)
  private joinWhenReady = effect(() => {
    const connected = this.hubConnected();
    const alreadyJoined = this.joined();
    const code = this.roomCode();
    const username = this.user()?.username ?? 'Player';

    if (connected && !alreadyJoined && code) {
      this.store.dispatch(roomActions.joinRoom({ code, username }));
    }
  });

  ngOnInit(): void {
    const code = this.route.snapshot.paramMap.get('code');
    this.roomCode.set(code);
    this.store.dispatch(roomActions.hubConnect());
  }

  ngOnDestroy(): void {
    this.store.dispatch(roomActions.leaveRoom());
  }

  startGame() {
    const id = this.roomId();
    if (id) {
      this.roomsHttp.startGame(id).subscribe();
    }
  }
}
