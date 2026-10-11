import { Component, input } from '@angular/core';
import { Scoreboard } from '../models/game.models';

@Component({
  selector: 'app-scoreboard',
  template: `
    <section class="card" aria-labelledby="score-title">
      <h2 id="score-title">Scoreboard</h2>
      <div class="tiles">
        <div class="tile x"><span class="value" data-testid="x-wins">{{ score().xWins }}</span><span class="name">X wins</span></div>
        <div class="tile o"><span class="value" data-testid="o-wins">{{ score().oWins }}</span><span class="name">O wins</span></div>
        <div class="tile"><span class="value" data-testid="draws">{{ score().draws }}</span><span class="name">Draws</span></div>
      </div>
    </section>
  `,
  styles: `
    h2 { margin: 0 0 0.75rem; font-size: 1.05rem; }
    .tiles { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.6rem; }
    .tile { display: flex; flex-direction: column; align-items: center; padding: 0.6rem 0.25rem;
            border: 1px solid var(--border); border-radius: 10px; background: var(--bg); }
    .value { font-size: 1.8rem; font-weight: 700; }
    .name { font-size: 0.8rem; color: var(--muted); }
    .tile.x .value { color: var(--x); }
    .tile.o .value { color: var(--o); }
  `,
})
export class ScoreboardComponent {
  readonly score = input.required<Scoreboard>();
}
