using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

/// <summary>End-to-end checks of the REST contract against the real ASP.NET pipeline (in-memory server).</summary>
public sealed class ApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // A new factory (= new in-memory state) per test keeps tests independent.
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PostGames_CreatesAGame_AndGetReturnsTheSameState()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "VsComputer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<GameStateResponse>(response);
        Assert.Equal(GameMode.VsComputer, created.Mode);
        Assert.Equal(GameStatus.InProgress, created.Status);
        Assert.Equal(Player.X, created.CurrentPlayer);

        var fetched = await ReadAsync<GameStateResponse>(await _client.GetAsync($"/api/games/{created.Id}"));
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Board, fetched.Board);
    }

    [Fact]
    public async Task PostGames_WithoutAMode_DefaultsToTwoPlayer()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(GameMode.TwoPlayer, (await ReadAsync<GameStateResponse>(response)).Mode);
    }

    [Fact]
    public async Task PostMove_UpdatesBoardTurnAndHistory()
    {
        var game = await CreateAsync();

        var state = await ReadAsync<GameStateResponse>(await MoveAsync(game.Id, "X", 4));

        Assert.Equal(Player.X, state.Board[4]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        var move = Assert.Single(state.Moves);
        Assert.Equal("Row 2, Column 2", move.Position);
    }

    [Fact]
    public async Task PostMove_OnOccupiedCell_Returns409()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, "X", 4);

        var response = await MoveAsync(game.Id, "O", 4);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CellOccupied", (await ReadAsync<ApiError>(response)).Code);
    }

    [Fact]
    public async Task PostMove_OutsideTheBoard_Returns400()
    {
        var game = await CreateAsync();

        var response = await MoveAsync(game.Id, "X", 9);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("OutOfBounds", (await ReadAsync<ApiError>(response)).Code);
    }

    [Fact]
    public async Task PostMove_ByTheWrongPlayer_Returns409()
    {
        var game = await CreateAsync();

        var response = await MoveAsync(game.Id, "O", 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("WrongPlayer", (await ReadAsync<ApiError>(response)).Code);
    }

    [Fact]
    public async Task PostMove_WithMissingFields_Returns400()
    {
        var game = await CreateAsync();

        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownGame_Returns404()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("GameNotFound", (await ReadAsync<ApiError>(response)).Code);
    }

    [Fact]
    public async Task Undo_RestoresThePreviousState_AndFailsWhenNothingIsLeft()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, "X", 0);

        var undone = await ReadAsync<GameStateResponse>(await _client.PostAsync($"/api/games/{game.Id}/undo", null));
        Assert.Empty(undone.Moves);
        Assert.False(undone.CanUndo);

        var again = await _client.PostAsync($"/api/games/{game.Id}/undo", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("NothingToUndo", (await ReadAsync<ApiError>(again)).Code);
    }

    [Fact]
    public async Task ComputerMode_ResponseAlreadyContainsTheComputersReply()
    {
        var game = await CreateAsync(GameMode.VsComputer);

        var state = await ReadAsync<GameStateResponse>(await MoveAsync(game.Id, "X", 0));

        Assert.Equal(2, state.Moves.Count);
        Assert.Equal(Player.O, state.Moves[1].Player);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task Scoreboard_IsServedByTheBackend_UpdatesOnWin_AndCanBeReset()
    {
        var game = await CreateAsync();
        GameStateResponse state = game;
        foreach (var cell in TestHelpers.XWinsTopRow)
            state = await ReadAsync<GameStateResponse>(await MoveAsync(game.Id, state.CurrentPlayer!.Value.ToString(), cell));

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(new[] { 0, 1, 2 }, state.WinningCells);

        var score = await ReadAsync<ScoreSnapshot>(await _client.GetAsync("/api/scoreboard"));
        Assert.Equal(new ScoreSnapshot(1, 0, 0), score);

        var reset = await _client.PostAsync("/api/scoreboard/reset", null);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(new ScoreSnapshot(0, 0, 0), await ReadAsync<ScoreSnapshot>(reset));
    }

    [Fact]
    public async Task ResetEndpoint_ClearsTheGame_ButNotTheScoreboard()
    {
        var game = await CreateAsync();
        GameStateResponse state = game;
        foreach (var cell in TestHelpers.XWinsTopRow)
            state = await ReadAsync<GameStateResponse>(await MoveAsync(game.Id, state.CurrentPlayer!.Value.ToString(), cell));

        var reset = await ReadAsync<GameStateResponse>(await _client.PostAsync($"/api/games/{game.Id}/reset", null));

        Assert.Equal(GameStatus.InProgress, reset.Status);
        Assert.Empty(reset.Moves);
        Assert.Equal(Player.X, reset.CurrentPlayer);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), reset.Scoreboard);
    }

    // ---------- helpers ----------

    private async Task<GameStateResponse> CreateAsync(GameMode mode = GameMode.TwoPlayer)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = mode.ToString() });
        response.EnsureSuccessStatusCode();
        return await ReadAsync<GameStateResponse>(response);
    }

    private Task<HttpResponseMessage> MoveAsync(Guid gameId, string player, int cellIndex) =>
        _client.PostAsJsonAsync($"/api/games/{gameId}/moves", new { player, cellIndex });

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
