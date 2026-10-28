namespace GameBackend.Api.Models;

public class Player
{
    public int PlayerId { get; set; }
    public int UserId { get; set; }                 // "sub" fra game token (Website Users.user_id)
    public int GameId { get; set; }                 // "game_id" fra game token
    public string Username { get; set; } = null!;   // "name" fra game token
    public DateTime CreatedAt { get; set; }         // første gang spilleren startede spillet
    public DateTime LastSeenAt { get; set; }       // seneste spilstart
}