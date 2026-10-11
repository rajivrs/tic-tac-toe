namespace TicTacToe.Api.Domain;

public enum GameError
{
    OutOfBounds,
    CellOccupied,
    GameOver,
    WrongPlayer,
    NothingToUndo,
    UndoNotAllowed
}

/// <summary>Thrown when a request breaks a game rule. Mapped to an HTTP 4xx by the API layer.</summary>
public sealed class GameRuleException : Exception
{
    public GameRuleException(GameError error, string message) : base(message) => Error = error;

    public GameError Error { get; }
}

public sealed class GameNotFoundException : Exception
{
    public GameNotFoundException(Guid id) : base($"Game '{id}' was not found.") { }
}
