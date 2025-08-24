export interface PlayerStateDto {
  playerId: string;
  username: string;
  x: number;
  y: number;
  rotation: number;
  health: number;
  isAlive: boolean;
  lives?: number;
  score?: number;
}

export interface PlayerPositionDto {
  playerId: string;
  x: number;
  y: number;
  rotation: number;
  timestamp: number;
}

export type SystemOrUser = 'System' | 'User';

export interface ChatMessageDto {
  messageId: string;
  userId: string;
  username: string;
  content: string;
  type: SystemOrUser;
  sentAt: string;
}

export interface BulletStateDto {
  bulletId: string;
  roomId: string;
  shooterId: string;
  x: number;
  y: number;
  directionRadians: number;
  speed: number;
  spawnTimestamp: number;
  isActive: boolean;
}

export type MapTileType = 0 | 1 | 2; // 0=Empty, 1=Indestructible, 2=Destructible

export interface MapTileDto {
  x: number;
  y: number;
  type: MapTileType;
  hp?: number;
}

export interface MapSnapshotDto {
  roomId: string;
  width: number;
  height: number;
  tileSize: number;
  tiles: MapTileDto[];
}

export interface PlayerLifeLostDto {
  targetPlayerId: string;
  livesAfter: number;
  eliminated: boolean;
}

export interface PlayerRespawnedDto {
  playerId: string;
  x: number;
  y: number;
}

export interface PlayerScoredDto {
  playerId: string;
  score: number;
}

export interface GameEndedDto {
  winnerPlayerId: string;
  scores: PlayerScoredDto[];
}

export interface RoomSnapshotDto {
  roomId: string;
  roomCode: string;
  players: PlayerStateDto[];
}

export type PowerUpType = 'ExtraLife';

export interface PowerUpDto {
  id: string;
  roomId: string;
  type: PowerUpType;
  x: number;
  y: number;
}
