using TicTacToe.Api.Domain;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

/// <summary>Computer (O) move priority: win, block, center, corner, any.</summary>
public class ComputerPlayerTests
{
    [Fact]
    public void Priority1_PlaysTheWinningMove_EvenWhenTheOpponentAlsoThreatens()
    {
        // O can win at 5; X threatens to win at 2. Winning beats blocking.
        Assert.Equal(5, ComputerPlayer.ChooseMove(Board("XX.OO...."), Player.O));
    }

    [Fact]
    public void Priority2_BlocksTheOpponentsWinningMove()
    {
        Assert.Equal(2, ComputerPlayer.ChooseMove(Board("XX..O...."), Player.O));
    }

    [Fact]
    public void Priority2_BlocksAColumnAndADiagonalToo()
    {
        Assert.Equal(6, ComputerPlayer.ChooseMove(Board("X..X.O..."), Player.O)); // column 0
        Assert.Equal(8, ComputerPlayer.ChooseMove(Board("X...X...."), Player.O)); // diagonal 0-4-8
    }

    [Fact]
    public void Priority3_TakesTheCenter_WhenThereIsNothingToWinOrBlock()
    {
        Assert.Equal(4, ComputerPlayer.ChooseMove(Board("X........"), Player.O));
    }

    [Fact]
    public void Priority4_TakesACorner_WhenTheCenterIsTaken()
    {
        Assert.Equal(0, ComputerPlayer.ChooseMove(Board("....X...."), Player.O));
    }

    [Fact]
    public void Priority4_SkipsOccupiedCorners()
    {
        Assert.Equal(2, ComputerPlayer.ChooseMove(Board("X...O...."), Player.O));
    }

    [Fact]
    public void Priority5_TakesAnyFreeCell_WhenCenterAndCornersAreGone()
    {
        // Only the edge cells 3 and 5 are free; nobody can win or be blocked.
        Assert.Equal(3, ComputerPlayer.ChooseMove(Board("XOX.X.OXO"), Player.O));
    }

    [Fact]
    public void AlwaysChoosesAFreeCell()
    {
        var board = Board("XOXOXOOX.");
        Assert.Equal(8, ComputerPlayer.ChooseMove(board, Player.O));
    }

    [Fact]
    public void ThrowsWhenTheBoardIsFull()
    {
        Assert.Throws<InvalidOperationException>(() => ComputerPlayer.ChooseMove(Board("XOXXOOOXX"), Player.O));
    }
}
