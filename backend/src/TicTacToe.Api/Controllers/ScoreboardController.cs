using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
[Produces("application/json")]
public sealed class ScoreboardController : ControllerBase
{
    private readonly IGameService _games;

    public ScoreboardController(IGameService games) => _games = games;

    /// <summary>Get the session scoreboard (X wins, O wins, draws).</summary>
    [HttpGet]
    public ActionResult<ScoreSnapshot> Get() => _games.GetScoreboard();

    /// <summary>Reset the scoreboard to zero.</summary>
    [HttpPost("reset")]
    public ActionResult<ScoreSnapshot> Reset() => _games.ResetScoreboard();
}
