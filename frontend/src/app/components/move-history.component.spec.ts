import { TestBed } from '@angular/core/testing';
import { move } from '../testing/game-fixtures';
import { MoveHistoryComponent } from './move-history.component';

describe('MoveHistoryComponent', () => {
  it('shows an empty message when there are no moves', () => {
    const fixture = TestBed.createComponent(MoveHistoryComponent);
    fixture.componentRef.setInput('moves', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No moves yet.');
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
  });

  it('renders move number, player and position for each move', () => {
    const fixture = TestBed.createComponent(MoveHistoryComponent);
    fixture.componentRef.setInput('moves', [move(1, 'X', 0), move(2, 'O', 4)]);
    fixture.detectChanges();

    const rows = Array.from(fixture.nativeElement.querySelectorAll('tbody tr')) as HTMLElement[];
    const cells = rows.map((r) => Array.from(r.querySelectorAll('td')).map((td) => td.textContent?.trim()));
    expect(cells).toEqual([
      ['1', 'X', 'Row 1, Column 1'],
      ['2', 'O', 'Row 2, Column 2'],
    ]);
  });
});
