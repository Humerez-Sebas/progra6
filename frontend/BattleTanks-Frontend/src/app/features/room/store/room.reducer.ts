import { createReducer, on } from '@ngrx/store';
import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { roomActions } from './room.actions';
import {
  BulletStateDto,
  ChatMessageDto,
  PlayerStateDto,
  MapTileDto,
  MapTileType,
  PowerUpDto,
} from '../../../core/models/game.models';

export interface PlayerEntity extends PlayerStateDto {}
export interface BulletEntity extends BulletStateDto {}
export interface PowerUpEntity extends PowerUpDto {}

export type RoomStatus = 'Waiting' | 'InProgress' | 'Finished';

export interface RoomState {
  roomId: string | null;
  roomCode: string | null;
  status: RoomStatus;
  joined: boolean;
  hubConnected: boolean;
  error: string | null;
  players: EntityState<PlayerEntity>;
  bullets: EntityState<BulletEntity>;
  powerUps: EntityState<PowerUpEntity>;
  chat: ChatMessageDto[];
  lastUsername: string | null;
  gameResult: { winnerPlayerId: string; scores: { playerId: string; score: number }[] } | null;

  map: {
    width: number;
    height: number;
    tileSize: number;
    tilesByKey: Record<string, { type: MapTileType; hp?: number }>;
  } | null;
}

const playersAdapter = createEntityAdapter<PlayerEntity>({ selectId: (p) => p.playerId });
const bulletsAdapter = createEntityAdapter<BulletEntity>({ selectId: (b) => b.bulletId });
const powerUpsAdapter = createEntityAdapter<PowerUpEntity>({ selectId: (p) => p.id });

const initialState: RoomState = {
  roomId: null,
  roomCode: null,
  status: 'Waiting' as RoomStatus,
  joined: false,
  hubConnected: false,
  error: null,
  players: playersAdapter.getInitialState(),
  bullets: bulletsAdapter.getInitialState(),
  powerUps: powerUpsAdapter.getInitialState(),
  chat: [],
  lastUsername: null,
  gameResult: null,
  map: null,
};

