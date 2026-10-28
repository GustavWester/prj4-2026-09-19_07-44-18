namespace GameBackend.Api.Dtos;

// Svar fra POST /api/game/verify
public sealed record VerifyResponse(int UserId, string Username, int GameId);