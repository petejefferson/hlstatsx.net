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

    private static string ComputeMd5(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
