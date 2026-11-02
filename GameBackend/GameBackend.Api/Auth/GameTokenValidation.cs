using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GameBackend.Api.Auth;

// Fælles regler for game-tokens fra Website Backend.
// Bruges både af JwtBearer (Authorization-header) og af GameTokenValidator (login-flowet).
public static class GameTokenValidation
{
    public const string ScopeClaim = "scope";
    public const string GamePlayScope = "game:play";
    public const string GameIdClaim = "game_id";
    public const string UserIdClaim = "sub";
    public const string NameClaim = "name";
    public const string EmailClaim = "email";

    public static TokenValidationParameters Create(IConfiguration config)
    {
        var jwt = config.GetSection("Jwt");
        var signingKey = jwt["Key"];
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException("Jwt:Key mangler. Kør: dotnet user-secrets set \"Jwt:Key\" \"<samme nøgle som Website Backend>\"");

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],          // "DungeonCrawlerApi"
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],      // "dungeon-crawler-game-api"
            ValidateLifetime = true,
            NameClaimType = NameClaim,
            RoleClaimType = "role"
        };
    }
}
