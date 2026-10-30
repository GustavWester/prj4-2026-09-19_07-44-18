using System.Security.Claims;
using GameBackend.Api.Dtos;
using GameBackend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/game")]
[Authorize(Policy = "GamePlay")]
public class GameController(IPlayerService players) : ControllerBase
{
    [HttpPost("verify")]
    [ProducesResponseType<VerifyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
     public async Task<IActionResult> Verify(CancellationToken cancellationToken)
    {
        var username = User.FindFirstValue("name");

        if (!int.TryParse(User.FindFirstValue("sub"), out var userId) ||
            !int.TryParse(User.FindFirstValue("game_id"), out var gameId) ||
            string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new { error = "Game token mangler sub, name eller game_id." });
        }

        var player = await players.VerifyAsync(userId, username, gameId, cancellationToken);
        return Ok(new VerifyResponse(player.UserId, player.Username, gameId));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<VerifyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var player = await players.LoginAsync(request.Email, request.Password, cancellationToken);
        if (player is null)
            return Unauthorized(new { error = "Forkert email eller password." });

        // Kontoen får først et game_id, når spillet er købt på hjemmesiden
        if (player.GameId is not int gameId)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kontoen har ikke adgang til spillet." });

        return Ok(new VerifyResponse(player.UserId, player.Username, gameId));
    }
}