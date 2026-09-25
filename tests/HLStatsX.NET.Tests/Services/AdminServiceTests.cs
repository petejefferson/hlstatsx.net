using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Infrastructure.Services;

namespace HLStatsX.NET.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IAdminRepository> _repoMock;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _repoMock = new Mock<IAdminRepository>();
        _service = new AdminService(_repoMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenCredentialsValid()
    {
        var hash = ComputeMd5("securepassword");
        var user = new AdminUser { Username = "admin", Password = hash };
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", default)).ReturnsAsync(user);

        var result = await _service.AuthenticateAsync("admin", "securepassword");

        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordWrong()
    {
        var user = new AdminUser { Username = "admin", Password = ComputeMd5("correct") };
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", default)).ReturnsAsync(user);

        var result = await _service.AuthenticateAsync("admin", "wrongpassword");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenUserNotFound()
    {
        _repoMock.Setup(r => r.GetByUsernameAsync("ghost", default)).ReturnsAsync((AdminUser?)null);

        var result = await _service.AuthenticateAsync("ghost", "anything");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_HashesPassword()
    {
        AdminUser? capturedUser = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<AdminUser>(), default))
            .Callback<AdminUser, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        var user = new AdminUser { Username = "newadmin" };
        await _service.CreateUserAsync(user, "mypassword");

        capturedUser.Should().NotBeNull();
        capturedUser!.Password.Should().NotBe("mypassword");
        capturedUser.Password.Should().HaveLength(32); // MD5 hex = 32 chars
    }

    [Fact]
    public async Task GetOptionsAsync_ReturnsDictionaryFromOptions()
    {
        var options = new List<Option>
        {
            new() { KeyName = "style", Value = "default" },
            new() { KeyName = "countPerPage", Value = "50" }
        };
        _repoMock.Setup(r => r.GetOptionsAsync(default)).ReturnsAsync(options);

        var result = await _service.GetOptionsAsync();

        result.Should().ContainKey("style").WhoseValue.Should().Be("default");
        result.Should().ContainKey("countPerPage").WhoseValue.Should().Be("50");
    }

    [Fact]
    public async Task DeleteUserAsync_CallsRepository()
    {
        _repoMock.Setup(r => r.DeleteAsync("admin", default)).Returns(Task.CompletedTask);

        await _service.DeleteUserAsync("admin");

        _repoMock.Verify(r => r.DeleteAsync("admin", default), Times.Once);
    }

    // ── GetUsersAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_DelegatesToRepository()
    {
        var users = new List<AdminUser>
        {
            new() { Username = "admin" },
            new() { Username = "moderator" },
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(users);

        var result = await _service.GetUsersAsync();

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetAllAsync(default), Times.Once);
    }

    // ── UpdateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_WithNewPassword_HashesPassword()
    {
        AdminUser? captured = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<AdminUser>(), default))
                 .Callback<AdminUser, CancellationToken>((u, _) => captured = u)
                 .Returns(Task.CompletedTask);

        var user = new AdminUser { Username = "admin", Password = "old_hash" };
        await _service.UpdateUserAsync(user, "newpassword");

        captured.Should().NotBeNull();
        captured!.Password.Should().NotBe("newpassword");
        captured.Password.Should().HaveLength(32);
        captured.Password.Should().NotBe("old_hash");
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullPassword_PreservesExistingHash()
    {
        AdminUser? captured = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<AdminUser>(), default))
                 .Callback<AdminUser, CancellationToken>((u, _) => captured = u)
                 .Returns(Task.CompletedTask);

        var user = new AdminUser { Username = "admin", Password = "existing_hash" };
        await _service.UpdateUserAsync(user, null);

        captured!.Password.Should().Be("existing_hash");
    }

    [Fact]
    public async Task UpdateUserAsync_WithEmptyPassword_PreservesExistingHash()
    {
        AdminUser? captured = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<AdminUser>(), default))
                 .Callback<AdminUser, CancellationToken>((u, _) => captured = u)
                 .Returns(Task.CompletedTask);

        var user = new AdminUser { Username = "admin", Password = "existing_hash" };
        await _service.UpdateUserAsync(user, "");

        captured!.Password.Should().Be("existing_hash");
    }

    // ── SetOptionAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetOptionAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.SetOptionAsync("style", "dark", default)).Returns(Task.CompletedTask);

        await _service.SetOptionAsync("style", "dark");

        _repoMock.Verify(r => r.SetOptionAsync("style", "dark", default), Times.Once);
    }

    // ── SaveOptionsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveOptionsAsync_CallsSetOptionForEachEntry()
    {
        var values = new Dictionary<string, string>
        {
            ["style"]        = "dark",
            ["countPerPage"] = "50",
            ["sitename"]     = "MyStats",
        };
        _repoMock.Setup(r => r.SetOptionAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                 .Returns(Task.CompletedTask);

        await _service.SaveOptionsAsync(values);

        _repoMock.Verify(r => r.SetOptionAsync("style",        "dark",     default), Times.Once);
        _repoMock.Verify(r => r.SetOptionAsync("countPerPage", "50",       default), Times.Once);
        _repoMock.Verify(r => r.SetOptionAsync("sitename",     "MyStats",  default), Times.Once);
    }

    [Fact]
    public async Task SaveOptionsAsync_HandlesEmptyDictionary()
    {
        await _service.SaveOptionsAsync(new Dictionary<string, string>());

        _repoMock.Verify(r => r.SetOptionAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── Games delegation ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSupportedGamesAsync_DelegatesToRepository()
    {
        var games = new List<GameSupported> { new() { Code = "cstrike" } };
        _repoMock.Setup(r => r.GetSupportedGamesAsync(default)).ReturnsAsync(games);

        var result = await _service.GetSupportedGamesAsync();

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetSupportedGamesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddGameAsync_DelegatesToRepository()
    {
        var game = new Game { Code = "tf2" };
        _repoMock.Setup(r => r.AddGameAsync(game, default)).Returns(Task.CompletedTask);

        await _service.AddGameAsync(game);

        _repoMock.Verify(r => r.AddGameAsync(game, default), Times.Once);
    }

    [Fact]
    public async Task DeleteGameAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DeleteGameAsync("tf2", default)).Returns(Task.CompletedTask);

        await _service.DeleteGameAsync("tf2");

        _repoMock.Verify(r => r.DeleteGameAsync("tf2", default), Times.Once);
    }

    // ── Server delegation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetServerByIdAsync_DelegatesToRepository()
    {
        var server = new Server { ServerId = 5, Name = "FragServer" };
        _repoMock.Setup(r => r.GetServerByIdAsync(5, default)).ReturnsAsync(server);

        var result = await _service.GetServerByIdAsync(5);

        result.Should().NotBeNull();
        result!.Name.Should().Be("FragServer");
    }

    [Fact]
    public async Task DeleteServerAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DeleteServerAsync(3, default)).Returns(Task.CompletedTask);

        await _service.DeleteServerAsync(3);

        _repoMock.Verify(r => r.DeleteServerAsync(3, default), Times.Once);
    }

    // ── Rank delegation ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRanksAsync_DelegatesToRepository()
    {
        var ranks = new List<Rank> { new() { RankId = 1, RankName = "Private" } };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        var result = await _service.GetRanksAsync("cstrike");

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetRanksAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task AddRankAsync_DelegatesToRepository()
    {
        var rank = new Rank { RankName = "General" };
        _repoMock.Setup(r => r.AddRankAsync(rank, default)).Returns(Task.CompletedTask);

        await _service.AddRankAsync(rank);

        _repoMock.Verify(r => r.AddRankAsync(rank, default), Times.Once);
    }

    [Fact]
    public async Task DeleteRankAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DeleteRankAsync(7, default)).Returns(Task.CompletedTask);

        await _service.DeleteRankAsync(7);

        _repoMock.Verify(r => r.DeleteRankAsync(7, default), Times.Once);
    }

    // ── Ribbon delegation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRibbonsAsync_DelegatesToRepository()
    {
        var ribbons = new List<Ribbon> { new() { RibbonId = 1, RibbonName = "First Blood" } };
        _repoMock.Setup(r => r.GetRibbonsAsync("cstrike", default)).ReturnsAsync(ribbons);

        var result = await _service.GetRibbonsAsync("cstrike");

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetRibbonsAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task AddRibbonAsync_DelegatesToRepository()
    {
        var ribbon = new Ribbon { RibbonName = "Ace" };
        _repoMock.Setup(r => r.AddRibbonAsync(ribbon, default)).Returns(Task.CompletedTask);

        await _service.AddRibbonAsync(ribbon);

        _repoMock.Verify(r => r.AddRibbonAsync(ribbon, default), Times.Once);
    }

    private static string ComputeMd5(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
