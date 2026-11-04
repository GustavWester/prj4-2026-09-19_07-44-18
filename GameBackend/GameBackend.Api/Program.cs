using GameBackend.Api.Auth;
using GameBackend.Api.Data;
using GameBackend.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("GameDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:GameDb mangler i User Secrets.");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT game-token fra Website Backend (samme regler til Authorization-header og login-flowet)
var tokenValidationParameters = GameTokenValidation.Create(builder.Configuration);
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddSingleton<IGameTokenValidator, GameTokenValidator>();

// Website Backend (DungeonCrawler.Api) tjekker email + password og udsteder game-token
var websiteBaseUrl = builder.Configuration["Website:BaseUrl"];
if (string.IsNullOrWhiteSpace(websiteBaseUrl))
    throw new InvalidOperationException("Website:BaseUrl mangler i appsettings.json.");

builder.Services.AddHttpClient<IWebsiteAuthClient, WebsiteAuthClient>(client =>
{
    client.BaseAddress = new Uri(websiteBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Services
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameLoginService, GameLoginService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Behold claim-navnene "sub", "name", "game_id" osv. som Website Backend skriver dem
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokenValidationParameters;
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Kun game-tokens med scope "game:play" og et game_id
    options.AddPolicy("GamePlay", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(GameTokenValidation.ScopeClaim, GameTokenValidation.GamePlayScope)
        .RequireClaim(GameTokenValidation.GameIdClaim));
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
