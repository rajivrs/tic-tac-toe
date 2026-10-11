namespace TicTacToe.Api.Domain;

/// <summary>
/// One Tic Tac Toe game. The move list is the source of truth: board, current player, status,
/// winner and winning cells are always (re)derived from it, which keeps Undo trivially correct.
/// </summary>
public sealed class Game
{
    public const int BoardSize = 9;

    /// <summary>All 8 winning lines as cell indexes (3 rows, 3 columns, 2 diagonals).</summary>
    internal static readonly int[][] WinningLines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
    };

    private readonly Player?[] _board = new Player?[BoardSize];
    private readonly List<Move> _moves = new();

    public Game(Guid id, GameMode mode)
    {
        Id = id;
        Mode = mode;
        Recalculate();
    }

    public Guid Id { get; }
    public GameMode Mode { get; }

    public IReadOnlyList<Player?> Board => _board;
    public IReadOnlyList<Move> Moves => _moves;

    /// <summary>Whose turn it is; null once the game is completed.</summary>
    public Player? CurrentPlayer { get; private set; }

    public GameStatus Status { get; private set; }
    public Player? Winner { get; private set; }
    public IReadOnlyList<int> WinningCells { get; private set; } = Array.Empty<int>();

    public bool IsCompleted => Status != GameStatus.InProgress;

    /// <summary>
    /// Clarification 2, Option A: Undo is disabled once a game is won or drawn,
    /// so a recorded scoreboard result can never need to be reversed.
    /// </summary>
    public bool CanUndo => !IsCompleted && _moves.Count > 0;

    /// <summary>True once this game's result has been added to the scoreboard (guards against double counting).</summary>
    public bool ResultRecorded { get; private set; }

    /// <summary>
    /// Plays a human move. In <see cref="GameMode.VsComputer"/> the computer (O) replies immediately
    /// when the game is still in progress.
    /// </summary>
    public void PlayMove(Player player, int cellIndex)
    {
        if (IsCompleted)
            throw new GameRuleException(GameError.GameOver, "The game is already over. Reset the game to play again.");

        if (cellIndex < 0 || cellIndex >= BoardSize)
            throw new GameRuleException(GameError.OutOfBounds, $"Cell index must be between 0 and {BoardSize - 1}.");

        if (Mode == GameMode.VsComputer && player == Player.O)
            throw new GameRuleException(GameError.WrongPlayer, "Player O is controlled by the computer in this mode.");

        if (player != CurrentPlayer)
            throw new GameRuleException(GameError.WrongPlayer, $"It is Player {CurrentPlayer}'s turn.");

        if (_board[cellIndex] is not null)
            throw new GameRuleException(GameError.CellOccupied, "That cell is already taken.");

        Apply(player, cellIndex);

        if (Mode == GameMode.VsComputer && !IsCompleted && CurrentPlayer == Player.O)
            Apply(Player.O, ComputerPlayer.ChooseMove(_board, Player.O));
    }

    /// <summary>
    /// Two Player: removes the most recent move.
    /// Computer mode: removes the computer's last move together with the human move before it.
    /// </summary>
    public void Undo()
    {
        if (_moves.Count == 0)
            throw new GameRuleException(GameError.NothingToUndo, "There are no moves to undo.");

        if (IsCompleted)
            throw new GameRuleException(GameError.UndoNotAllowed, "Undo is not available after the game has finished. Reset the game to play again.");

        var removed = RemoveLastMove();
        if (Mode == GameMode.VsComputer && removed.Player == Player.O && _moves.Count > 0)
            RemoveLastMove();

        Recalculate();
    }

    /// <summary>Clears board, history and status and gives the turn back to X. Mode and Id are kept.</summary>
    public void Reset()
    {
        _moves.Clear();
        Array.Clear(_board);
        ResultRecorded = false;
        Recalculate();
    }

    public void MarkResultRecorded()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Only a completed game can have its result recorded.");
        ResultRecorded = true;
    }

    private void Apply(Player player, int cellIndex)
    {
        _board[cellIndex] = player;
        _moves.Add(new Move(_moves.Count + 1, player, cellIndex));
        Recalculate();
    }

    private Move RemoveLastMove()
    {
        var last = _moves[^1];
        _moves.RemoveAt(_moves.Count - 1);
        _board[last.CellIndex] = null;
        return last;
    }

    /// <summary>Derives status, winner, winning cells and current player from the board and move count.</summary>
    private void Recalculate()
    {
        Winner = null;
        WinningCells = Array.Empty<int>();

        foreach (var line in WinningLines)
        {
            var first = _board[line[0]];
            if (first is not null && first == _board[line[1]] && first == _board[line[2]])
            {
                Winner = first;
                WinningCells = line;
                Status = GameStatus.Won;
                CurrentPlayer = null;
                return;
            }
        }

        if (_moves.Count == BoardSize)
        {
            Status = GameStatus.Draw;
            CurrentPlayer = null;
            return;
        }

        Status = GameStatus.InProgress;
        CurrentPlayer = _moves.Count % 2 == 0 ? Player.X : Player.O; // X always starts
    }
}
