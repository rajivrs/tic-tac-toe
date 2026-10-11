namespace TicTacToe.Api.Domain;

/// <summary>A single move. <see cref="CellIndex"/> is 0-8, row-major (0 = top-left, 8 = bottom-right).</summary>
public sealed record Move(int MoveNumber, Player Player, int CellIndex);
