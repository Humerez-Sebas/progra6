import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { selectRooms, selectRoomsLoading } from '../store/rooms.selectors';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { authActions } from '../../auth/store/auth.actions';
import { Observable } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-rooms-list',
  imports: [CommonModule],
  templateUrl: './rooms-list.component.html',
  styleUrls: ['./rooms-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomsListComponent {
  private store = inject(Store);
  private router = inject(Router);
  private auth = inject(AuthService);

  rooms$: Observable<any[]> = this.store.select(selectRooms);
  loading$: Observable<boolean> = this.store.select(selectRoomsLoading);

  enter(code: string) {
    this.router.navigateByUrl(`/rooms/${encodeURIComponent(code)}`);
  }

  onLogout() {
    this.auth.logout().subscribe(() => {
      this.store.dispatch(authActions.logoutSuccess());
    });
  }
}
