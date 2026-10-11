namespace TicTacToe.Api.Domain;

public sealed record ScoreSnapshot(int XWins, int OWins, int Draws);

/// <summary>Session-level scoreboard (shared by every game for the lifetime of the API process).</summary>
public sealed class Scoreboard
{
    private readonly object _gate = new();
    private int _xWins;
    private int _oWins;
    private int _draws;

    public ScoreSnapshot Snapshot()
    {
        lock (_gate) return new ScoreSnapshot(_xWins, _oWins, _draws);
    }

    public void Record(GameStatus status, Player? winner)
    {
        lock (_gate)
        {
            switch (status)
            {
                case GameStatus.Won when winner == Player.X: _xWins++; break;
                case GameStatus.Won when winner == Player.O: _oWins++; break;
                case GameStatus.Draw: _draws++; break;
                default: throw new InvalidOperationException("Only completed games can be recorded.");
            }
        }
    }

    public void Reset()
    {
        lock (_gate) _xWins = _oWins = _draws = 0;
    }
}
