export interface RegisterDto {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginDto {
  usernameOrEmail: string;
  password: string;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  gamesPlayed: number;
  gamesWon: number;
  totalScore: number;
  winRate: number;
  createdAt: string;
}

export interface VerifyResponse {
  authenticated: boolean;
  userId?: string;
  username?: string;
}

export interface AuthResponse {
  success: boolean;
  message?: string;
  user: UserDto;
}
