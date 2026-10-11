using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games) => _games = games;

    /// <summary>Create a new game session (Two Player by default).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status201Created)]
    public ActionResult<GameStateResponse> Create([FromBody] CreateGameRequest? request)
    {
        var state = _games.CreateGame(request?.Mode ?? GameMode.TwoPlayer);
        return CreatedAtAction(nameof(Get), new { id = state.Id }, state);
    }

    /// <summary>Get the current state of a game.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public ActionResult<GameStateResponse> Get(Guid id) => _games.GetGame(id);

    /// <summary>Submit a move. In computer mode the response already includes the computer's reply.</summary>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public ActionResult<GameStateResponse> PlayMove(Guid id, [FromBody] MoveRequest request) =>
        _games.PlayMove(id, request.Player!.Value, request.CellIndex!.Value);

    /// <summary>Undo the last move (Two Player) or the last move pair (Computer mode).</summary>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public ActionResult<GameStateResponse> Undo(Guid id) => _games.Undo(id);

    /// <summary>Reset the current game. The scoreboard is kept.</summary>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public ActionResult<GameStateResponse> Reset(Guid id) => _games.Reset(id);
}
