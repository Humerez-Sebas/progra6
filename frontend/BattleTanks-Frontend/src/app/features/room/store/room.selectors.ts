import { createFeatureSelector, createSelector } from '@ngrx/store';
import { roomBulletsAdapter, roomPlayersAdapter, roomPowerUpsAdapter, RoomState } from './room.reducer';

export const selectRoomState = createFeatureSelector<RoomState>('room');

export const selectRoomCode = createSelector(selectRoomState, (s) => s.roomCode);
export const selectRoomId = createSelector(selectRoomState, (s) => s.roomId);
export const selectHubConnected = createSelector(selectRoomState, (s) => s.hubConnected);
export const selectJoined = createSelector(selectRoomState, (s) => s.joined);
export const selectRoomError = createSelector(selectRoomState, (s) => s.error);
export const selectChat = createSelector(selectRoomState, (s) => s.chat);

const playersSelectors = roomPlayersAdapter.getSelectors();
const bulletsSelectors = roomBulletsAdapter.getSelectors();
const powerUpsSelectors = roomPowerUpsAdapter.getSelectors();

export const selectPlayers = createSelector(selectRoomState, (s) => playersSelectors.selectAll(s.players));
export const selectBullets = createSelector(selectRoomState, (s) => bulletsSelectors.selectAll(s.bullets));
export const selectPowerUps = createSelector(selectRoomState, (s) => powerUpsSelectors.selectAll(s.powerUps));

export const selectMap = createSelector(selectRoomState, (s) => s.map);
export const selectMapSize = createSelector(selectMap, (m) => ({
  width: m?.width ?? 0,
  height: m?.height ?? 0,
  tileSize: m?.tileSize ?? 0,
}));
export const selectTilesByKey = createSelector(selectMap, (m) => m?.tilesByKey ?? {});

export const selectTile = (x: number, y: number) =>
  createSelector(selectTilesByKey, (tiles) => tiles[`${x},${y}`]);
