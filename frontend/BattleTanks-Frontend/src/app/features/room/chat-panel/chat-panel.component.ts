import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { selectChat } from '../store/room.selectors';
import { roomActions } from '../store/room.actions';

@Component({
  standalone: true,
  selector: 'app-chat-panel',
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-panel.component.html',
  styleUrls: ['./chat-panel.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatPanelComponent {
  private store = inject(Store);

  chat = toSignal(this.store.select(selectChat), { initialValue: [] });
  input = signal('');

  send() {
    const content = this.input().trim();
    if (!content) return;
    this.store.dispatch(roomActions.sendMessage({ content }));
    this.input.set('');
  }
}
