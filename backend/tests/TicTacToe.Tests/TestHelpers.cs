using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

namespace TicTacToe.Tests;

internal static class TestHelpers
{
    public static Game NewGame(GameMode mode = GameMode.TwoPlayer) => new(Guid.NewGuid(), mode);

    /// <summary>Plays the given cells in order, letting whoever is current move (X first, then O, ...).</summary>
    public static Game PlayAlternating(this Game game, params int[] cells)
    {
        foreach (var cell in cells)
            game.PlayMove(game.CurrentPlayer!.Value, cell);
        return game;
    }

    /// <summary>Same as <see cref="PlayAlternating(Game, int[])"/> but through the service layer.</summary>
    public static GameStateResponse PlayAlternating(this IGameService service, Guid gameId, params int[] cells)
    {
        var state = service.GetGame(gameId);
        foreach (var cell in cells)
            state = service.PlayMove(gameId, state.CurrentPlayer!.Value, cell);
        return state;
    }

    /// <summary>Builds a board from a 9-character string: 'X', 'O' or '.' for empty (row-major).</summary>
    public static Player?[] Board(string cells)
    {
        Assert.Equal(9, cells.Length);
        return cells.Select(c => c switch
        {
            'X' => (Player?)Player.X,
            'O' => Player.O,
            '.' => null,
            _ => throw new ArgumentException($"Unexpected board character '{c}'.")
        }).ToArray();
    }

    // Move sequences (cell indexes, X first, alternating) used across several test classes.
    public static readonly int[] XWinsTopRow = { 0, 3, 1, 4, 2 };
    public static readonly int[] OWinsMiddleRow = { 0, 3, 1, 4, 8, 5 };
    public static readonly int[] Draw = { 0, 1, 2, 4, 3, 5, 7, 6, 8 };

    /// <summary>Human (X) moves that beat the deterministic computer: it answers 4, 2, 7.</summary>
    public static readonly int[] HumanMovesThatWinVsComputer = { 0, 8, 6, 3 };

    /// <summary>Human (X) moves that lose to the computer: it answers 4, 2, 6 (O wins on 2-4-6).</summary>
    public static readonly int[] HumanMovesThatLoseVsComputer = { 0, 1, 8 };
}
