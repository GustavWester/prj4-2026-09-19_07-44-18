using GameBackend.Api.Models;

namespace GameBackend.Api.Services;

public interface IPlayerService
{
    // Opretter en spiller første gang, ellers opdateres brugernavn og email fra hjemmesiden
    Task<Player> VerifyAsync(int userId, string username, int gameId, string? email, CancellationToken cancellationToken);
}
