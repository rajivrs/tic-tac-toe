import { TestBed } from '@angular/core/testing';
import { ScoreboardComponent } from './scoreboard.component';

describe('ScoreboardComponent', () => {
  it('renders X wins, O wins and draws', () => {
    const fixture = TestBed.createComponent(ScoreboardComponent);
    fixture.componentRef.setInput('score', { xWins: 3, oWins: 1, draws: 2 });
    fixture.detectChanges();

    const text = (id: string) => fixture.nativeElement.querySelector(`[data-testid="${id}"]`).textContent.trim();
    expect(text('x-wins')).toBe('3');
    expect(text('o-wins')).toBe('1');
    expect(text('draws')).toBe('2');
  });
});
