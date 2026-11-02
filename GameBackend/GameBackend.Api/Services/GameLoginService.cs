using GameBackend.Api.Auth;
using GameBackend.Api.Models;

namespace GameBackend.Api.Services;

public enum GameLoginStatus { Success, InvalidCredentials, NoGameAccess }

public sealed record GameLoginResult(GameLoginStatus Status, Player? Player = null, string? GameToken = null);

public interface IGameLoginService
{
    // Logger ind via hjemmesiden, henter game-token og gemmer spilleren
    Task<GameLoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
}

public sealed class GameLoginService(
    IWebsiteAuthClient website,
    IGameTokenValidator tokenValidator,
    IPlayerService players,
    IConfiguration config) : IGameLoginService
{
    public async Task<GameLoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        email = email.Trim();

        // 1+2) hjemmesiden tjekker email + password og udsteder kun game-token, hvis spillet er købt
        var gameToken = await website.GameLoginAsync(email, password, GetGameId(), cancellationToken);
        switch (gameToken.Status)
        {
            case GameTokenStatus.InvalidCredentials:
                return new GameLoginResult(GameLoginStatus.InvalidCredentials);
            case GameTokenStatus.NoGameAccess:
                return new GameLoginResult(GameLoginStatus.NoGameAccess);
        }

        // 3) valider game-tokenet med samme regler som [Authorize(Policy = "GamePlay")]
        var claims = await tokenValidator.ValidateAsync(gameToken.Token!)
            ?? throw new WebsiteUnavailableException("Game-token fra hjemmesiden kunne ikke valideres (tjek at Jwt:Key er ens).");

        // 4) gem/opdater spilleren. Email fra tokenet, ellers den der blev logget ind med
        var player = await players.VerifyAsync(
            claims.UserId, claims.Username, claims.GameId, claims.Email ?? email, cancellationToken);

        return new GameLoginResult(GameLoginStatus.Success, player, gameToken.Token);
    }

    private int GetGameId() =>
        int.TryParse(config["Website:GameId"], out var gameId)
            ? gameId
            : throw new InvalidOperationException("Website:GameId mangler i konfigurationen.");
}
