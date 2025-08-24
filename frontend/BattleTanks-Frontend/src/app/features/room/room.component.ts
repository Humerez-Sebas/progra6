import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';

import { roomActions } from './store/room.actions';
import {
  selectHubConnected,
  selectJoined,
  selectRoomError,
  selectRoomId,
  selectPlayers,
  selectGameResult,
} from './store/room.selectors';
import { selectUser } from './../auth/store/auth.selectors';
import { RoomCanvasComponent } from './room-canvas/room-canvas.component';
import { ChatPanelComponent } from './chat-panel/chat-panel.component';
import { RoomService } from '../../core/services/room.service';
import { UserDto } from '../../core/models/auth.models';

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
  user         = toSignal<UserDto | null>(this.store.select(selectUser), { initialValue: null });
  roomId       = toSignal(this.store.select(selectRoomId),       { initialValue: null });
  players      = toSignal(this.store.select(selectPlayers),      { initialValue: [] as any[] });
  gameResult   = toSignal(this.store.select(selectGameResult),   { initialValue: null });

  private roomCode = signal<string | null>(null);
  gameOver = signal(false);
  victory = signal(false);
  stats = signal<{ score: number } | null>(null);

  /** Identifica MI jugador por playerId === user.id */
  myPlayer = computed(() => {
    const me = this.user();
    const plist = this.players();
    if (!me || !plist?.length) return null;
    const meId = String(me.id);
    return plist.find(p => p?.playerId != null && String(p.playerId) === meId) ?? null;
  });

  /**
   * Unirse SOLO cuando:
   *  - hay conexión al hub
   *  - NO me he unido
   *  - tengo roomCode
   *  - y YA tengo user (para usar su username real)
   */
  private joinWhenReady = effect(() => {
    const connected = this.hubConnected();
    const alreadyJoined = this.joined();
    const code = this.roomCode();
    const me = this.user();

    if (!connected || alreadyJoined || !code || !me) return; // 👈 evita unirte como "Player"
    const username = me.username;

    this.store.dispatch(roomActions.joinRoom({ code, username }));
  });

  /** Mostrar modal de game over cuando mi jugador se queda sin vidas */
  private watchElimination = effect(() => {
    const mine = this.myPlayer();
    if (!mine) return;
    const lives = Number(mine.lives ?? 0);
    if (lives <= 0 && !this.gameOver()) {
      this.gameOver.set(true);
      this.stats.set({ score: Number(mine.score ?? 0) });
    }
  });

  /** Mostrar modal de victoria cuando el juego termina y yo soy el ganador */
  private watchVictory = effect(() => {
    const result = this.gameResult();
    const me = this.user();
    if (!result || !me || this.gameOver()) return;
    if (result.winnerPlayerId === String(me.id)) {
      const myScore = result.scores.find(s => String(s.playerId) === String(me.id))?.score ?? 0;
      this.stats.set({ score: myScore });
      this.victory.set(true);
      this.gameOver.set(true);
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
