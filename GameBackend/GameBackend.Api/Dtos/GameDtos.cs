namespace GameBackend.Api.Dtos;

// Body til POST /api/game/login
public sealed record LoginRequest(string Email, string Password);

// Svar fra POST /api/game/verify og POST /api/game/login
public sealed record VerifyResponse(int UserId, string Username, int GameId);