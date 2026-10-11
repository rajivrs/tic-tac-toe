import { Component, input, output } from '@angular/core';
import { Player } from '../models/game.models';

@Component({
  selector: 'app-board',
  template: `
    <div class="board" role="group" aria-label="Tic Tac Toe board">
      @for (cell of cells(); track $index) {
        <button
          type="button"
          class="cell"
          [class.x]="cell === 'X'"
          [class.o]="cell === 'O'"
          [class.winning]="isWinning($index)"
          [disabled]="disabled() || cell !== null"
          [attr.aria-label]="label($index, cell)"
          (click)="cellSelected.emit($index)"
        >{{ cell }}</button>
      }
    </div>
  `,
  styles: `
    .board {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 10px;
      width: min(100%, 360px);
      margin: 0 auto;
    }
    .cell {
      aspect-ratio: 1 / 1;
      font-size: clamp(2.5rem, 9vw, 3.75rem);
      font-weight: 700;
      line-height: 1;
      border: 2px solid var(--border);
      border-radius: 14px;
      background: var(--surface);
      color: var(--text);
      cursor: pointer;
      transition: background 0.12s, transform 0.12s, box-shadow 0.12s;
    }
    .cell:hover:enabled { background: var(--hover); transform: translateY(-1px); }
    .cell:focus-visible { outline: 3px solid var(--focus); outline-offset: 2px; }
    .cell:disabled { cursor: not-allowed; opacity: 1; }
    .cell.x { color: var(--x); }
    .cell.o { color: var(--o); }
    .cell.winning {
      background: var(--win-bg);
      border-color: var(--win-border);
      box-shadow: 0 0 0 3px var(--win-border) inset;
    }
  `,
})
export class BoardComponent {
  readonly cells = input.required<(Player | null)[]>();
  readonly winningCells = input<number[]>([]);
  readonly disabled = input(false);
  readonly cellSelected = output<number>();

  protected isWinning(index: number): boolean {
    return this.winningCells().includes(index);
  }

  protected label(index: number, cell: Player | null): string {
    const position = `Row ${Math.floor(index / 3) + 1}, Column ${(index % 3) + 1}`;
    return `${position}, ${cell ?? 'empty'}`;
  }
}
