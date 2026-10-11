import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Player } from '../models/game.models';
import { BoardComponent } from './board.component';

@Component({
  imports: [BoardComponent],
  template: `<app-board [cells]="cells" [winningCells]="winning" [disabled]="locked" (cellSelected)="picked.push($event)" />`,
})
class HostComponent {
  cells: (Player | null)[] = ['X', null, null, null, 'O', null, null, null, null];
  winning: number[] = [];
  locked = false;
  picked: number[] = [];
}

function setup(configure: (host: HostComponent) => void = () => {}) {
  const fixture = TestBed.createComponent(HostComponent);
  configure(fixture.componentInstance);
  fixture.detectChanges();
  const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
  return { fixture, host: fixture.componentInstance, buttons };
}

describe('BoardComponent', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [HostComponent] }));

  it('renders a 3x3 board of 9 cells showing X and O', () => {
    const { buttons } = setup();
    expect(buttons).toHaveLength(9);
    expect(buttons[0].textContent?.trim()).toBe('X');
    expect(buttons[4].textContent?.trim()).toBe('O');
  });

  it('locks occupied cells and leaves empty cells clickable', () => {
    const { buttons } = setup();
    expect(buttons[0].disabled).toBe(true);
    expect(buttons[4].disabled).toBe(true);
    expect(buttons[1].disabled).toBe(false);
  });

  it('emits the cell index when an empty cell is clicked', () => {
    const { buttons, host } = setup();
    buttons[8].click();
    expect(host.picked).toEqual([8]);
  });

  it('does not emit when an occupied cell is clicked', () => {
    const { buttons, host } = setup();
    buttons[0].click();
    expect(host.picked).toEqual([]);
  });

  it('disables every cell when the board is locked', () => {
    const { buttons } = setup((h) => (h.locked = true));
    expect(buttons.every((b) => b.disabled)).toBe(true);
  });

  it('highlights winning cells', () => {
    const { buttons } = setup((h) => {
      h.cells = ['X', 'X', 'X', 'O', 'O', null, null, null, null];
      h.winning = [0, 1, 2];
      h.locked = true;
    });
    const highlighted = buttons.map((b, i) => (b.classList.contains('winning') ? i : -1)).filter((i) => i >= 0);
    expect(highlighted).toEqual([0, 1, 2]);
  });

  it('gives each cell an accessible label with its position', () => {
    const { buttons } = setup();
    expect(buttons[0].getAttribute('aria-label')).toBe('Row 1, Column 1, X');
    expect(buttons[5].getAttribute('aria-label')).toBe('Row 2, Column 3, empty');
  });
});
