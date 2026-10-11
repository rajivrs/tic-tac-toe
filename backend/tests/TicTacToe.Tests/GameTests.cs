using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class GameTests
{
    // ---------- Board & valid moves ----------

    [Fact]
    public void NewGame_HasEmptyBoard_AndXToMove()
    {
        var game = TestHelpers.NewGame();

        Assert.Equal(9, game.Board.Count);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Empty(game.Moves);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void ValidMove_PlacesMark_AndRecordsMoveHistory()
    {
        var game = TestHelpers.NewGame();

        game.PlayMove(Player.X, 0);

        Assert.Equal(Player.X, game.Board[0]);
        var move = Assert.Single(game.Moves);
        Assert.Equal(new Move(1, Player.X, 0), move);
    }

    // ---------- Invalid moves ----------

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(100)]
    public void MoveOutsideTheBoard_IsRejected(int cellIndex)
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.X, cellIndex));

        Assert.Equal(GameError.OutOfBounds, ex.Error);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void MoveOnOccupiedCell_IsRejected_AndDoesNotChangeTurn()
    {
        var game = TestHelpers.NewGame();
        game.PlayMove(Player.X, 4);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 4));

        Assert.Equal(GameError.CellOccupied, ex.Error);
        Assert.Equal(Player.X, game.Board[4]);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Single(game.Moves);
    }

    [Fact]
    public void MoveByWrongPlayer_IsRejected_AndDoesNotChangeTurn()
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameError.WrongPlayer, ex.Error);
        Assert.Null(game.Board[0]);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    // ---------- Turn switching ----------

    [Fact]
    public void Turns_AlternateAfterEveryValidMove()
    {
        var game = TestHelpers.NewGame();

        Assert.Equal(Player.X, game.CurrentPlayer);
        game.PlayMove(Player.X, 0);
        Assert.Equal(Player.O, game.CurrentPlayer);
        game.PlayMove(Player.O, 1);
        Assert.Equal(Player.X, game.CurrentPlayer);
        game.PlayMove(Player.X, 2);
        Assert.Equal(Player.O, game.CurrentPlayer);
    }

    // ---------- Win detection ----------

    [Theory]
    [InlineData(new[] { 0, 3, 1, 4, 2 }, new[] { 0, 1, 2 })] // top row
    [InlineData(new[] { 3, 0, 4, 1, 5 }, new[] { 3, 4, 5 })] // middle row
    [InlineData(new[] { 6, 0, 7, 1, 8 }, new[] { 6, 7, 8 })] // bottom row
    public void RowWin_IsDetected(int[] moves, int[] expectedLine) => AssertXWins(moves, expectedLine);

    [Theory]
    [InlineData(new[] { 0, 1, 3, 2, 6 }, new[] { 0, 3, 6 })] // left column
    [InlineData(new[] { 1, 0, 4, 2, 7 }, new[] { 1, 4, 7 })] // middle column
    [InlineData(new[] { 2, 0, 5, 1, 8 }, new[] { 2, 5, 8 })] // right column
    public void ColumnWin_IsDetected(int[] moves, int[] expectedLine) => AssertXWins(moves, expectedLine);

    [Theory]
    [InlineData(new[] { 0, 1, 4, 2, 8 }, new[] { 0, 4, 8 })] // main diagonal
    [InlineData(new[] { 2, 0, 4, 1, 6 }, new[] { 2, 4, 6 })] // anti-diagonal
    public void DiagonalWin_IsDetected(int[] moves, int[] expectedLine) => AssertXWins(moves, expectedLine);

    [Fact]
    public void PlayerOCanWin_AndWinningCellsAreReported()
    {
        var game = TestHelpers.NewGame().PlayAlternating(TestHelpers.OWinsMiddleRow);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(new[] { 3, 4, 5 }, game.WinningCells);
    }

    [Fact]
    public void WhenWon_CurrentPlayerIsCleared_AndFurtherMovesAreRejected()
    {
        var game = TestHelpers.NewGame().PlayAlternating(TestHelpers.XWinsTopRow);

        Assert.Null(game.CurrentPlayer);
        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 8));

        Assert.Equal(GameError.GameOver, ex.Error);
        Assert.Null(game.Board[8]);
        Assert.Equal(5, game.Moves.Count);
    }

    // ---------- Draw ----------

    [Fact]
    public void FullBoardWithoutWinner_IsADraw()
    {
        var game = TestHelpers.NewGame().PlayAlternating(TestHelpers.Draw);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Null(game.CurrentPlayer);
        Assert.Equal(9, game.Moves.Count);
    }

    [Fact]
    public void MoveAfterDraw_IsRejected()
    {
        var game = TestHelpers.NewGame().PlayAlternating(TestHelpers.Draw);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameError.GameOver, ex.Error);
    }

    // ---------- Reset ----------

    [Fact]
    public void Reset_ClearsBoardHistoryAndStatus_AndGivesTurnBackToX()
    {
        var game = TestHelpers.NewGame().PlayAlternating(TestHelpers.XWinsTopRow);
        var id = game.Id;

        game.Reset();

        Assert.Equal(id, game.Id);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.False(game.ResultRecorded);
    }

    [Fact]
    public void Reset_MidGame_LetsXStartAgain()
    {
        var game = TestHelpers.NewGame().PlayAlternating(0, 1, 2);

        game.Reset();
        game.PlayMove(Player.X, 4);

        Assert.Equal(Player.X, game.Board[4]);
        Assert.Single(game.Moves);
    }

    [Fact]
    public void Reset_KeepsTheGameMode()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);
        game.PlayMove(Player.X, 0);

        game.Reset();

        Assert.Equal(GameMode.VsComputer, game.Mode);
    }

    // ---------- Undo: Two Player mode ----------

    [Fact]
    public void Undo_TwoPlayer_RemovesOnlyTheMostRecentMove_AndRestoresTurn()
    {
        var game = TestHelpers.NewGame().PlayAlternating(0, 4); // X0, O4

        game.Undo();

        Assert.Null(game.Board[4]);
        Assert.Equal(Player.X, game.Board[0]);
        Assert.Equal(Player.O, game.CurrentPlayer); // it is O's turn again
        var remaining = Assert.Single(game.Moves);
        Assert.Equal(1, remaining.MoveNumber);
    }

    [Fact]
    public void Undo_TwoPlayer_KeepsMoveHistoryAccurate_WhenRepeated()
    {
        var game = TestHelpers.NewGame().PlayAlternating(0, 1, 2, 3);

        game.Undo();
        game.Undo();

        Assert.Equal(new[] { 1, 2 }, game.Moves.Select(m => m.MoveNumber));
        Assert.Equal(new[] { 0, 1 }, game.Moves.Select(m => m.CellIndex));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.True(game.CanUndo);
    }

    [Fact]
    public void Undo_IsRejected_WhenThereAreNoMoves()
    {
        var game = TestHelpers.NewGame();

        var ex = Assert.Throws<GameRuleException>(() => game.Undo());

        Assert.False(game.CanUndo);
        Assert.Equal(GameError.NothingToUndo, ex.Error);
    }

    [Fact]
    public void Undo_IsDisabled_AfterTheGameIsWonOrDrawn()
    {
        var won = TestHelpers.NewGame().PlayAlternating(TestHelpers.XWinsTopRow);
        var drawn = TestHelpers.NewGame().PlayAlternating(TestHelpers.Draw);

        Assert.False(won.CanUndo);
        Assert.False(drawn.CanUndo);
        Assert.Equal(GameError.UndoNotAllowed, Assert.Throws<GameRuleException>(() => won.Undo()).Error);
        Assert.Equal(GameError.UndoNotAllowed, Assert.Throws<GameRuleException>(() => drawn.Undo()).Error);
        Assert.Equal(GameStatus.Won, won.Status); // untouched
        Assert.Equal(5, won.Moves.Count);
    }

    // ---------- Computer mode ----------

    [Fact]
    public void ComputerMode_ComputerRepliesAutomatically_AfterTheHumanMove()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);

        game.PlayMove(Player.X, 0);

        Assert.Equal(2, game.Moves.Count);
        Assert.Equal(Player.X, game.Moves[0].Player);
        Assert.Equal(Player.O, game.Moves[1].Player);
        Assert.Equal(Player.O, game.Board[4]); // center is the computer's first choice here
        Assert.Equal(Player.X, game.CurrentPlayer); // back to the human
    }

    [Fact]
    public void ComputerMode_HumanCannotPlayForTheComputer()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameError.WrongPlayer, ex.Error);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void ComputerMode_ComputerOnlyMakesValidMoves_ThroughAWholeGame()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);

        // The human always takes the first free cell; every computer reply must land on a free cell.
        while (!game.IsCompleted)
        {
            var freeCell = Enumerable.Range(0, 9).First(i => game.Board[i] is null);
            game.PlayMove(Player.X, freeCell);
        }

        var cells = game.Moves.Select(m => m.CellIndex).ToArray();
        Assert.Equal(cells.Length, cells.Distinct().Count()); // no cell was ever used twice
    }

    [Fact]
    public void ComputerMode_ComputerWins_WhenTheHumanIgnoresItsThreat()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);

        foreach (var cell in TestHelpers.HumanMovesThatLoseVsComputer)
            game.PlayMove(Player.X, cell);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(new[] { 2, 4, 6 }, game.WinningCells);
    }

    [Fact]
    public void ComputerMode_ComputerDoesNotMove_AfterTheHumanWins()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);

        foreach (var cell in TestHelpers.HumanMovesThatWinVsComputer)
            game.PlayMove(Player.X, cell);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(7, game.Moves.Count); // X, O, X, O, X, O, X - the last move is the human's winning move
        Assert.Equal(Player.X, game.Moves[^1].Player);
    }

    // ---------- Undo: Computer mode ----------

    [Fact]
    public void Undo_ComputerMode_RemovesTheComputerMoveAndTheHumanMoveBeforeIt()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);
        game.PlayMove(Player.X, 0); // X0, then the computer answers O4

        game.Undo();

        Assert.Empty(game.Moves);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Undo_ComputerMode_OnlyRemovesTheLatestPair()
    {
        var game = TestHelpers.NewGame(GameMode.VsComputer);
        game.PlayMove(Player.X, 0); // X0, O4
        game.PlayMove(Player.X, 1); // X1, O2 (blocks)
        Assert.Equal(4, game.Moves.Count);

        game.Undo();

        Assert.Equal(new[] { 0, 4 }, game.Moves.Select(m => m.CellIndex));
        Assert.Equal(new[] { 1, 2 }, game.Moves.Select(m => m.MoveNumber));
        Assert.Null(game.Board[1]);
        Assert.Null(game.Board[2]);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    // ---------- Helpers ----------

    private static void AssertXWins(int[] moves, int[] expectedLine)
    {
        var game = TestHelpers.NewGame().PlayAlternating(moves);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(expectedLine, game.WinningCells);
    }
}
