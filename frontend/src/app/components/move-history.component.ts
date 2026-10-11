import { Component, input } from '@angular/core';
import { MoveEntry } from '../models/game.models';

@Component({
  selector: 'app-move-history',
  template: `
    <section class="card" aria-labelledby="history-title">
      <h2 id="history-title">Move History</h2>
      @if (moves().length === 0) {
        <p class="empty">No moves yet.</p>
      } @else {
        <div class="scroll">
          <table>
            <thead>
              <tr><th scope="col">Move</th><th scope="col">Player</th><th scope="col">Position</th></tr>
            </thead>
            <tbody>
              @for (move of moves(); track move.moveNumber) {
                <tr>
                  <td>{{ move.moveNumber }}</td>
                  <td [class.x]="move.player === 'X'" [class.o]="move.player === 'O'">{{ move.player }}</td>
                  <td>{{ move.position }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: `
    h2 { margin: 0 0 0.75rem; font-size: 1.05rem; }
    .scroll { max-height: 260px; overflow-y: auto; }
    table { width: 100%; border-collapse: collapse; font-size: 0.95rem; }
    th, td { text-align: left; padding: 0.4rem 0.5rem; border-bottom: 1px solid var(--border); }
    th { position: sticky; top: 0; background: var(--surface); font-weight: 600; color: var(--muted); }
    td.x { color: var(--x); font-weight: 700; }
    td.o { color: var(--o); font-weight: 700; }
    .empty { margin: 0; color: var(--muted); }
  `,
})
export class MoveHistoryComponent {
  readonly moves = input.required<MoveEntry[]>();
}
