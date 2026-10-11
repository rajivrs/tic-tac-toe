import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BoardComponent } from './components/board.component';
import { MoveHistoryComponent } from './components/move-history.component';
import { ScoreboardComponent } from './components/scoreboard.component';
import { ApiErrorBody, GameMode, GameState, Player, Scoreboard } from './models/game.models';
import { GameApiService } from './services/game-api.service';

const SAVED_GAME_KEY = 'tictactoe.gameId';

@Component({
  selector: 'app-root',
  imports: [BoardComponent, MoveHistoryComponent, ScoreboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(GameApiService);

  /** Latest game state returned by the backend (the single source of truth). */
  protected readonly game = signal<GameState | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly boardLocked = computed(() => {
    const g = this.game();
    return this.busy() || g === null || g.status !== 'InProgress';
  });

  /** Player shown in the badge: whose turn it is, or the winner once the game is over. */
  protected readonly badgePlayer = computed<Player | null>(() => {
    const g = this.game();
    if (!g) return null;
    return g.status === 'Won' ? g.winner : g.currentPlayer;
  });

  protected readonly statusMessage = computed(() => {
    const g = this.game();
    if (!g) return '';
    switch (g.status) {
      case 'Won':
        return g.mode === 'VsComputer'
          ? g.winner === 'X' ? 'You win! (Player X)' : 'Computer wins! (Player O)'
          : `Player ${g.winner} wins!`;
      case 'Draw':
        return "It's a draw!";
      default:
        return g.mode === 'VsComputer'
          ? 'Your turn (Player X)'
          : `Player ${g.currentPlayer}'s turn`;
    }
  });

  protected readonly modeLabel = computed(() =>
    this.game()?.mode === 'VsComputer'
      ? 'Play Against Computer — you are X, the computer is O'
      : 'Two Player Mode — X and O take turns',
  );

  async ngOnInit(): Promise<void> {
    await this.run(async () => {
      const savedId = this.readSavedGameId();
      if (savedId) {
        try {
          this.applyGame(await firstValueFrom(this.api.getGame(savedId)));
          return;
        } catch {
          // The backend restarted (in-memory state) or the id is stale: start fresh below.
        }
      }
      this.applyGame(await firstValueFrom(this.api.createGame('TwoPlayer')));
    });
  }

  protected onCellSelected(cellIndex: number): void {
    const g = this.game();
    if (!g || g.currentPlayer === null) return;
    const player = g.currentPlayer;
    void this.run(async () => this.applyGame(await firstValueFrom(this.api.playMove(g.id, player, cellIndex))));
  }

  protected onModeChange(mode: GameMode): void {
    if (this.game()?.mode === mode) return;
    void this.run(async () => this.applyGame(await firstValueFrom(this.api.createGame(mode))));
  }

  protected onResetGame(): void {
    const g = this.game();
    if (!g) return;
    void this.run(async () => this.applyGame(await firstValueFrom(this.api.resetGame(g.id))));
  }

  protected onUndo(): void {
    const g = this.game();
    if (!g || !g.canUndo) return;
    void this.run(async () => this.applyGame(await firstValueFrom(this.api.undo(g.id))));
  }

  protected onResetScoreboard(): void {
    void this.run(async () => this.applyScoreboard(await firstValueFrom(this.api.resetScoreboard())));
  }

  protected dismissError(): void {
    this.error.set(null);
  }

  private applyGame(state: GameState): void {
    this.game.set(state);
    this.saveGameId(state.id);
  }

  private applyScoreboard(scoreboard: Scoreboard): void {
    this.game.update((g) => (g ? { ...g, scoreboard } : g));
  }

  /** Runs an API action with a busy flag (prevents double clicks) and unified error handling. */
  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
    } catch (err) {
      this.error.set(this.describeError(err));
      await this.refreshAfterFailure();
    } finally {
      this.busy.set(false);
    }
  }

  /** After a rejected action, re-fetch so the UI shows exactly what the backend holds. */
  private async refreshAfterFailure(): Promise<void> {
    const g = this.game();
    if (!g) return;
    try {
      this.game.set(await firstValueFrom(this.api.getGame(g.id)));
    } catch {
      // Backend unreachable: keep the last known state and the error message.
    }
  }

  private describeError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) {
        return 'Cannot reach the game server. Make sure the .NET API is running on http://localhost:5080.';
      }
      const body = err.error as Partial<ApiErrorBody> | null;
      if (body?.message) return body.message;
      return `Request failed (${err.status}).`;
    }
    return 'Something went wrong. Please try again.';
  }

  private readSavedGameId(): string | null {
    try {
      return sessionStorage.getItem(SAVED_GAME_KEY);
    } catch {
      return null;
    }
  }

  private saveGameId(id: string): void {
    try {
      sessionStorage.setItem(SAVED_GAME_KEY, id);
    } catch {
      // Storage unavailable (private mode etc.) — resuming after reload is simply skipped.
    }
  }
}
