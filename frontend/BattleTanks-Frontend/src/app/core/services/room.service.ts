import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { env } from '../utils/env';
import { CreateRoomDto, RoomStateDto } from '../models/room.models';

@Injectable({ providedIn: 'root' })
export class RoomService {
  private readonly base = env.API_BASE_URL;

  constructor(private http: HttpClient) {}

  getRooms() {
    return this.http.get<RoomStateDto[]>(`${this.base}/Rooms`);
  }

  createRoom(dto: CreateRoomDto) {
    return this.http.post<RoomStateDto>(`${this.base}/Rooms`, dto);
  }

  getRoom(roomId: string) {
    return this.http.get<RoomStateDto>(`${this.base}/Rooms/${roomId}`);
  }

  startGame(roomId: string) {
    return this.http.post<RoomStateDto>(`${this.base}/Rooms/${roomId}/start`, {});
  }
}
