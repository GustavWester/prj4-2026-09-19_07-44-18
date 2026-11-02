using System.Net;

namespace GameBackend.Api.Services;

public enum GameTokenStatus { Ok, NoGameAccess, InvalidCredentials }

public sealed record GameTokenResult(GameTokenStatus Status, string? Token);

// Hjemmesiden svarer ikke eller svarer uventet
public sealed class WebsiteUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public interface IWebsiteAuthClient
{
    // Game-login på hjemmesiden (email + password) og returnerer game-token til det angivne spil
    Task<GameTokenResult> GameLoginAsync(string email, string password, int gameId, CancellationToken cancellationToken);
}

// Kalder Website Backend (DungeonCrawler.Api). BaseAddress kommer fra "Website:BaseUrl".
public sealed class WebsiteAuthClient(HttpClient http) : IWebsiteAuthClient
{
    public async Task<GameTokenResult> GameLoginAsync(string email, string password, int gameId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => http.PostAsJsonAsync("api/auth/game-login", new WebsiteGameLoginRequest(email, password), cancellationToken));

        // 401 = forkert email/password ELLER kontoen ejer ingen spil (hjemmesiden skelner ikke)
        // 400 = valideringsfejl (fx tomt felt eller ugyldig email), behandles som forkert login
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            return new GameTokenResult(GameTokenStatus.InvalidCredentials, null);

        if (!response.IsSuccessStatusCode)
            throw new WebsiteUnavailableException($"Hjemmesiden svarede {(int)response.StatusCode} på game-login.");

        var body = await response.Content.ReadFromJsonAsync<WebsiteGameLoginResponse>(cancellationToken)
            ?? throw new WebsiteUnavailableException("Hjemmesiden returnerede et tomt svar ved game-login.");

        // Kontoen ejer andre spil, men ikke dette
        var token = body.GameTokens?.FirstOrDefault(t => t.GameId == gameId)?.Token;
        return string.IsNullOrWhiteSpace(token)
            ? new GameTokenResult(GameTokenStatus.NoGameAccess, null)
            : new GameTokenResult(GameTokenStatus.Ok, token);
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException ex)
        {
            throw new WebsiteUnavailableException("Kan ikke nå hjemmesidens API.", ex);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new WebsiteUnavailableException("Hjemmesidens API svarede ikke i tide.", ex);
        }
    }

    // JSON-kontrakter fra DungeonCrawler.Api (camelCase), se AuthDtos.cs på hjemmesiden
    private sealed record WebsiteGameLoginRequest(string Email, string Password);
    private sealed record WebsiteGameLoginResponse(IReadOnlyList<WebsiteGameToken>? GameTokens);
    private sealed record WebsiteGameToken(int GameId, string? Token, int ExpiresInSeconds);
}
