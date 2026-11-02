using GameBackend.Api.Data;
using GameBackend.Api.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GameBackend.Api.Tests.Services;

[TestFixture]
public class PlayerServiceTests
{
    private GameDbContext _db = null!;
    private PlayerService _playerService = null!;

    [SetUp]
    public void SetUp()
    {
        // Ny, tom database til hver test, så testene ikke på virker hinanden
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new GameDbContext(options);
        _playerService = new PlayerService(_db);    
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task VerifyAsync_NewPlayer_CreatesPlayer()
    {
        var result = await _playerService.VerifyAsync(1, "testspiller", 7, CancellationToken.None);

        var saved = await _db.Players.AsNoTracking().SingleAsync();
        Assert.That(saved.UserId, Is.EqualTo(1));
        Assert.That(saved.Username, Is.EqualTo("testspiller"));
        Assert.That(saved.GameId, Is.EqualTo(7));
        Assert.That(result.PlayerId, Is.EqualTo(saved.PlayerId));
    }

    [Test]
    public async Task VerifyAsync_ExistingPlayer_DoesNotCreateDuplicate()
    {
        await _playerService.VerifyAsync(1, "testspiller", 7, CancellationToken.None);
        await _playerService.VerifyAsync(1, "testspiller", 7, CancellationToken.None);

        Assert.That(await _db.Players.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task VerifyAsync_UsernameChangedOnWebsite_UpdatesUsername()
    {
        await _playerService.VerifyAsync(1, "gammeltnavn", 7, CancellationToken.None);
        await _playerService.VerifyAsync(1, "nytnavn", 7, CancellationToken.None);

        var saved = await _db.Players.AsNoTracking().SingleAsync();
        Assert.That(saved.Username, Is.EqualTo("nytnavn"));
    }

    [Test]
    public async Task VerifyAsync_SameUserDifferntGame_CreatesSeparatePlayer()
    {
        await _playerService.VerifyAsync(1, "testspiller", 7, CancellationToken.None);
        await _playerService.VerifyAsync(1, "testspiller", 8, CancellationToken.None);

        Assert.That(await _db.Players.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task VerifyAsync_DifferentUsersSameGame_CreatesSeparatePlayers()
    {
        await _playerService.VerifyAsync(1, "spiller1", 7, CancellationToken.None);
        await _playerService.VerifyAsync(2, "spiller2", 7, CancellationToken.None);

        Assert.That(await _db.Players.CountAsync(), Is.EqualTo(2));
    }
}    