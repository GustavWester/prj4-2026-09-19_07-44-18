using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GameBackend.Api.Auth;

// Indholdet af et gyldigt game-token
public sealed record GameTokenClaims(int UserId, string Username, int GameId, string? Email);

public interface IGameTokenValidator
{
    // Validerer tokenet med samme regler som JwtBearer, ellers null
    Task<GameTokenClaims?> ValidateAsync(string token);
}

public sealed class GameTokenValidator(TokenValidationParameters parameters) : IGameTokenValidator
{
    // Behold claim-navnene "sub", "name", "game_id" osv. som Website Backend skriver dem
    private readonly JsonWebTokenHandler handler = new() { MapInboundClaims = false };

    public async Task<GameTokenClaims?> ValidateAsync(string token)
    {
        var result = await handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid) return null;

        return FromPrincipal(new ClaimsPrincipal(result.ClaimsIdentity));
    }

    // Kræver samme claims som "GamePlay"-policyen + sub og name
    public static GameTokenClaims? FromPrincipal(ClaimsPrincipal user)
    {
        if (!user.HasClaim(GameTokenValidation.ScopeClaim, GameTokenValidation.GamePlayScope))
            return null;

        var username = user.FindFirstValue(GameTokenValidation.NameClaim);
        if (!int.TryParse(user.FindFirstValue(GameTokenValidation.UserIdClaim), out var userId) ||
            !int.TryParse(user.FindFirstValue(GameTokenValidation.GameIdClaim), out var gameId) ||
            string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var email = user.FindFirstValue(GameTokenValidation.EmailClaim);
        return new GameTokenClaims(userId, username, gameId, string.IsNullOrWhiteSpace(email) ? null : email);
    }
}
