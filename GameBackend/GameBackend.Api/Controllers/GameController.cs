using GameBackend.Api.Auth;
using GameBackend.Api.Dtos;
using GameBackend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/game")]
[Authorize(Policy = "GamePlay")]
public class GameController(IPlayerService players, IGameLoginService gameLogin, ILogger<GameController> logger) : ControllerBase
{
    [HttpPost("verify")]
    [ProducesResponseType<VerifyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify(CancellationToken cancellationToken)
    {
        if (GameTokenValidator.FromPrincipal(User) is not { } claims)
            return Unauthorized(new { error = "Game token mangler sub, name eller game_id." });

        var player = await players.VerifyAsync(claims.UserId, claims.Username, claims.GameId, claims.Email, cancellationToken);
        return Ok(new VerifyResponse(player.UserId, player.Username, claims.GameId));
    }

    // Unity: email + password -> hjemmesiden tjekker login og udsteder game-token
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        GameLoginResult result;
        try
        {
            result = await gameLogin.LoginAsync(request.Email, request.Password, cancellationToken);
        }
        catch (WebsiteUnavailableException ex)
        {
            logger.LogWarning(ex, "Login via hjemmesiden fejlede");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Kan ikke nå hjemmesiden." });
        }

        return result.Status switch
        {
            GameLoginStatus.InvalidCredentials => Unauthorized(new { error = "Forkert email eller password." }),
            GameLoginStatus.NoGameAccess => StatusCode(StatusCodes.Status403Forbidden, new { error = "Kontoen har ikke adgang til spillet." }),
            _ => Ok(new LoginResponse(result.Player!.UserId, result.Player.Username, result.Player.GameId!.Value, result.GameToken!))
        };
    }
}
