import { createFeatureSelector, createSelector } from '@ngrx/store';
import { roomBulletsAdapter, roomPlayersAdapter, roomPowerUpsAdapter, RoomState } from './room.reducer';

export const selectRoom = createFeatureSelector<RoomState>('room');

export const selectRoomCode = createSelector(selectRoom, (s) => s.roomCode);
export const selectRoomId = createSelector(selectRoom, (s) => s.roomId);
export const selectRoomStatus = createSelector(selectRoom, (s) => s.status);
export const selectHubConnected = createSelector(selectRoom, (s) => s.hubConnected);
export const selectJoined = createSelector(selectRoom, (s) => s.joined);
export const selectRoomError = createSelector(selectRoom, (s) => s.error);
export const selectChat = createSelector(selectRoom, (s) => s.chat);
export const selectGameResult = createSelector(selectRoom, (s) => s.gameResult);

const playersSelectors = roomPlayersAdapter.getSelectors();
const bulletsSelectors = roomBulletsAdapter.getSelectors();
const powerUpsSelectors = roomPowerUpsAdapter.getSelectors();

export const selectPlayers = createSelector(selectRoom, (s) => playersSelectors.selectAll(s.players));
export const selectAlivePlayers = createSelector(selectPlayers, (players) => players.filter((p) => (p.lives ?? 0) > 0));
export const selectBullets = createSelector(selectRoom, (s) => bulletsSelectors.selectAll(s.bullets));
export const selectPowerUps = createSelector(selectRoom, (s) => powerUpsSelectors.selectAll(s.powerUps));

export const selectMap = createSelector(selectRoom, (s) => s.map);
export const selectMapSize = createSelector(selectMap, (m) => ({
  width: m?.width ?? 0,
  height: m?.height ?? 0,
  tileSize: m?.tileSize ?? 0,
}));
export const selectTilesByKey = createSelector(selectMap, (m) => m?.tilesByKey ?? {});

export const selectTile = (x: number, y: number) =>
  createSelector(selectTilesByKey, (tiles) => tiles[`${x},${y}`]);
