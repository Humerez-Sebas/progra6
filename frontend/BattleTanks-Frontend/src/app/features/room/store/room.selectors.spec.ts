import { selectBullets, selectChat, selectHubConnected, selectJoined, selectPlayers, selectRoomCode } from './room.selectors';
import { RoomState, roomPlayersAdapter, roomBulletsAdapter } from './room.reducer';
import { MemoizedSelector } from '@ngrx/store';

describe('Room Selectors', () => {
  let state: { room: any };

  beforeEach(() => {
    const players = roomPlayersAdapter.setAll([
      { playerId: 'p1', username: 'juan', x: 1, y: 2, rotation: 0, health: 100, isAlive: true },
      { playerId: 'p2', username: 'sebas', x: 4, y: 5, rotation: 0, health: 100, isAlive: true },
    ], roomPlayersAdapter.getInitialState());

    const bullets = roomBulletsAdapter.setAll([
      { bulletId: 'b1', roomId: 'r1', shooterId: 'p1', x: 10, y: 20, directionRadians: 0, speed: 100, spawnTimestamp: 1, isActive: true },
    ], roomBulletsAdapter.getInitialState());

    const room: RoomState = {
      roomId: 'r1',
      roomCode: 'ABCD',
      joined: true,
      hubConnected: true,
      error: null,
      players,
      bullets,
      chat: [],
      lastUsername: 'juan',
      map: null,
    };
    state = { room };
  });

  it('selectRoomCode', () => {
    expect(selectRoomCode.projector(state.room)).toBe('ABCD');
  });
  it('selectHubConnected', () => {
    expect(selectHubConnected.projector(state.room)).toBeTrue();
  });
  it('selectJoined', () => {
    expect(selectJoined.projector(state.room)).toBeTrue();
  });
  it('selectPlayers', () => {
    const res = selectPlayers.projector(state.room);
    expect(res.length).toBe(2);
    expect(res[0].username).toBe('juan');
  });
  it('selectBullets', () => {
    const res = selectBullets.projector(state.room);
    expect(res.length).toBe(1);
  });
  it('selectChat', () => {
    expect(selectChat.projector(state.room)).toEqual([]);
  });
});
