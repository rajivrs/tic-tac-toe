using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Services;

public interface IGameService
{
    GameStateResponse CreateGame(GameMode mode);
    GameStateResponse GetGame(Guid id);
    GameStateResponse PlayMove(Guid id, Player player, int cellIndex);
    GameStateResponse Undo(Guid id);
    GameStateResponse Reset(Guid id);
    ScoreSnapshot GetScoreboard();
    ScoreSnapshot ResetScoreboard();
}

/// <summary>
/// Owns all in-memory state (games + the session scoreboard) and is registered as a singleton.
/// A single lock keeps every operation atomic; that is plenty for a local, single-user app.
/// </summary>
public sealed class GameService : IGameService
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Game> _games = new();
    private readonly Scoreboard _scoreboard = new();

    public GameStateResponse CreateGame(GameMode mode)
    {
        lock (_gate)
        {
            var game = new Game(Guid.NewGuid(), mode);
            _games[game.Id] = game;
            return Snapshot(game);
        }
    }

    public GameStateResponse GetGame(Guid id)
    {
        lock (_gate) return Snapshot(Find(id));
    }

    public GameStateResponse PlayMove(Guid id, Player player, int cellIndex)
    {
        lock (_gate)
        {
            var game = Find(id);
            game.PlayMove(player, cellIndex);
            RecordResultOnce(game);
            return Snapshot(game);
        }
    }

    public GameStateResponse Undo(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            game.Undo();
            return Snapshot(game);
        }
    }

    public GameStateResponse Reset(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            game.Reset(); // the scoreboard is intentionally left untouched
            return Snapshot(game);
        }
    }

    public ScoreSnapshot GetScoreboard()
    {
        lock (_gate) return _scoreboard.Snapshot();
    }

    public ScoreSnapshot ResetScoreboard()
    {
        lock (_gate)
        {
            _scoreboard.Reset();
            return _scoreboard.Snapshot();
        }
    }

    /// <summary>Adds a finished game's result to the scoreboard exactly once.</summary>
    private void RecordResultOnce(Game game)
    {
        if (!game.IsCompleted || game.ResultRecorded) return;

        _scoreboard.Record(game.Status, game.Winner);
        game.MarkResultRecorded();
    }

    private Game Find(Guid id) =>
        _games.TryGetValue(id, out var game) ? game : throw new GameNotFoundException(id);

    private GameStateResponse Snapshot(Game game) => GameStateResponse.From(game, _scoreboard.Snapshot());
}
