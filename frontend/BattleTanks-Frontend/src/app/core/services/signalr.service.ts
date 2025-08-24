import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { env } from '../utils/env';
import { Subject } from 'rxjs';
import {
  ChatMessageDto,
  PlayerPositionDto,
  PlayerStateDto,
  BulletStateDto,
  MapSnapshotDto,
  MapTileDto,
  PlayerLifeLostDto,
  PlayerRespawnedDto,
  RoomSnapshotDto,
} from '../models/game.models';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub: HubConnection | null = null;

  readonly playerJoined$ = new Subject<{ userId: string; username: string }>();
  readonly playerLeft$ = new Subject<string>();
  readonly chatMessage$ = new Subject<ChatMessageDto>();
  readonly playerMoved$ = new Subject<PlayerStateDto | { playerId: string; x: number; y: number; rotation: number }>();
  readonly bulletSpawned$ = new Subject<BulletStateDto>();
  readonly bulletDespawned$ = new Subject<{ bulletId: string; reason: string }>();

  readonly roomSnapshot$ = new Subject<RoomSnapshotDto>();
  readonly mapSnapshot$ = new Subject<MapSnapshotDto>();
  readonly mapTileUpdated$ = new Subject<MapTileDto>();
  readonly playerLifeLost$ = new Subject<PlayerLifeLostDto>();
  readonly playerRespawned$ = new Subject<PlayerRespawnedDto>();

  readonly reconnected$ = new Subject<void>();
  readonly disconnected$ = new Subject<void>();

  get isConnected() {
    return !!this.hub && this.hub.state === 'Connected';
  }

  async connect(): Promise<void> {
    if (this.hub) return;

    this.hub = new HubConnectionBuilder()
      .withUrl(env.HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // Server → Client registrations
    this.hub.on('playerJoined', (payload) => this.playerJoined$.next(payload));
    this.hub.on('playerLeft', (userId: string) => this.playerLeft$.next(userId));
    this.hub.on('chatMessage', (msg: ChatMessageDto) => this.chatMessage$.next(msg));
    this.hub.on('playerMoved', (p: any) => this.playerMoved$.next(p));
    this.hub.on('bulletSpawned', (b: BulletStateDto) => this.bulletSpawned$.next(b));
    this.hub.on('bulletDespawned', (bulletId: string, reason: string) =>
      this.bulletDespawned$.next({ bulletId, reason })
    );

    this.hub.on('roomSnapshot', (snap: RoomSnapshotDto) => this.roomSnapshot$.next(snap));
    this.hub.on('mapSnapshot', (snap: MapSnapshotDto) => this.mapSnapshot$.next(snap));
    this.hub.on('mapTileUpdated', (tile: MapTileDto) => this.mapTileUpdated$.next(tile));
    this.hub.on('playerLifeLost', (data: PlayerLifeLostDto) => this.playerLifeLost$.next(data));
    this.hub.on('playerRespawned', (data: PlayerRespawnedDto) => this.playerRespawned$.next(data));

    this.hub.onreconnected(() => this.reconnected$.next());
    this.hub.onclose(() => this.disconnected$.next());

    await this.hub.start();
  }

  async disconnect(): Promise<void> {
    if (!this.hub) return;
    try {
      await this.hub.stop();
    } finally {
      this.hub = null;
    }
  }

  async joinRoom(roomCode: string, username: string): Promise<void> {
    if (!this.hub) throw new Error('Hub not connected');
    await this.hub.invoke('JoinRoom', roomCode, username);
  }

  async sendChat(content: string): Promise<void> {
    if (!this.hub) throw new Error('Hub not connected');
    await this.hub.invoke('SendChat', content);
  }

  async updatePosition(dto: PlayerPositionDto): Promise<void> {
    if (!this.hub) throw new Error('Hub not connected');
    await this.hub.invoke('UpdatePosition', dto);
  }

  async spawnBullet(x: number, y: number, directionRadians: number, speed: number): Promise<string | null> {
    if (!this.hub) throw new Error('Hub not connected');
    return await this.hub.invoke<string | null>('SpawnBullet', x, y, directionRadians, speed);
  }
}
