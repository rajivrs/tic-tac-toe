using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Infrastructure;

/// <summary>
/// Translates domain exceptions into a consistent JSON error body (<see cref="ApiError"/>):
/// 400 for a move outside the board, 404 for an unknown game, 409 for rule conflicts
/// (occupied cell, wrong player, game over, nothing to undo).
/// Implemented as an MVC filter so response headers added earlier (e.g. CORS) are preserved.
/// </summary>
public sealed class GameExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, code) = context.Exception switch
        {
            GameNotFoundException => (StatusCodes.Status404NotFound, "GameNotFound"),
            GameRuleException rule => (
                rule.Error == GameError.OutOfBounds ? StatusCodes.Status400BadRequest : StatusCodes.Status409Conflict,
                rule.Error.ToString()),
            _ => (0, string.Empty)
        };

        if (status == 0) return; // not a game error: let the default 500 handling run

        context.Result = new ObjectResult(new ApiError(code, context.Exception.Message)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
