import { createActionGroup, emptyProps, props } from '@ngrx/store';
import {
  ChatMessageDto,
  PlayerPositionDto,
  PlayerStateDto,
  BulletStateDto,
  MapSnapshotDto,
  MapTileDto,
  PlayerLifeLostDto,
  PlayerRespawnedDto,
  PlayerScoredDto,
  GameEndedDto,
  RoomSnapshotDto,
  PowerUpDto,
} from '../../../core/models/game.models';

export const roomActions = createActionGroup({
  source: 'Room',
  events: {
    // Hub
    'Hub Connect': emptyProps(),
    'Hub Connected': emptyProps(),
    'Hub Disconnected': emptyProps(),
    'Hub Reconnected': emptyProps(),
    'Hub Error': props<{ error: string }>(),

    // Sala
    'Join Room': props<{ code: string; username: string }>(),
    'Joined': emptyProps(),
    'Leave Room': emptyProps(),
    'Left': emptyProps(),

    'Roster Loaded': props<{ players: PlayerStateDto[]; roomId: string | null }>(),

    // Snapshots nuevos
    'Room Snapshot Received': props<{ snapshot: RoomSnapshotDto }>(),
    'Map Snapshot Received': props<{ snapshot: MapSnapshotDto }>(),
    'Map Tile Updated': props<{ tile: MapTileDto }>(),
    
    'Player Joined': props<{ userId: string; username: string }>(),
    'Player Left': props<{ userId: string }>(),
    'Player Moved': props<{ player: PlayerStateDto | { playerId: string; x: number; y: number; rotation: number; health?: number; isAlive?: boolean; username?: string } }>(),
    'Bullet Spawned': props<{ bullet: BulletStateDto }>(),
    'Bullet Despawned': props<{ bulletId: string; reason?: string }>(),
    'Player Life Lost': props<{ data: PlayerLifeLostDto }>(),
    'Player Respawned': props<{ data: PlayerRespawnedDto }>(),
    'Player Scored': props<{ data: PlayerScoredDto }>(),
    'Game Ended': props<{ data: GameEndedDto }>(),
    'PowerUps Snapshot Received': props<{ powerUps: PowerUpDto[] }>(),
    'PowerUp Spawned': props<{ powerUp: PowerUpDto }>(),
    'PowerUp Collected': props<{ powerUpId: string; userId: string }>(),
    'Message Received': props<{ msg: ChatMessageDto }>(),

    // Cliente→servidor
    'Send Message': props<{ content: string }>(),
    'Update Position': props<{ dto: PlayerPositionDto }>(),
    'Spawn Bullet': props<{ x: number; y: number; dir: number; speed: number }>(),
  },
});
