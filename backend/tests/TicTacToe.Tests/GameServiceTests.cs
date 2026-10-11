using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

namespace TicTacToe.Tests;

/// <summary>State ownership: games, move history in responses and the session scoreboard.</summary>
public class GameServiceTests
{
    private readonly GameService _service = new();

    [Fact]
    public void CreateGame_ReturnsAFreshGameState()
    {
        var state = _service.CreateGame(GameMode.VsComputer);

        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal(GameMode.VsComputer, state.Mode);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Null(state.Winner);
        Assert.Equal(9, state.Board.Count);
        Assert.All(state.Board, cell => Assert.Null(cell));
        Assert.Empty(state.WinningCells);
        Assert.Empty(state.Moves);
        Assert.False(state.CanUndo);
        Assert.Equal(new ScoreSnapshot(0, 0, 0), state.Scoreboard);
    }

    [Fact]
    public void GetGame_WithUnknownId_Throws()
    {
        Assert.Throws<GameNotFoundException>(() => _service.GetGame(Guid.NewGuid()));
    }

    [Fact]
    public void Response_ContainsMoveHistory_WithMoveNumberPlayerAndPosition()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        var state = _service.PlayAlternating(id, 0, 4, 5);

        Assert.Collection(state.Moves,
            m => { Assert.Equal(1, m.MoveNumber); Assert.Equal(Player.X, m.Player); Assert.Equal("Row 1, Column 1", m.Position); },
            m => { Assert.Equal(2, m.MoveNumber); Assert.Equal(Player.O, m.Player); Assert.Equal("Row 2, Column 2", m.Position); },
            m => { Assert.Equal(3, m.MoveNumber); Assert.Equal(Player.X, m.Player); Assert.Equal((2, 3), (m.Row, m.Column)); });
        Assert.True(state.CanUndo);
    }

    // ---------- Scoreboard ----------

    [Fact]
    public void Scoreboard_CountsAnXWin()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        var state = _service.PlayAlternating(id, TestHelpers.XWinsTopRow);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), state.Scoreboard);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void Scoreboard_CountsAnOWin()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        _service.PlayAlternating(id, TestHelpers.OWinsMiddleRow);

        Assert.Equal(new ScoreSnapshot(0, 1, 0), _service.GetScoreboard());
    }

    [Fact]
    public void Scoreboard_CountsADraw()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        var state = _service.PlayAlternating(id, TestHelpers.Draw);

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Equal(new ScoreSnapshot(0, 0, 1), _service.GetScoreboard());
    }

    [Fact]
    public void Scoreboard_IsUpdatedOnlyOnce_ForACompletedGame()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.PlayAlternating(id, TestHelpers.XWinsTopRow);

        // Reading the game, rejected moves and a rejected undo must not count the result again.
        _service.GetGame(id);
        _service.GetGame(id);
        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 8));
        Assert.Throws<GameRuleException>(() => _service.Undo(id));

        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void ResetGame_KeepsTheScoreboard_AndTheNextCompletedGameIsCountedAgain()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.PlayAlternating(id, TestHelpers.XWinsTopRow);

        var afterReset = _service.Reset(id);

        Assert.Equal(new ScoreSnapshot(1, 0, 0), afterReset.Scoreboard); // unchanged by Reset Game
        Assert.Equal(GameStatus.InProgress, afterReset.Status);
        Assert.Empty(afterReset.Moves);

        _service.PlayAlternating(id, TestHelpers.XWinsTopRow);
        Assert.Equal(new ScoreSnapshot(2, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void ResetScoreboard_ClearsAllCounts_WithoutReopeningFinishedGames()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.PlayAlternating(id, TestHelpers.XWinsTopRow);

        var score = _service.ResetScoreboard();

        Assert.Equal(new ScoreSnapshot(0, 0, 0), score);
        Assert.Equal(GameStatus.Won, _service.GetGame(id).Status); // the game itself is untouched
        Assert.Equal(new ScoreSnapshot(0, 0, 0), _service.GetScoreboard()); // and is not re-counted
    }

    [Fact]
    public void Scoreboard_IsSharedAcrossGames_InTheSession()
    {
        var first = _service.CreateGame(GameMode.TwoPlayer).Id;
        var second = _service.CreateGame(GameMode.TwoPlayer).Id;

        _service.PlayAlternating(first, TestHelpers.XWinsTopRow);
        _service.PlayAlternating(second, TestHelpers.OWinsMiddleRow);

        Assert.Equal(new ScoreSnapshot(1, 1, 0), _service.GetScoreboard());
        Assert.Equal(new ScoreSnapshot(1, 1, 0), _service.GetGame(first).Scoreboard);
    }

    [Fact]
    public void Scoreboard_CountsComputerModeResults()
    {
        var humanWins = _service.CreateGame(GameMode.VsComputer).Id;
        foreach (var cell in TestHelpers.HumanMovesThatWinVsComputer)
            _service.PlayMove(humanWins, Player.X, cell);

        var computerWins = _service.CreateGame(GameMode.VsComputer).Id;
        foreach (var cell in TestHelpers.HumanMovesThatLoseVsComputer)
            _service.PlayMove(computerWins, Player.X, cell);

        Assert.Equal(new ScoreSnapshot(1, 1, 0), _service.GetScoreboard());
    }

    // ---------- Rejected requests leave state untouched ----------

    [Fact]
    public void RejectedMove_DoesNotChangeTheGameOrTheScoreboard()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.PlayMove(id, Player.X, 0);
        var before = _service.GetGame(id);

        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 0)); // occupied
        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.X, 5)); // wrong player
        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 42)); // outside the board

        var after = _service.GetGame(id);
        Assert.Equal(before.Board, after.Board);
        Assert.Equal(before.CurrentPlayer, after.CurrentPlayer);
        Assert.Equal(before.Moves, after.Moves);
        Assert.Equal(new ScoreSnapshot(0, 0, 0), after.Scoreboard);
    }

    // ---------- Undo through the service ----------

    [Fact]
    public void Undo_ReturnsTheRestoredState_ForTwoPlayerAndComputerGames()
    {
        var twoPlayer = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.PlayAlternating(twoPlayer, 0, 4);
        var afterTwoPlayerUndo = _service.Undo(twoPlayer);
        Assert.Single(afterTwoPlayerUndo.Moves);
        Assert.Equal(Player.O, afterTwoPlayerUndo.CurrentPlayer);

        var vsComputer = _service.CreateGame(GameMode.VsComputer).Id;
        _service.PlayMove(vsComputer, Player.X, 0);
        var afterComputerUndo = _service.Undo(vsComputer);
        Assert.Empty(afterComputerUndo.Moves);
        Assert.Equal(Player.X, afterComputerUndo.CurrentPlayer);
        Assert.False(afterComputerUndo.CanUndo);
    }
}
