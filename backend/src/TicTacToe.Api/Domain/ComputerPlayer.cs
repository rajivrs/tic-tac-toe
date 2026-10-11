namespace TicTacToe.Api.Domain;

/// <summary>
/// Basic, deterministic computer strategy. Priority (from the requirements):
/// 1) win, 2) block the opponent's win, 3) center, 4) a corner, 5) any free cell.
/// </summary>
public static class ComputerPlayer
{
    private const int Center = 4;
    private static readonly int[] Corners = { 0, 2, 6, 8 };

    /// <returns>The chosen cell index (0-8). Throws if the board has no free cell.</returns>
    public static int ChooseMove(IReadOnlyList<Player?> board, Player computer)
    {
        var winningMove = FindCompletingMove(board, computer);
        if (winningMove is not null) return winningMove.Value;

        var blockingMove = FindCompletingMove(board, computer.Opponent());
        if (blockingMove is not null) return blockingMove.Value;

        if (board[Center] is null) return Center;

        foreach (var corner in Corners)
            if (board[corner] is null) return corner;

        for (var i = 0; i < board.Count; i++)
            if (board[i] is null) return i;

        throw new InvalidOperationException("The board has no free cell.");
    }

    /// <summary>Finds a free cell that would complete a line for <paramref name="player"/> (two of theirs + one empty).</summary>
    private static int? FindCompletingMove(IReadOnlyList<Player?> board, Player player)
    {
        foreach (var line in Game.WinningLines)
        {
            var owned = 0;
            var empty = -1;
            foreach (var cell in line)
            {
                if (board[cell] == player) owned++;
                else if (board[cell] is null) empty = cell;
            }

            if (owned == 2 && empty >= 0) return empty;
        }

        return null;
    }
}
