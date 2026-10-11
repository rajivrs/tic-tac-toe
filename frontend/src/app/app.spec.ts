import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { GameState } from './models/game.models';
import { GameApiService } from './services/game-api.service';
import { emptyGame, move } from './testing/game-fixtures';

describe('App', () => {
  let api: {
    createGame: ReturnType<typeof vi.fn>;
    getGame: ReturnType<typeof vi.fn>;
    playMove: ReturnType<typeof vi.fn>;
    undo: ReturnType<typeof vi.fn>;
    resetGame: ReturnType<typeof vi.fn>;
    getScoreboard: ReturnType<typeof vi.fn>;
    resetScoreboard: ReturnType<typeof vi.fn>;
  };

  /** Lets the component's pending promise chain (firstValueFrom -> signals) finish, then re-renders. */
  async function settle(fixture: ComponentFixture<App>) {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  async function render(initial: GameState = emptyGame()) {
    api.createGame.mockReturnValue(of(initial));
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await settle(fixture);
    const el = fixture.nativeElement as HTMLElement;
    return { fixture, el };
  }

  const buttonByText = (el: HTMLElement, text: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === text) as HTMLButtonElement;

  beforeEach(() => {
    sessionStorage.clear();
    api = {
      createGame: vi.fn(),
      getGame: vi.fn(),
      playMove: vi.fn(),
      undo: vi.fn(),
      resetGame: vi.fn(),
      getScoreboard: vi.fn(),
      resetScoreboard: vi.fn(),
    };
    TestBed.configureTestingModule({ imports: [App], providers: [{ provide: GameApiService, useValue: api }] });
  });

  it('creates a Two Player game on start and shows whose turn it is', async () => {
    const { el } = await render();
    expect(api.createGame).toHaveBeenCalledWith('TwoPlayer');
    expect(el.querySelector('[data-testid="status-message"]')?.textContent).toContain("Player X's turn");
    expect(el.querySelector('[data-testid="mode-label"]')?.textContent).toContain('Two Player Mode');
    expect(el.querySelectorAll('app-board button')).toHaveLength(9);
  });

  it('sends the current player and clicked cell to the backend and renders the response', async () => {
    const { fixture, el } = await render();
    api.playMove.mockReturnValue(
      of(emptyGame({ board: ['X', null, null, null, null, null, null, null, null], currentPlayer: 'O', moves: [move(1, 'X', 0)], canUndo: true })),
    );

    (el.querySelectorAll('app-board button')[0] as HTMLButtonElement).click();
    await settle(fixture);

    expect(api.playMove).toHaveBeenCalledWith('game-1', 'X', 0);
    expect(el.querySelector('[data-testid="status-message"]')?.textContent).toContain("Player O's turn");
    expect(el.querySelectorAll('app-move-history tbody tr')).toHaveLength(1);
  });

  it('shows the winner and locks the board when the game is won', async () => {
    const { el } = await render(
      emptyGame({
        status: 'Won',
        winner: 'X',
        currentPlayer: null,
        board: ['X', 'X', 'X', 'O', 'O', null, null, null, null],
        winningCells: [0, 1, 2],
        scoreboard: { xWins: 1, oWins: 0, draws: 0 },
      }),
    );
    expect(el.querySelector('[data-testid="status-message"]')?.textContent).toContain('Player X wins!');
    const cells = Array.from(el.querySelectorAll('app-board button')) as HTMLButtonElement[];
    expect(cells.every((c) => c.disabled)).toBe(true);
    expect(el.querySelectorAll('app-board .winning')).toHaveLength(3);
    expect(el.querySelector('[data-testid="x-wins"]')?.textContent?.trim()).toBe('1');
  });

  it('shows a draw message', async () => {
    const { el } = await render(emptyGame({ status: 'Draw', currentPlayer: null }));
    expect(el.querySelector('[data-testid="status-message"]')?.textContent).toContain("It's a draw!");
  });

  it('disables Undo when the backend says there is nothing to undo, and enables it otherwise', async () => {
    const { fixture, el } = await render();
    expect(buttonByText(el, 'Undo Last Move').disabled).toBe(true);

    api.playMove.mockReturnValue(of(emptyGame({ canUndo: true, currentPlayer: 'O', moves: [move(1, 'X', 0)] })));
    (el.querySelectorAll('app-board button')[0] as HTMLButtonElement).click();
    await settle(fixture);
    expect(buttonByText(el, 'Undo Last Move').disabled).toBe(false);
  });

  it('calls the undo, reset game and reset scoreboard endpoints', async () => {
    const { fixture, el } = await render(emptyGame({ canUndo: true, moves: [move(1, 'X', 0)] }));
    api.undo.mockReturnValue(of(emptyGame()));
    api.resetGame.mockReturnValue(of(emptyGame()));
    api.resetScoreboard.mockReturnValue(of({ xWins: 0, oWins: 0, draws: 0 }));

    buttonByText(el, 'Undo Last Move').click();
    await settle(fixture);
    expect(api.undo).toHaveBeenCalledWith('game-1');

    buttonByText(el, 'Reset Game').click();
    await settle(fixture);
    expect(api.resetGame).toHaveBeenCalledWith('game-1');

    buttonByText(el, 'Reset Scoreboard').click();
    await settle(fixture);
    expect(api.resetScoreboard).toHaveBeenCalled();
  });

  it('starts a new game in Computer mode when the mode is switched', async () => {
    const { fixture, el } = await render();
    api.createGame.mockReturnValue(of(emptyGame({ id: 'game-2', mode: 'VsComputer' })));

    buttonByText(el, 'Play Against Computer').click();
    await settle(fixture);

    expect(api.createGame).toHaveBeenLastCalledWith('VsComputer');
    expect(el.querySelector('[data-testid="mode-label"]')?.textContent).toContain('Play Against Computer');
    expect(el.querySelector('[data-testid="status-message"]')?.textContent).toContain('Your turn');
  });

  it('shows the backend error message when a move is rejected', async () => {
    const { fixture, el } = await render();
    api.playMove.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'CellOccupied', message: 'Cell is already taken.' } })),
    );
    api.getGame.mockReturnValue(of(emptyGame()));

    (el.querySelectorAll('app-board button')[2] as HTMLButtonElement).click();
    await settle(fixture);

    expect(el.querySelector('.alert')?.textContent).toContain('Cell is already taken.');
  });

  it('explains when the backend cannot be reached', async () => {
    api.getGame.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    api.createGame.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await settle(fixture);
    expect(fixture.nativeElement.querySelector('.alert')?.textContent).toContain('Cannot reach the game server');
  });
});
