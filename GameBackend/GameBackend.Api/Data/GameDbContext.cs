using GameBackend.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("Players");
            entity.HasKey(p => p.PlayerId);

            // snake_case som i Website-databasen
            entity.Property(p => p.PlayerId).HasColumnName("player_id");
            entity.Property(p => p.UserId).HasColumnName("user_id");
            entity.Property(p => p.GameId).HasColumnName("game_id");
            entity.Property(p => p.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            entity.Property(p => p.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(p => p.PasswordHash).HasColumnName("password_hash");

            // Én spiller pr. bruger pr. spil
            entity.HasIndex(p => new { p.UserId, p.GameId }).IsUnique();

            // Én konto pr. email
            entity.HasIndex(p => p.Email).IsUnique();
        });
    }
}