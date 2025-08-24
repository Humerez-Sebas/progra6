import { createReducer, on } from '@ngrx/store';
import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { roomActions } from './room.actions';
import {
  BulletStateDto,
  ChatMessageDto,
  PlayerStateDto,
  MapTileDto,
  MapTileType,
} from '../../../core/models/game.models';

export interface PlayerEntity extends PlayerStateDto {}
export interface BulletEntity extends BulletStateDto {}

export interface RoomState {
  roomId: string | null;
  roomCode: string | null;
  joined: boolean;
  hubConnected: boolean;
  error: string | null;
  players: EntityState<PlayerEntity>;
  bullets: EntityState<BulletEntity>;
  chat: ChatMessageDto[];
  lastUsername: string | null;

  map: {
    width: number;
    height: number;
    tileSize: number;
    tilesByKey: Record<string, { type: MapTileType; hp?: number }>;
  } | null;
}

const playersAdapter = createEntityAdapter<PlayerEntity>({ selectId: (p) => p.playerId });
const bulletsAdapter = createEntityAdapter<BulletEntity>({ selectId: (b) => b.bulletId });

const initialState: RoomState = {
  roomId: null,
  roomCode: null,
  joined: false,
  hubConnected: false,
  error: null,
  players: playersAdapter.getInitialState(),
  bullets: bulletsAdapter.getInitialState(),
  chat: [],
  lastUsername: null,
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
    players: playersAdapter.removeAll(s.players),
    bullets: bulletsAdapter.removeAll(s.bullets),
    chat: [],
    map: null,
  })),

  // Snapshots
  on(roomActions.roomSnapshotReceived, (s, { snapshot }) => ({
    ...s,
    players: playersAdapter.setAll(snapshot.players ?? [], s.players),
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
      health: existing?.health ?? 100,
      isAlive: existing?.isAlive ?? true,
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
      health: (player as any).health ?? existing?.health ?? 100,
      isAlive: (player as any).isAlive ?? existing?.isAlive ?? true,
    };
    return { ...s, players: playersAdapter.upsertOne(merged, s.players) };
  }),

  // Vidas / Respawn
  on(roomActions.playerLifeLost, (s, { data }) => {
    const p = s.players.entities[data.targetPlayerId];
    if (!p) return s;
    const updated: PlayerStateDto = {
      ...p,
      // health visual si quieres (no sabemos max HP exacto; usamos bool eliminated)
      isAlive: !data.eliminated,
      lives: data.livesAfter,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  on(roomActions.playerRespawned, (s, { data }) => {
    const p = s.players.entities[data.playerId];
    const updated: PlayerStateDto = {
      ...(p ?? { playerId: data.playerId, username: 'Player', rotation: 0, health: 100, isAlive: true, x: 0, y: 0 }),
      x: data.x,
      y: data.y,
      isAlive: true,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  on(roomActions.playerScored, (s, { data }) => {
    const p = s.players.entities[data.playerId];
    const updated: PlayerStateDto = {
      ...(p ?? { playerId: data.playerId, username: 'Player', rotation: 0, health: 100, isAlive: true, x: 0, y: 0 }),
      score: data.score,
    };
    return { ...s, players: playersAdapter.upsertOne(updated, s.players) };
  }),

  // Balas
  on(roomActions.bulletSpawned, (s, { bullet }) => ({
    ...s,
    bullets: bulletsAdapter.upsertOne(bullet, s.bullets),
  })),
  on(roomActions.bulletDespawned, (s, { bulletId }) => ({
    ...s,
    bullets: bulletsAdapter.removeOne(bulletId, s.bullets),
  })),

  // Roster HTTP
  on(roomActions.rosterLoaded, (s, { players, roomId }) => ({
    ...s,
    roomId: roomId ?? s.roomId,
    players: playersAdapter.upsertMany(players, s.players),
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
        ...(ex ?? { playerId: sc.playerId, username: 'Player', rotation: 0, health: 100, isAlive: true, x: 0, y: 0 }),
        score: sc.score,
      };
      players = playersAdapter.upsertOne(up, players);
    }
    return { ...s, players };
  }),
);

export const roomPlayersAdapter = playersAdapter;
export const roomBulletsAdapter = bulletsAdapter;
