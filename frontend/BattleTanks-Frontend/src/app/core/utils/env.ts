export const env = {
  API_BASE_URL: (import.meta as any)?.env?.VITE_API_BASE_URL ?? 'http://localhost:5284/api/v1',
  HUB_URL: (import.meta as any)?.env?.VITE_HUB_URL ?? 'http://localhost:5284/game-hub',
};
