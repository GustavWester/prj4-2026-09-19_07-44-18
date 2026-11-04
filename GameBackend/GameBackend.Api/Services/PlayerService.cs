using GameBackend.Api.Data;
using GameBackend.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Services;

public sealed class PlayerService(GameDbContext db) : IPlayerService
{
    public async Task<Player> VerifyAsync(
        int userId,
        string username,
        int gameId,
        string? email,
        CancellationToken cancellationToken)
    {
        var player = await FindAsync(userId, gameId, cancellationToken);
        if (player is not null)
        {
            Update(player, username, email);
            await db.SaveChangesAsync(cancellationToken);
            return player;
        }

        player = new Player
        {
            UserId = userId,
            GameId = gameId,
            Username = username,
            Email = email,
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

            Update(existing, username, email);
            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }

    // Følger altid brugernavn og email på hjemmesiden
    private static void Update(Player player, string username, string? email)
    {
        player.Username = username;
        if (email is not null) player.Email = email;
    }

    private Task<Player?> FindAsync(int userId, int gameId, CancellationToken cancellationToken) =>
        db.Players.SingleOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId, cancellationToken);
}
