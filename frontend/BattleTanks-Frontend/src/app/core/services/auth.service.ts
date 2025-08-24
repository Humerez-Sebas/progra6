import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { env } from '../utils/env';
import { RegisterDto, LoginDto, UserDto, VerifyResponse, AuthResponse } from '../models/auth.models';
import { map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = env.API_BASE_URL;

  constructor(private http: HttpClient) {}

  register(dto: RegisterDto) {
    return this.http
      .post<AuthResponse>(`${this.base}/auth/register`, dto)
      .pipe(map((res) => res.user));
  }

  login(dto: LoginDto) {
    return this.http
      .post<AuthResponse>(`${this.base}/auth/login`, dto)
      .pipe(map((res) => res.user));
  }

  logout() {
    return this.http.post<void>(`${this.base}/auth/logout`, {});
  }

  profile() {
    return this.http
      .get<AuthResponse>(`${this.base}/auth/profile`)
      .pipe(map((res) => res.user));
  }

  verify() {
    return this.http.get<VerifyResponse>(`${this.base}/auth/verify`);
  }
}