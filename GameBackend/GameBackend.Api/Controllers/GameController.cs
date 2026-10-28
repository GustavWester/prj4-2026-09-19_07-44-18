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
        return Ok(new VerifyResponse(player.UserId, player.Username, player.GameId));
    }
}