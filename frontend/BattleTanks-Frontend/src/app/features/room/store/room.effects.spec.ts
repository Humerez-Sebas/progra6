import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable, Subject, of, firstValueFrom } from 'rxjs';
import { provideMockStore, MockStore } from '@ngrx/store/testing';

import { RoomEffects } from './room.effects';
import { roomActions } from './room.actions';
import { SignalRService } from '../../../core/services/signalr.service';
import { RoomService } from '../../../core/services/room.service';
import { selectRoomCode } from './room.selectors';
import { selectUser } from '../../auth/store/auth.selectors';
import { UserDto } from '../../../core/models/auth.models';

class SignalRServiceMock {
  playerJoined$ = new Subject<{ userId: string; username: string }>();
  playerLeft$ = new Subject<string>();
  chatMessage$ = new Subject<any>();
  playerMoved$ = new Subject<any>();
  bulletSpawned$ = new Subject<any>();
  bulletDespawned$ = new Subject<{ bulletId: string; reason: string }>();
  reconnected$ = new Subject<void>();
  disconnected$ = new Subject<void>();

  connect = jasmine.createSpy('connect').and.returnValue(Promise.resolve());
  disconnect = jasmine.createSpy('disconnect').and.returnValue(Promise.resolve());
  joinRoom = jasmine.createSpy('joinRoom').and.returnValue(Promise.resolve());
  sendChat = jasmine.createSpy('sendChat').and.returnValue(Promise.resolve());
  updatePosition = jasmine.createSpy('updatePosition').and.returnValue(Promise.resolve());
  spawnBullet = jasmine.createSpy('spawnBullet').and.returnValue(Promise.resolve());
}

class RoomServiceMock {
  getRooms = jasmine.createSpy('getRooms').and.returnValue(of({ items: [] }));
  getRoom  = jasmine.createSpy('getRoom').and.returnValue(of(null));
}

describe('RoomEffects (simple)', () => {
  let actions$: Observable<any>;
  let effects: RoomEffects;
  let hub: SignalRServiceMock;
  let store: MockStore;

  const mockUser: UserDto = {
    id: 'u1',
    username: 'juan',
    email: 'juan@example.com',
    gamesPlayed: 0,
    gamesWon: 0,
    totalScore: 0,
    winRate: 0,
    createdAt: new Date().toISOString(),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        RoomEffects,
        provideMockActions(() => actions$),
        provideMockStore(),
        { provide: SignalRService, useClass: SignalRServiceMock },
        { provide: RoomService,   useClass: RoomServiceMock },
      ],
    });

    effects = TestBed.inject(RoomEffects);
    hub = TestBed.inject(SignalRService) as any;
    store = TestBed.inject(MockStore);

    store.overrideSelector(selectRoomCode, 'ABCD');
    store.overrideSelector(selectUser, mockUser);
  });

  it('hubConnect debe llamar hub.connect y emitir hubConnected', async () => {
    actions$ = of(roomActions.hubConnect());
    const out = await firstValueFrom(effects.hubConnect$);
    expect(hub.connect).toHaveBeenCalled();
    expect(out.type).toBe(roomActions.hubConnected.type);
  });


  it('joinRoom debe invocar hub.joinRoom y emitir joined', async () => {
    actions$ = of(roomActions.joinRoom({ code: 'ABCD', username: 'juan' }));
    const out = await firstValueFrom(effects.joinRoom$);
    expect(hub.joinRoom).toHaveBeenCalledWith('ABCD', 'juan');
    expect(out.type).toBe(roomActions.joined.type);
  });
});
