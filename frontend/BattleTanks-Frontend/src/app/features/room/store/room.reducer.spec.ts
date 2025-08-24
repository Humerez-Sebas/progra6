import { roomReducer, RoomState, roomPlayersAdapter, roomBulletsAdapter } from './room.reducer';
import { roomActions } from './room.actions';
import { ChatMessageDto } from '../../../core/models/game.models';

describe('Room Reducer', () => {
  let initial: RoomState;

  beforeEach(() => {
    initial = {
      roomId: null,
      roomCode: null,
      joined: false,
      hubConnected: false,
      error: null,
      players: roomPlayersAdapter.getInitialState(),
      bullets: roomBulletsAdapter.getInitialState(),
      chat: [],
      lastUsername: null,
      map: null,
    };
  });

  it('hubConnected debe poner hubConnected=true', () => {
    const state = roomReducer(initial, roomActions.hubConnected());
    expect(state.hubConnected).toBeTrue();
  });

  it('joinRoom debe setear roomCode y lastUsername', () => {
    const state = roomReducer(initial, roomActions.joinRoom({ code: 'ABCD', username: 'juan' }));
    expect(state.roomCode).toBe('ABCD');
    expect(state.lastUsername).toBe('juan');
  });

  it('playerJoined debe upsertear jugador con username', () => {
    const s1 = roomReducer(initial, roomActions.playerJoined({ userId: 'u1', username: 'sebas' }));
    const player = s1.players.entities['u1']!;
    expect(player.username).toBe('sebas');
    expect(player.playerId).toBe('u1');
  });

  it('playerMoved (simple) debe fusionar y conservar username existente', () => {
    const s1 = roomReducer(initial, roomActions.playerJoined({ userId: 'u1', username: 'sebas' }));
    const s2 = roomReducer(s1, roomActions.playerMoved({ player: { playerId: 'u1', x: 10, y: 20, rotation: 0 } }));
    const p = s2.players.entities['u1']!;
    expect(p.username).toBe('sebas');
    expect(p.x).toBe(10);
    expect(p.y).toBe(20);
  });
});
