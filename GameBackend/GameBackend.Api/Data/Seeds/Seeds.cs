using GameBackend.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace GameBackend.Api.Data.Seeds;

// seed data til hele databasen
public static class Seed
{
    public static void SeedPlayers(GameDbContext db)
    {
        if (db.Players.Any()) return;          // er der allerede data? så stop

        var players = new[]
        {
            new Player { UserId = 1, GameId = 001, Username = "Goonarkus", Email = "markus@dungeon.dk" },
            new Player { UserId = 2, GameId = 002, Username = "Gohn", Email = "john@dungeon.dk" },
            new Player { UserId = 3, GameId = 003, Username = "Goonustav", Email = "gustav@dungeon.dk" },
            new Player { UserId = 4, GameId = null, Username = "NoGame_Tester", Email = "nogame@dungeon.dk" } // har ikke købt spillet
        };
        
        db.Players.AddRange(players);
        db.SaveChanges();
    }
}