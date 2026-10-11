using System.ComponentModel.DataAnnotations;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Contracts;

/// <summary>Body of POST /api/games. The mode defaults to TwoPlayer when omitted.</summary>
public sealed class CreateGameRequest
{
    public GameMode Mode { get; init; } = GameMode.TwoPlayer;
}

/// <summary>Body of POST /api/games/{id}/moves. The game id comes from the route.</summary>
public sealed class MoveRequest
{
    /// <summary>"X" or "O".</summary>
    [Required]
    public Player? Player { get; init; }

    /// <summary>Cell index 0-8, row-major (0 = top-left, 8 = bottom-right).</summary>
    [Required]
    public int? CellIndex { get; init; }
}

public sealed record MoveDto(int MoveNumber, Player Player, int CellIndex, int Row, int Column, string Position);

/// <summary>Everything the frontend needs to render a game.</summary>
public sealed record GameStateResponse(
    Guid Id,
    GameMode Mode,
    GameStatus Status,
    Player? CurrentPlayer,
    Player? Winner,
    IReadOnlyList<Player?> Board,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveDto> Moves,
    bool CanUndo,
    ScoreSnapshot Scoreboard)
{
    public static GameStateResponse From(Game game, ScoreSnapshot scoreboard) => new(
        game.Id,
        game.Mode,
        game.Status,
        game.CurrentPlayer,
        game.Winner,
        game.Board.ToArray(),
        game.WinningCells.ToArray(),
        game.Moves.Select(ToDto).ToArray(),
        game.CanUndo,
        scoreboard);

    private static MoveDto ToDto(Move move)
    {
        var row = move.CellIndex / 3 + 1;    // 1-based for display
        var column = move.CellIndex % 3 + 1; // 1-based for display
        return new MoveDto(move.MoveNumber, move.Player, move.CellIndex, row, column, $"Row {row}, Column {column}");
    }
}

/// <summary>Error body returned for every rejected request (4xx).</summary>
public sealed record ApiError(string Code, string Message);
