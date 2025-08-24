import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  inject,
  signal,
  effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  selectBullets,
  selectPlayers,
  selectMapSize,
  selectTilesByKey,
  selectPowerUps,
} from '../store/room.selectors';
import { selectUser } from '../../auth/store/auth.selectors';
import { roomActions } from '../store/room.actions';

type TileRec = Record<string, { type: 0 | 1 | 2; hp?: number }>;

interface CosmeticBullet {
  id: string;
  x: number;
  y: number;
  dir: number;
  speed: number;
  trail: { x: number; y: number }[];
}

@Component({
  standalone: true,
  selector: 'app-room-canvas',
  imports: [CommonModule],
  templateUrl: './room-canvas.component.html',
  styleUrls: ['./room-canvas.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomCanvasComponent implements AfterViewInit, OnDestroy {
  private store = inject(Store);

  @ViewChild('wrap', { static: true }) wrapRef!: ElementRef<HTMLDivElement>;
  @ViewChild('gameCanvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  // Store signals
  players       = toSignal(this.store.select(selectPlayers),    { initialValue: [] });
  serverBullets = toSignal(this.store.select(selectBullets),    { initialValue: [] });
  powerUps      = toSignal(this.store.select(selectPowerUps),   { initialValue: [] });
  me            = toSignal(this.store.select(selectUser),       { initialValue: null });
  mapSize       = toSignal(this.store.select(selectMapSize),    { initialValue: { width: 0, height: 0, tileSize: 40 } });
  tilesByKey    = toSignal(this.store.select(selectTilesByKey), { initialValue: {} as TileRec });

  private ctx: CanvasRenderingContext2D | null = null;
  private running = false;
  private lastTs = 0;
  private pressed = new Set<string>();

  // Jugador local (coordenadas de mundo)
  private px = signal(300);
  private py = signal(200);
  private rot = signal(0);

  // Cámara
  private camX = signal(0);
  private camY = signal(0);
  private camSmooth = 0.15;

  // Collider tamaño tanque
  private halfW = 12;
  private halfH = 10;

  private spawnPlaced = false;

  // Balas cosméticas
  private cosmetics = new Map<string, CosmeticBullet>();
  private maxTrail = 10;

  // Viewport (CSS px)
  private viewportW = 800;
  private viewportH = 480;

  // Heartbeat de posición para que el server detecte pickup aunque estés quieto
  private hbAccum = 0;
  private hbInterval = 0.12; // segundos (~120ms)

  constructor() {
    // ---- Sincroniza balas cosméticas con servidor
    effect(() => {
      const bullets = this.serverBullets();
      const seen = new Set<string>();
      bullets.forEach(b => {
        seen.add(b.bulletId);
        const existing = this.cosmetics.get(b.bulletId);
        if (!existing) {
          this.cosmetics.set(b.bulletId, {
            id: b.bulletId,
            x: b.x, y: b.y, dir: b.directionRadians, speed: b.speed,
            trail: [],
          });
        } else {
          existing.x = b.x; existing.y = b.y; existing.dir = b.directionRadians; existing.speed = b.speed;
        }
      });
      [...this.cosmetics.keys()].forEach(id => { if (!seen.has(id)) this.cosmetics.delete(id); });
    });

    // ---- Coloca spawn inicial cuando tengamos jugador y mapa.
    effect(() => {
      const ms = this.mapSize();
      const meId = this.me()?.id;
      const mePlayer = this.players().find(p => p.playerId === meId);

      if (!this.spawnPlaced && ms.width > 0 && ms.height > 0 && mePlayer) {
        this.setPlayerPositionClamped(mePlayer.x, mePlayer.y);
        this.rot.set(mePlayer.rotation || 0);
        this.spawnPlaced = true;

        // Inicia cámara centrada en el jugador
        const initialCam = this.getClampedCamera(mePlayer.x - this.viewportW / 2, mePlayer.y - this.viewportH / 2);
        this.camX.set(initialCam.x);
        this.camY.set(initialCam.y);
      }
    });
  }

  ngAfterViewInit(): void {
    const canvas = this.canvasRef.nativeElement;
    this.ctx = canvas.getContext('2d');
    this.resizeCanvas(true);

    // Si no hay spawn aún, centra temporalmente
    if (!this.spawnPlaced) {
      this.px.set(Math.floor(this.viewportW / 2));
      this.py.set(Math.floor(this.viewportH / 2));
      const cam = this.getClampedCamera(this.px() - this.viewportW / 2, this.py() - this.viewportH / 2);
      this.camX.set(cam.x);
      this.camY.set(cam.y);
    }

    this.running = true;
    requestAnimationFrame(this.loop);
  }

  ngOnDestroy(): void { this.running = false; }

  // ====== Eventos ======
  @HostListener('window:resize') onWindowResize() { this.resizeCanvas(); }

  @HostListener('window:keydown', ['$event'])
  onDown(e: KeyboardEvent) {
    this.pressed.add(e.key.toLowerCase());
    if (e.code === 'Space') {
      this.store.dispatch(
        roomActions.spawnBullet({ x: this.px(), y: this.py(), dir: this.rot(), speed: 500 })
      );
      e.preventDefault();
    }
  }

  @HostListener('window:keyup', ['$event'])
  onUp(e: KeyboardEvent) { this.pressed.delete(e.key.toLowerCase()); }

  // ====== Resize según contenedor + DPR ======
  private resizeCanvas(initial = false) {
    const wrap = this.wrapRef.nativeElement;
    const canvas = this.canvasRef.nativeElement;
    const ctx = this.ctx;
    if (!ctx) return;

    let cssW = Math.max(320, Math.floor(wrap.clientWidth || wrap.getBoundingClientRect().width || 800));
    const cssH = Math.round(cssW * 9 / 16);
    const maxH = Math.max(360, Math.min(window.innerHeight - 32, 900));

    this.viewportW = cssW;
    this.viewportH = Math.min(cssH, maxH);

    const dpr = Math.max(1, Math.min(3, window.devicePixelRatio || 1));
    canvas.style.width = `${this.viewportW}px`;
    canvas.style.height = `${this.viewportH}px`;
    canvas.width = Math.floor(this.viewportW * dpr);
    canvas.height = Math.floor(this.viewportH * dpr);

    // Reset + escala por DPR
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    if (initial && !this.spawnPlaced) {
      this.px.set(Math.floor(this.viewportW / 2));
      this.py.set(Math.floor(this.viewportH / 2));
      const cam = this.getClampedCamera(this.px() - this.viewportW / 2, this.py() - this.viewportH / 2);
      this.camX.set(cam.x);
      this.camY.set(cam.y);
    }
  }

  // ====== Mundo / Colisión / Cámara ======
  private isSolidTile(tx: number, ty: number): boolean {
    const ms = this.mapSize();
    if (ms.width === 0 || ms.height === 0) return false; // mapa aún no listo => no bloquear
    if (tx < 0 || ty < 0 || tx >= ms.width || ty >= ms.height) return true; // bordes sólidos
    const t = this.tilesByKey()[`${tx},${ty}`];
    if (!t) return false;
    return t.type === 1 || (t.type === 2 && (t.hp ?? 1) > 0);
  }

  private rectHitsSolid(cx: number, cy: number): boolean {
    const { tileSize, width, height } = this.mapSize();
    if (width === 0 || height === 0 || tileSize <= 0) return false; // no bloquear
    const minX = Math.floor((cx - this.halfW) / tileSize);
    const maxX = Math.floor((cx + this.halfW - 1) / tileSize);
    const minY = Math.floor((cy - this.halfH) / tileSize);
    const maxY = Math.floor((cy + this.halfH - 1) / tileSize);
    for (let ty = minY; ty <= maxY; ty++) {
      for (let tx = minX; tx <= maxX; tx++) {
        if (this.isSolidTile(tx, ty)) return true;
      }
    }
    return false;
  }

  private clampToWorld(cx: number, cy: number): { x: number; y: number } {
    const ms = this.mapSize();
    const worldW = Math.max(1, ms.width * ms.tileSize);
    const worldH = Math.max(1, ms.height * ms.tileSize);
    const x = Math.min(Math.max(cx, this.halfW), Math.max(this.halfW, worldW - this.halfW));
    const y = Math.min(Math.max(cy, this.halfH), Math.max(this.halfH, worldH - this.halfH));
    return { x, y };
  }

  private getClampedCamera(rawX: number, rawY: number): { x: number; y: number } {
    const ms = this.mapSize();
    const worldW = Math.max(1, ms.width * ms.tileSize);
    const worldH = Math.max(1, ms.height * ms.tileSize);
    const maxCamX = Math.max(0, worldW - this.viewportW);
    const maxCamY = Math.max(0, worldH - this.viewportH);
    const x = Math.min(Math.max(rawX, 0), maxCamX);
    const y = Math.min(Math.max(rawY, 0), maxCamY);
    return { x, y };
  }

  private setPlayerPositionClamped(cx: number, cy: number) {
    const clamped = this.clampToWorld(cx, cy);
    this.px.set(clamped.x);
    this.py.set(clamped.y);
  }

  // ====== Loop ======
  private loop = (ts: number) => {
    if (!this.running || !this.ctx) return;

    const dt = (ts - (this.lastTs || ts)) / 1000;
    this.lastTs = ts;

    // Input
    const speed = 200;
    let dx = 0, dy = 0;
    if (this.pressed.has('w') || this.pressed.has('arrowup')) dy -= 1;
    if (this.pressed.has('s') || this.pressed.has('arrowdown')) dy += 1;
    if (this.pressed.has('a') || this.pressed.has('arrowleft')) dx -= 1;
    if (this.pressed.has('d') || this.pressed.has('arrowright')) dx += 1;

    let moved = false;

    if (dx !== 0 || dy !== 0) {
      const len = Math.hypot(dx, dy) || 1;
      dx /= len; dy /= len;

      const tryX = this.px() + dx * speed * dt;
      const clampedX = this.clampToWorld(tryX, this.py()).x;
      if (!this.rectHitsSolid(clampedX, this.py())) {
        this.px.set(clampedX);
        moved = true;
      }

      const tryY = this.py() + dy * speed * dt;
      const clampedY = this.clampToWorld(this.px(), tryY).y;
      if (!this.rectHitsSolid(this.px(), clampedY)) {
        this.py.set(clampedY);
        moved = true;
      }

      if (moved) this.rot.set(Math.atan2(dy, dx));
    }

    // --- Heartbeat de posición: siempre mandamos cada hbInterval o si hubo movimiento
    this.hbAccum += dt;
    if (moved || this.hbAccum >= this.hbInterval) {
      this.hbAccum = 0;
      const meId = this.me()?.id ?? 'me';
      this.store.dispatch(roomActions.updatePosition({
        dto: { playerId: meId, x: this.px(), y: this.py(), rotation: this.rot(), timestamp: Date.now() }
      }));
    }

    // Balas cosméticas
    this.cosmetics.forEach(c => {
      c.x += Math.cos(c.dir) * c.speed * dt;
      c.y += Math.sin(c.dir) * c.speed * dt;
      c.trail.push({ x: c.x, y: c.y });
      if (c.trail.length > this.maxTrail) c.trail.shift();
    });

    const ctx = this.ctx!;
    // limpiar viewport (en unidades CSS gracias a DPR setTransform)
    ctx.clearRect(0, 0, this.viewportW, this.viewportH);

    // Cámara hacia el jugador (suavizada)
    const targetCamX = this.px() - this.viewportW / 2;
    const targetCamY = this.py() - this.viewportH / 2;
    const clampedTarget = this.getClampedCamera(targetCamX, targetCamY);
    const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
    this.camX.set(lerp(this.camX(), clampedTarget.x, this.camSmooth));
    this.camY.set(lerp(this.camY(), clampedTarget.y, this.camSmooth));

    // Dibujo mundo
    ctx.save();
    ctx.translate(-this.camX(), -this.camY());

    const { tileSize } = this.mapSize();
    const worldW = Math.max(1, this.mapSize().width * tileSize);
    const worldH = Math.max(1, this.mapSize().height * tileSize);

    // Fondo
    ctx.fillStyle = '#0b1e16';
    ctx.fillRect(0, 0, worldW, worldH);

    // Grid visible
    if (tileSize > 0) {
      ctx.strokeStyle = 'rgba(0,255,170,0.06)';
      ctx.lineWidth = 1;
      const startGX = Math.floor(this.camX() / tileSize) * tileSize;
      const endGX = Math.min(worldW, this.camX() + this.viewportW + tileSize);
      for (let x = startGX; x <= endGX; x += tileSize) {
        ctx.beginPath();
        ctx.moveTo(x, this.camY());
        ctx.lineTo(x, Math.min(worldH, this.camY() + this.viewportH));
        ctx.stroke();
      }
      const startGY = Math.floor(this.camY() / tileSize) * tileSize;
      const endGY = Math.min(worldH, this.camY() + this.viewportH + tileSize);
      for (let y = startGY; y <= endGY; y += tileSize) {
        ctx.beginPath();
        ctx.moveTo(this.camX(), y);
        ctx.lineTo(Math.min(worldW, this.camX() + this.viewportW), y);
        ctx.stroke();
      }
    }

    // Tiles visibles
    const tiles = this.tilesByKey();
    if (tileSize > 0) {
      const visMinTx = Math.max(0, Math.floor(this.camX() / tileSize) - 1);
      const visMaxTx = Math.min(this.mapSize().width - 1, Math.floor((this.camX() + this.viewportW) / tileSize) + 1);
      const visMinTy = Math.max(0, Math.floor(this.camY() / tileSize) - 1);
      const visMaxTy = Math.min(this.mapSize().height - 1, Math.floor((this.camY() + this.viewportH) / tileSize) + 1);

      for (let ty = visMinTy; ty <= visMaxTy; ty++) {
        for (let tx = visMinTx; tx <= visMaxTx; tx++) {
          const t = tiles[`${tx},${ty}`];
          if (!t) continue;
          const x = tx * tileSize;
          const y = ty * tileSize;
          if (t.type === 1) {
            ctx.fillStyle = '#1f2937';
            ctx.fillRect(x, y, tileSize, tileSize);
          } else if (t.type === 2) {
            const hp = t.hp ?? 2;
            ctx.fillStyle = hp >= 2 ? '#b45309' : '#f59e0b';
            ctx.fillRect(x, y, tileSize, tileSize);
            // barra hp
            ctx.fillStyle = '#111827';
            ctx.fillRect(x + 4, y + tileSize - 8, tileSize - 8, 4);
            ctx.fillStyle = '#fde68a';
            const w = Math.max(4, (tileSize - 8) * (hp / 2));
            ctx.fillRect(x + 4, y + tileSize - 8, w, 4);
          }
        }
      }
    }

    // Power-ups (soporta id o powerUpId por si tu DTO cambia)
    this.powerUps().forEach((p: any) => {
      const x = p.x, y = p.y;
      const pid = (p.id ?? p.powerUpId) as string | undefined;

      ctx.fillStyle = '#f87171';
      ctx.beginPath();
      ctx.moveTo(x, y + 5);
      ctx.arc(x - 4, y - 2, 4, Math.PI, 0, true);
      ctx.arc(x + 4, y - 2, 4, Math.PI, 0, true);
      ctx.lineTo(x, y + 5);
      ctx.closePath();
      ctx.fill();

      // // Depura el id si lo necesitas
      // if (!pid) console.warn('PowerUp sin id/powerUpId', p);
    });

    // Rastro balas
    this.cosmetics.forEach(b => {
      for (let i = 1; i < b.trail.length; i++) {
        const p0 = b.trail[i - 1], p1 = b.trail[i];
        const alpha = i / b.trail.length;
        ctx.strokeStyle = `rgba(34, 211, 238, ${alpha * 0.7})`;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(p0.x, p0.y);
        ctx.lineTo(p1.x, p1.y);
        ctx.stroke();
      }
    });

    // Balas
    ctx.fillStyle = '#22d3ee';
    this.cosmetics.forEach(b => {
      ctx.beginPath();
      ctx.arc(b.x, b.y, 3, 0, Math.PI * 2);
      ctx.fill();
    });

    // Jugadores
    const myId = this.me()?.id ?? null;
    const myName = this.me()?.username?.toLowerCase() ?? null;
    this.players().forEach(p => {
      const isMe = (p.playerId === myId) || (!!myName && p.username?.toLowerCase() === myName);

      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.rotation || 0);
      ctx.fillStyle = isMe ? '#86efac' : '#34d399';
      ctx.fillRect(-12, -8, 24, 16);
      ctx.fillStyle = isMe ? '#16a34a' : '#065f46';
      ctx.fillRect(0, -2, 16, 4);
      ctx.restore();

      const label = isMe ? 'TÚ' : (p.username ?? 'Tank');
      const bgW = Math.max(24, label.length * 7);
      ctx.fillStyle = '#0b1e16';
      ctx.fillRect(p.x - 18, p.y - 22, bgW, 12);
      ctx.fillStyle = '#a7f3d0';
      ctx.font = '10px monospace';
      ctx.fillText(label, p.x - 16, p.y - 12);
    });

    ctx.restore(); // fin cámara

    requestAnimationFrame(this.loop);
  };
}