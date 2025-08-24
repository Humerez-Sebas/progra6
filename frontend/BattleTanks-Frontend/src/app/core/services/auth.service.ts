import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { env } from '../utils/env';
import { RegisterDto, LoginDto, UserDto, VerifyResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = env.API_BASE_URL;

  constructor(private http: HttpClient) {}

  register(dto: RegisterDto) {
    return this.http.post<UserDto>(`${this.base}/auth/register`, dto);
  }

  login(dto: LoginDto) {
    return this.http.post<UserDto>(`${this.base}/auth/login`, dto);
  }

  logout() {
    return this.http.post<void>(`${this.base}/auth/logout`, {});
  }

  profile() {
    return this.http.get<UserDto>(`${this.base}/auth/profile`);
  }

  verify() {
    return this.http.get<VerifyResponse>(`${this.base}/auth/verify`);
  }
}
