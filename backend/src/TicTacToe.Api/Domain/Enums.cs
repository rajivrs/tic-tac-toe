namespace TicTacToe.Api.Domain;

public enum Player
{
    X,
    O
}

public enum GameMode
{
    /// <summary>Both X and O are controlled by people.</summary>
    TwoPlayer,

    /// <summary>The human is X, the computer is O.</summary>
    VsComputer
}

public enum GameStatus
{
    InProgress,
    Won,
    Draw
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player) => player == Player.X ? Player.O : Player.X;
}
