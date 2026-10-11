/** Contract types mirroring the .NET API responses (see README -> API reference). */

export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveEntry {
  moveNumber: number;
  player: Player;
  /** 0-8, row-major (0 = top-left, 8 = bottom-right). */
  cellIndex: number;
  /** 1-based, for display. */
  row: number;
  /** 1-based, for display. */
  column: number;
  /** Human readable, e.g. "Row 1, Column 1". */
  position: string;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  id: string;
  mode: GameMode;
  status: GameStatus;
  /** null once the game is completed. */
  currentPlayer: Player | null;
  winner: Player | null;
  /** 9 cells, row-major; null = empty. */
  board: (Player | null)[];
  winningCells: number[];
  moves: MoveEntry[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}

export interface ApiErrorBody {
  code: string;
  message: string;
}
