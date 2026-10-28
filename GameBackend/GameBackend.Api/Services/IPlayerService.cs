using GameBackend.Api.Models;

namespace GameBackend.Api.Services;

public interface IPlayerService
{
    // Opretter en spiller første gang, ellers opdateres brugernavn og seneste spilstart
    Task<Player> VerifyAsync(int userId, string username, int gameId, CancellationToken cancellationToken);
}