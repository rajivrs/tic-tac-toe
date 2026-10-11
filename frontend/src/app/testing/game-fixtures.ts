import { GameState, MoveEntry, Player } from '../models/game.models';

export function emptyGame(overrides: Partial<GameState> = {}): GameState {
  return {
    id: 'game-1',
    mode: 'TwoPlayer',
    status: 'InProgress',
    currentPlayer: 'X',
    winner: null,
    board: Array<Player | null>(9).fill(null),
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

export function move(moveNumber: number, player: Player, cellIndex: number): MoveEntry {
  const row = Math.floor(cellIndex / 3) + 1;
  const column = (cellIndex % 3) + 1;
  return { moveNumber, player, cellIndex, row, column, position: `Row ${row}, Column ${column}` };
}
