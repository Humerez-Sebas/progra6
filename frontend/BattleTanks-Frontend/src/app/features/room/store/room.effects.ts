import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { roomActions } from './room.actions';
import { SignalRService } from '../../../core/services/signalr.service';
import { catchError, filter, from, map, merge, mergeMap, of, switchMap, takeUntil, tap, throttleTime, withLatestFrom } from 'rxjs';
import { Store } from '@ngrx/store';
import { selectRoomCode } from './room.selectors';
import { selectUser } from '../../auth/store/auth.selectors';
import { RoomService } from '../../../core/services/room.service';
import { RoomStateDto } from '../../../core/models/room.models';

@Injectable()
export class RoomEffects {
  private actions$ = inject(Actions);
  private hub = inject(SignalRService);
  private store = inject(Store);
  private roomsHttp = inject(RoomService);

  hubConnect$ = createEffect(() =>
    this.actions$.pipe(
      ofType(roomActions.hubConnect),
      switchMap(() =>
        from(this.hub.connect()).pipe(
          map(() => roomActions.hubConnected()),
          catchError((err) => of(roomActions.hubError({ error: err?.message ?? 'No se pudo conectar al hub' })))
        )
      )
    )
  );

  events$ = createEffect(() =>
    this.actions$.pipe(
      ofType(roomActions.hubConnected),
      switchMap(() => {
        const stop$ = this.actions$.pipe(ofType(roomActions.hubDisconnected, roomActions.left));
        return merge(
          this.hub.playerJoined$.pipe(map((p) => roomActions.playerJoined(p))),
          this.hub.playerLeft$.pipe(map((userId) => roomActions.playerLeft({ userId }))),
          this.hub.chatMessage$.pipe(map((msg) => roomActions.messageReceived({ msg }))),
          this.hub.playerMoved$.pipe(map((player: any) => roomActions.playerMoved({ player }))),
          this.hub.bulletSpawned$.pipe(map((bullet) => roomActions.bulletSpawned({ bullet }))),
          this.hub.bulletDespawned$.pipe(map(({ bulletId, reason }) => roomActions.bulletDespawned({ bulletId, reason }))),

          this.hub.roomSnapshot$.pipe(map((snapshot) => roomActions.roomSnapshotReceived({ snapshot }))),
          this.hub.mapSnapshot$.pipe(map((snapshot) => roomActions.mapSnapshotReceived({ snapshot }))),
          this.hub.mapTileUpdated$.pipe(map((tile) => roomActions.mapTileUpdated({ tile }))),
          this.hub.playerLifeLost$.pipe(map((data) => roomActions.playerLifeLost({ data }))),
          this.hub.playerRespawned$.pipe(map((data) => roomActions.playerRespawned({ data }))),
          this.hub.playerScored$.pipe(map((data) => roomActions.playerScored({ data }))),
          this.hub.gameEnded$.pipe(map((data) => roomActions.gameEnded({ data }))),
          this.hub.powerUpsSnapshot$.pipe(map((powerUps) => roomActions.powerUpsSnapshotReceived({ powerUps }))),
          this.hub.powerUpSpawned$.pipe(map((powerUp) => roomActions.powerUpSpawned({ powerUp }))),
          this.hub.powerUpCollected$.pipe(map((payload) => roomActions.powerUpCollected(payload))),
        ).pipe(takeUntil(stop$));
      })
    )
  );

  joinRoom$ = createEffect(() =>
    this.actions$.pipe(
      ofType(roomActions.joinRoom),
      switchMap(({ code, username }) =>
        from(this.hub.joinRoom(code, username)).pipe(
          map(() => roomActions.joined()),
          catchError((err) => of(roomActions.hubError({ error: err?.message ?? 'join_failed' })))
        )
      )
    )
  );

  rejoin$ = createEffect(() => this.hub.reconnected$.pipe(map(() => roomActions.hubReconnected())));

  rejoinOnReconnected$ = createEffect(() =>
    this.actions$.pipe(
      ofType(roomActions.hubReconnected),
      withLatestFrom(this.store.select(selectRoomCode), this.store.select(selectUser)),
      filter(([_, code, user]) => !!code && !!user?.username),
      switchMap(([_, code, user]) =>
        from(this.hub.joinRoom(code as string, user!.username)).pipe(
          map(() => roomActions.joined()),
          catchError((err) => of(roomActions.hubError({ error: err?.message ?? 'rejoin_failed' })))
        )
      )
    )
  );

  sendMessage$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(roomActions.sendMessage),
        mergeMap(({ content }) => from(this.hub.sendChat(content)).pipe(catchError(() => of(void 0))))
      ),
    { dispatch: false }
  );

  updatePosition$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(roomActions.updatePosition),
        throttleTime(60, undefined, { leading: true, trailing: true }),
        mergeMap(({ dto }) => from(this.hub.updatePosition(dto)).pipe(catchError(() => of(void 0))))
      ),
    { dispatch: false }
  );

  spawnBullet$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(roomActions.spawnBullet),
        mergeMap(({ x, y, dir, speed }) => from(this.hub.spawnBullet(x, y, dir, speed)).pipe(catchError(() => of(null))))
      ),
    { dispatch: false }
  );

  leave$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(roomActions.leaveRoom),
        mergeMap(() => from(this.hub.disconnect()).pipe(catchError(() => of(void 0))))
      ),
    { dispatch: false }
  );

  rosterAfterJoin$ = createEffect(() =>
    this.actions$.pipe(
      ofType(roomActions.joined),
      withLatestFrom(this.store.select(selectRoomCode)),
      filter(([_, code]) => !!code),
      switchMap(([_, code]) =>
        this.roomsHttp.getRooms().pipe(
          map((res: any) => {
            const list: RoomStateDto[] = Array.isArray(res?.items) ? res.items : Array.isArray(res) ? res : [];
            const match = list.find(r => r.roomCode === code);
            return match?.roomId ?? null;
          }),
          switchMap((roomId) => roomId ? this.roomsHttp.getRoom(roomId).pipe(map(r => ({ room: r, roomId }))) : of({ room: null, roomId: null })),
          map(({ room, roomId }) => roomActions.rosterLoaded({ players: room?.players ?? [], roomId })),
          catchError(() => of(roomActions.rosterLoaded({ players: [], roomId: null })))
        )
      )
    )
  );

  logBulletCollisions$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(roomActions.bulletDespawned),
        tap(({ bulletId, reason }) => console.log('Bullet', bulletId, 'despawned because', reason))
      ),
    { dispatch: false }
  );
}
