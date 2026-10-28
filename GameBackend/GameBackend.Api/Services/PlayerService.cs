using GameBackend.Api.Data;
using GameBackend.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Services;

public sealed class PlayerService(GameDbContext db, TimeProvider clock) : IPlayerService
{
    public async Task<Player> VerifyAsync(
        int userId,
        string username,
        int gameId,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var player = await FindAsync(userId, gameId, cancellationToken);
        if (player is not null)
        {
            Update(player, username, now);
            await db.SaveChangesAsync(cancellationToken);
            return player;
        }
        player = new Player
        {
            UserId = userId,
            GameId = gameId,
            Username = username,
            CreatedAt = now,
            LastSeenAt = now
        };
        db.Players.Add(player);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return player;
        }
        catch (DbUpdateException)
        {
            // To requests oprettede spiller samtidig, og det unikke indeks
            // (user_id, game_id) stoppede den ene. Hent den eksisterende og opdater den i stedet.
            db.ChangeTracker.Clear();
            var existing = await FindAsync(userId, gameId, cancellationToken)
                ?? throw new InvalidOperationException("Spilleren kunne hverken oprettes eller findes.");
            
            Update(existing, username, now);
            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
    private Task<Player?> FindAsync(int userId, int gameId, CancellationToken cancellationToken) =>
        db.Players.SingleOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId, cancellationToken);

    private static void Update(Player player, string username, DateTime now)
    {
        player.Username = username;  // følger altid brugernavn på hjemmesiden
        player.LastSeenAt = now;
    }    
}