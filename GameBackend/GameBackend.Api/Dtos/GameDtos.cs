namespace GameBackend.Api.Dtos;

// Body til POST /api/game/login
public sealed record LoginRequest(string Email, string Password);

// Svar fra POST /api/game/verify
public sealed record VerifyResponse(int UserId, string Username, int GameId);

// Svar fra POST /api/game/login. Token = game-JWT fra hjemmesiden (Bearer til fx /verify)
public sealed record LoginResponse(int UserId, string Username, int GameId, string Token);
