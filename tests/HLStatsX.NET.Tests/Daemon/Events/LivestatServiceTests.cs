using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Tests.Daemon.Events;

/// <summary>
/// Tests for the early-exit guards in <see cref="LivestatService"/>.
/// The actual INSERT/UPDATE/DELETE SQL paths require a real MySQL connection
/// and are covered by integration tests against a live DB.
/// </summary>
public sealed class LivestatServiceTests
{
    private static Mock<IDbContextFactory<HLStatsDbContext>> MockFactory() => new();

    private static PlayerSession ValidSession() => new()
    {
        DbPlayerId = 10,
        ServerId   = 1,
        Name       = "TestPlayer",
        IpAddress  = "1.2.3.4",
        PlainUniqueId = "STEAM_0:1:12345",
        Team       = "Allies",
        ConnectTime = 1_000_000,
        Skill       = 1000,
    };

    // ── UpsertAsync guards ────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_DbPlayerIdZero_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        var svc = new LivestatService(factory.Object);
        var session = ValidSession();
        session.DbPlayerId = 0;

        await svc.UpsertAsync(session);

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task UpsertAsync_ServerIdZero_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        var svc = new LivestatService(factory.Object);
        var session = ValidSession();
        session.ServerId = 0;

        await svc.UpsertAsync(session);

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task UpsertAsync_NegativePlayerId_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        var svc = new LivestatService(factory.Object);
        var session = ValidSession();
        session.DbPlayerId = -5;

        await svc.UpsertAsync(session);

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── DeleteAsync guards ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_InvalidPlayerId_ReturnsWithoutOpeningDb(int playerId)
    {
        var factory = MockFactory();
        var svc = new LivestatService(factory.Object);

        await svc.DeleteAsync(playerId);

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── UpdateAsync guards ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateAsync_InvalidDbPlayerId_ReturnsWithoutOpeningDb(int dbPlayerId)
    {
        var factory = MockFactory();
        var svc = new LivestatService(factory.Object);
        var session = ValidSession();
        session.DbPlayerId = dbPlayerId;

        await svc.UpdateAsync(session);

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── Valid session — verifies DB IS opened (not a guard) ──────────────────

    [Fact]
    public async Task UpsertAsync_ValidSession_OpensDbContext()
    {
        // DbContext is opened but ExecuteSqlRawAsync will throw with null connection —
        // we just verify the guard was passed and the context was created.
        var factory = MockFactory();
        factory.Setup(f => f.CreateDbContext())
               .Throws(new InvalidOperationException("no DB in unit tests"));

        var svc = new LivestatService(factory.Object);
        var session = ValidSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpsertAsync(session));

        factory.Verify(f => f.CreateDbContext(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ValidPlayerId_OpensDbContext()
    {
        var factory = MockFactory();
        factory.Setup(f => f.CreateDbContext())
               .Throws(new InvalidOperationException("no DB in unit tests"));

        var svc = new LivestatService(factory.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteAsync(42));

        factory.Verify(f => f.CreateDbContext(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidSession_OpensDbContext()
    {
        var factory = MockFactory();
        factory.Setup(f => f.CreateDbContext())
               .Throws(new InvalidOperationException("no DB in unit tests"));

        var svc = new LivestatService(factory.Object);
        var session = ValidSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(session));

        factory.Verify(f => f.CreateDbContext(), Times.Once);
    }
}