export const roomReducer = createReducer(
  initialState,

  // Hub
  on(roomActions.hubConnect, (s) => ({ ...s, error: null })),
  on(roomActions.hubConnected, (s) => ({ ...s, hubConnected: true })),
  on(roomActions.hubDisconnected, (s) => ({ ...s, hubConnected: false, joined: false })),
  on(roomActions.hubError, (s, { error }) => ({ ...s, error })),

  // Sala
  on(roomActions.joinRoom, (s, { code, username }) => ({ ...s, roomCode: code, lastUsername: username, error: null })),
  on(roomActions.joined, (s) => ({ ...s, joined: true })),
  on(roomActions.leaveRoom, (s) => ({ ...s, joined: false })),
  on(roomActions.left, (s) => ({
    ...s,
    joined: false,
    roomCode: null,
    status: 'Waiting' as RoomStatus,
    players: playersAdapter.removeAll(s.players),
    bullets: bulletsAdapter.removeAll(s.bullets),
    powerUps: powerUpsAdapter.removeAll(s.powerUps),
    chat: [],
    gameResult: null,
    map: null,
  })),

  // Snapshots
  on(roomActions.roomSnapshotReceived, (s, { snapshot }) => ({
    ...s,
    roomId: snapshot.roomId ?? s.roomId,
    roomCode: snapshot.roomCode ?? s.roomCode,
    status: s.status,
    players: playersAdapter.setAll(
      (snapshot.players ?? []).map(p => ({ ...p, lives: p.lives ?? 3, score: p.score ?? 0 })),
      s.players
    ),
  })),

  on(roomActions.mapSnapshotReceived, (s, { snapshot }) => {
    const tilesByKey: Record<string, { type: MapTileType; hp?: number }> = {};
    for (const t of snapshot.tiles ?? []) {
      tilesByKey[`${t.x},${t.y}`] = { type: t.type, hp: t.hp };
    }
    return {
      ...s,
      map: {
        width: snapshot.width,
        height: snapshot.height,
        tileSize: snapshot.tileSize,
        tilesByKey,
      },
    };
  }),

  on(roomActions.mapTileUpdated, (s, { tile }) => {
    if (!s.map) return s;
    const tilesByKey = { ...s.map.tilesByKey, [`${tile.x},${tile.y}`]: { type: tile.type, hp: tile.hp } };
    return { ...s, map: { ...s.map, tilesByKey } };
  }),

  // Jugadores
  on(roomActions.playerJoined, (s, { userId, username }) => {
    const existing = s.players.entities[userId];
    const upsert: PlayerStateDto = {
      playerId: userId,
      username: username ?? existing?.username ?? 'Unknown',
      x: existing?.x ?? 0,
      y: existing?.y ?? 0,
      rotation: existing?.rotation ?? 0,
      isAlive: existing?.isAlive ?? true,
      lives: existing?.lives ?? 3,
      score: existing?.score ?? 0,
    };
    return { ...s, players: playersAdapter.upsertOne(upsert, s.players) };
  }),

  on(roomActions.playerLeft, (s, { userId }) => ({
    ...s,
    players: playersAdapter.removeOne(userId, s.players),
  })),

  on(roomActions.playerMoved, (s, { player }) => {
    const id = (player as any).playerId;
    const existing = s.players.entities[id];
    const merged: PlayerStateDto = {
      playerId: id,
      username: (player as any).username ?? existing?.username ?? 'Unknown',
      x: (player as any).x,
      y: (player as any).y,
      rotation: (player as any).rotation,
      isAlive: (player as any).isAlive ?? existing?.isAlive ?? true,
      lives: (player as any).lives ?? existing?.lives ?? 3,
      score: (player as any).score ?? existing?.score ?? 0,
    };
    return { ...s, players: playersAdapter.upsertOne(merged, s.players) };
  }),

  // Vidas / Respawn
  on(roomActions.playerLifeLost, (s, { data }) => {
    const p = s.players.entities[data.targetPlayerId];
    if (!p) return s;
    const updated: PlayerStateDto = {
      ...p,
      isAlive: !data.eliminated,
      lives: data.livesAfter,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  on(roomActions.playerRespawned, (s, { data }) => {
    const p = s.players.entities[data.playerId];
    const updated: PlayerStateDto = {
      ...(p ?? { playerId: data.playerId, username: 'Player', rotation: 0, isAlive: true, x: 0, y: 0 }),
      x: data.x,
      y: data.y,
      isAlive: true,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  on(roomActions.playerScored, (s, { data }) => {
    const p = s.players.entities[data.playerId];
    const updated: PlayerStateDto = {
      ...(p ?? { playerId: data.playerId, username: 'Player', rotation: 0, isAlive: true, x: 0, y: 0 }),
      score: data.score,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  // PowerUps
  on(roomActions.powerUpsSnapshotReceived, (s, { powerUps }) => ({
    ...s,
    powerUps: powerUpsAdapter.setAll(powerUps, s.powerUps),
  })),
  on(roomActions.powerUpSpawned, (s, { powerUp }) => ({
    ...s,
    powerUps: powerUpsAdapter.upsertOne(powerUp, s.powerUps),
  })),
  on(roomActions.powerUpCollected, (s, { powerUpId }) => ({
    ...s,
    powerUps: powerUpsAdapter.removeOne(powerUpId, s.powerUps),
  })),

  // Balas
  on(roomActions.bulletSpawned, (s, { bullet }) => ({
    ...s,
    bullets: bulletsAdapter.upsertOne(bullet, s.bullets),
  })),
  on(roomActions.bulletDespawned, (s, { bulletId }) => ({
    ...s,
    bullets: bulletsAdapter.removeOne(bulletId, s.bullets),
  })),

  on(roomActions.messageReceived, (s, { msg }) => ({
    ...s,
    chat: [...s.chat, msg].slice(-200),
  })),

  on(roomActions.gameEnded, (s, { data }) => {
    let players = s.players;
    for (const sc of data.scores) {
      const ex = players.entities[sc.playerId];
      const up: PlayerStateDto = {
        ...(ex ?? { playerId: sc.playerId, username: 'Player', rotation: 0, isAlive: true, x: 0, y: 0 }),
        score: sc.score,
      };
      players = playersAdapter.upsertOne(up, players);
    }
    return { ...s, players, status: 'Finished' as RoomStatus, gameResult: { winnerPlayerId: data.winnerPlayerId, scores: data.scores } };
  }),

  on(roomActions.startGame, (s) => ({ ...s, error: null })),
  on(roomActions.startGameSuccess, (s) => ({ ...s, status: 'InProgress' as RoomStatus })),
  on(roomActions.startGameFailure, (s, { error }) => ({ ...s, error })),

  on(roomActions.endGame, (s) => ({ ...s, error: null })),
  on(roomActions.endGameSuccess, (s) => ({ ...s, status: 'Finished' as RoomStatus })),
  on(roomActions.endGameFailure, (s, { error }) => ({ ...s, error })),
);

export const roomPlayersAdapter = playersAdapter;
export const roomBulletsAdapter = bulletsAdapter;
export const roomPowerUpsAdapter = powerUpsAdapter;
