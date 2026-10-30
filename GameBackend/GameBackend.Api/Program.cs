using System.Text;
using GameBackend.Api.Data;
using GameBackend.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("GameDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:GameDb mangler i User Secrets.");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(connectionString));

// Services
builder.Services.AddScoped<IPlayerService, PlayerService>();    

// JWT game-token fra Website Backend
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(signingKey))
    throw new InvalidOperationException("Jwt:Key mangler. Kør: dotnet user-secrets set \"Jwt:Key\" \"<samme nøgle som Website Backend>\"");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Behold claim-navnene "sub", "name", "game_id" osv. som Website Backend skriver dem
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],          // "DungeonCrawlerApi"
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],      // "dungeon-crawler-game-api"
            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Kun game-tokens med scope "game:play" og et game_id
    options.AddPolicy("GamePlay", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("scope", "game:play")
        .RequireClaim("game_id"));
});

// Controllers/Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // Hvem er det? (validerer tokenet)
app.UseAuthorization();    // Må du det her? ([Authorize(Policy = "GamePlay")])
app.MapControllers();

app.Run();

// Gør Program synlig for testprojektet (WebApplicationFactory)
public partial class Program { }