using System.Security.Claims;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers.Admin;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _adminMock;
    private readonly Mock<IServerService> _serverMock;
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IClanRepository> _clanRepoMock;
    private readonly Mock<IGameRepository> _gameRepoMock;
    private readonly Mock<ISearchService> _searchMock;
    private readonly IConfiguration _config;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _adminMock      = new Mock<IAdminService>();
        _serverMock     = new Mock<IServerService>();
        _playerRepoMock = new Mock<IPlayerRepository>();
        _clanRepoMock   = new Mock<IClanRepository>();
        _gameRepoMock   = new Mock<IGameRepository>();
        _searchMock     = new Mock<ISearchService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"] = "cstrike"
            })
            .Build();

        _controller = new AdminController(
            _adminMock.Object, _serverMock.Object,
            _playerRepoMock.Object, _clanRepoMock.Object,
            _gameRepoMock.Object, _searchMock.Object, _config);

        // Allow TempData to be set by actions that redirect
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    private static ControllerContext MakeHttpContext(bool isAuthenticated = false)
    {
        var identity  = isAuthenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Cookies")
            : new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock
            .Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        authServiceMock
            .Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var urlHelper = new Mock<IUrlHelper>();
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                        .Returns(urlHelper.Object);

        var sp = new Mock<IServiceProvider>();
        sp.Setup(x => x.GetService(typeof(IAuthenticationService)))
          .Returns(authServiceMock.Object);
        sp.Setup(x => x.GetService(typeof(IUrlHelperFactory)))
          .Returns(urlHelperFactory.Object);

        var httpContext = new DefaultHttpContext { User = principal, RequestServices = sp.Object };
        return new ControllerContext { HttpContext = httpContext };
    }

    // ── Login GET ────────────────────────────────────────────────────────────

    [Fact]
    public void Login_GET_ReturnsView_WhenNotAuthenticated()
    {
        _controller.ControllerContext = MakeHttpContext(isAuthenticated: false);

        var result = _controller.Login(null);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Login_GET_RedirectsToDashboard_WhenAlreadyAuthenticated()
    {
        _controller.ControllerContext = MakeHttpContext(isAuthenticated: true);

        var result = _controller.Login(null);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Dashboard");
    }

    // ── Login POST ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_POST_ReturnsView_WhenModelIsInvalid()
    {
        _controller.ControllerContext = MakeHttpContext();
        _controller.ModelState.AddModelError("Username", "Required");
        var model = new LoginViewModel { Username = "", Password = "" };

        var result = await _controller.Login(model, default);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_POST_ReturnsView_WithError_WhenCredentialsInvalid()
    {
        _controller.ControllerContext = MakeHttpContext();
        _adminMock.Setup(a => a.AuthenticateAsync("admin", "wrong", default))
                  .ReturnsAsync((AdminUser?)null);

        var model = new LoginViewModel { Username = "admin", Password = "wrong" };
        var result = await _controller.Login(model, default);

        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_POST_Redirects_WhenCredentialsValid()
    {
        _controller.ControllerContext = MakeHttpContext();
        var user = new AdminUser { Username = "admin", AccLevel = 10 };
        _adminMock.Setup(a => a.AuthenticateAsync("admin", "correct", default)).ReturnsAsync(user);

        var model = new LoginViewModel { Username = "admin", Password = "correct" };
        var result = await _controller.Login(model, default);

        result.Should().BeOfType<LocalRedirectResult>()
              .Which.Url.Should().Be("/Admin/Dashboard");
    }

    [Fact]
    public async Task Login_POST_RespectsReturnUrl()
    {
        _controller.ControllerContext = MakeHttpContext();
        var user = new AdminUser { Username = "admin", AccLevel = 10 };
        _adminMock.Setup(a => a.AuthenticateAsync("admin", "correct", default)).ReturnsAsync(user);

        var model = new LoginViewModel { Username = "admin", Password = "correct", ReturnUrl = "/Admin/Options" };
        var result = await _controller.Login(model, default);

        result.Should().BeOfType<LocalRedirectResult>()
              .Which.Url.Should().Be("/Admin/Options");
    }

    // ── AccessDenied ─────────────────────────────────────────────────────────

    [Fact]
    public void AccessDenied_ReturnsView()
    {
        _controller.ControllerContext = MakeHttpContext();

        var result = _controller.AccessDenied();

        result.Should().BeOfType<ViewResult>();
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsView_WithSummaryStats()
    {
        var servers = new List<Server>
        {
            new() { ServerId = 1, Name = "FragServer", Game = "cstrike", LastEvent = 1 }, // IsActive = true
            new() { ServerId = 2, Name = "IdleServer", Game = "cstrike", LastEvent = 0 }  // IsActive = false
        };
        _serverMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync(servers);
        _playerRepoMock.Setup(r => r.GetTotalCountAsync("cstrike", default)).ReturnsAsync(500);
        _clanRepoMock.Setup(r => r.GetTotalCountAsync("cstrike", default)).ReturnsAsync(20);
        _adminMock.Setup(a => a.GetOptionsAsync(default)).ReturnsAsync(new Dictionary<string, string>());
        _gameRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Game>());

        var result = await _controller.Dashboard(default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<AdminDashboardViewModel>().Subject;
        model.TotalPlayers.Should().Be(500);
        model.TotalClans.Should().Be(20);
        model.ActiveServers.Should().Be(1);     // only the server with LastEvent > 0
        model.Servers.Should().HaveCount(2);
        model.Games.Should().BeEmpty();
    }

    // ── GamesList ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GamesList_ReturnsJsonWithGameCodes()
    {
        var games = new List<Game>
        {
            new() { Code = "cstrike", Name = "Counter-Strike" },
            new() { Code = "dods",    Name = "Day of Defeat: Source" }
        };
        _gameRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(games);

        var result = await _controller.GamesList(default);

        var json  = result.Should().BeOfType<JsonResult>().Subject;
        var items = (json.Value as IEnumerable<object>)!.ToList();
        items.Should().HaveCount(2);
    }

    // ── IpStats ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IpStats_ReturnsView_WithCorrectViewModel()
    {
        var pagedResult = PagedResult<IpStatsHostGroupRow>.Create(
            [new("Acme ISP", 100, 50.0)], totalCount: 1, page: 1, pageSize: 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostGroupAsync(1, 50, "Connects", true, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStats();

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<IpStatsViewModel>().Subject;
        model.SortBy.Should().Be("Connects");
        model.Descending.Should().BeTrue();
        model.Result.Items.Should().HaveCount(1);
        model.Result.Items[0].HostGroup.Should().Be("Acme ISP");
    }

    [Fact]
    public async Task IpStats_NormalisesInvalidSortBy_ToConnects()
    {
        var pagedResult = PagedResult<IpStatsHostGroupRow>.Create([], 0, 1, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostGroupAsync(1, 50, "Connects", true, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        // "Invalid" is not in the allowed list; should be normalised to "Connects"
        var result = await _controller.IpStats("Invalid", true, 1);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<IpStatsViewModel>().Subject;
        model.SortBy.Should().Be("Connects");
    }

    [Fact]
    public async Task IpStats_AcceptsAlternateSortAndPage()
    {
        var pagedResult = PagedResult<IpStatsHostGroupRow>.Create([], 0, 2, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostGroupAsync(2, 50, "HostGroup", false, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStats("HostGroup", false, 2);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<IpStatsViewModel>().Subject;
        model.SortBy.Should().Be("HostGroup");
        model.Descending.Should().BeFalse();
    }

    [Fact]
    public async Task IpStats_EmptyResult_StillReturnsView()
    {
        var pagedResult = PagedResult<IpStatsHostGroupRow>.Create([], 0, 1, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostGroupAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStats();

        result.Should().BeOfType<ViewResult>()
              .Which.Model.Should().BeOfType<IpStatsViewModel>()
              .Which.Result.Items.Should().BeEmpty();
    }

    // ── IpStatsDetail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task IpStatsDetail_ReturnsView_WithCorrectViewModel()
    {
        var pagedResult = PagedResult<IpStatsHostRow>.Create(
            [new("mail.example.com", 42, 21.0)], totalCount: 1, page: 1, pageSize: 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostAsync("Acme ISP", 1, 50, "Connects", true, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStatsDetail("Acme ISP");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<IpStatsDetailViewModel>().Subject;
        model.HostGroup.Should().Be("Acme ISP");
        model.SortBy.Should().Be("Connects");
        model.Descending.Should().BeTrue();
        model.Result.Items[0].Host.Should().Be("mail.example.com");
    }

    [Fact]
    public async Task IpStatsDetail_NormalisesInvalidSortBy_ToConnects()
    {
        var pagedResult = PagedResult<IpStatsHostRow>.Create([], 0, 1, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostAsync(It.IsAny<string>(), 1, 50, "Connects", true, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStatsDetail("Acme ISP", "BadField", true, 1);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<IpStatsDetailViewModel>().Subject;
        model.SortBy.Should().Be("Connects");
    }

    [Fact]
    public async Task IpStatsDetail_AcceptsHostSortAndAscending()
    {
        var pagedResult = PagedResult<IpStatsHostRow>.Create([], 0, 1, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostAsync("Acme ISP", 1, 50, "Host", false, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStatsDetail("Acme ISP", "Host", false, 1);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<IpStatsDetailViewModel>().Subject;
        model.SortBy.Should().Be("Host");
        model.Descending.Should().BeFalse();
    }

    [Fact]
    public async Task IpStatsDetail_UnresolvedSentinel_PassedThrough()
    {
        const string sentinel = "(Unresolved IP Addresses)";
        var pagedResult = PagedResult<IpStatsHostRow>.Create([], 0, 1, 50);
        _adminMock
            .Setup(a => a.GetIpStatsByHostAsync(sentinel, 1, 50, "Connects", true, default))
            .ReturnsAsync(pagedResult);
        _controller.ControllerContext = MakeHttpContext();

        var result = await _controller.IpStatsDetail(sentinel);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<IpStatsDetailViewModel>().Subject;
        model.HostGroup.Should().Be(sentinel);
    }

    // ── Users (list) ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Users_ReturnsView_WithUserList()
    {
        var users = new List<AdminUser>
        {
            new() { Username = "alice", AccLevel = 100 },
            new() { Username = "bob",   AccLevel = 80  },
        };
        _adminMock.Setup(a => a.GetUsersAsync(default)).ReturnsAsync(users);

        var result = await _controller.Users(default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<AdminUserListViewModel>().Subject;
        model.Users.Should().HaveCount(2);
        model.Users[0].Username.Should().Be("alice");
    }

    // ── CreateUser GET ────────────────────────────────────────────────────────

    [Fact]
    public void CreateUser_GET_ReturnsViewWithEmptyModel()
    {
        var result = _controller.CreateUser();

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<CreateAdminUserViewModel>().Subject;
        model.Username.Should().BeEmpty();
        model.AccLevel.Should().Be(100);
    }

    // ── CreateUser POST ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_POST_ReturnsView_WhenModelInvalid()
    {
        _controller.ModelState.AddModelError("Username", "Required");
        var model = new CreateAdminUserViewModel();

        var result = await _controller.CreateUser(model, default);

        result.Should().BeOfType<ViewResult>();
        _adminMock.Verify(a => a.CreateUserAsync(It.IsAny<AdminUser>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateUser_POST_CreatesUser_AndRedirects()
    {
        _adminMock.Setup(a => a.CreateUserAsync(It.IsAny<AdminUser>(), "secret", default))
                  .Returns(Task.CompletedTask);

        var model = new CreateAdminUserViewModel
        {
            Username        = "newadmin",
            Password        = "secret",
            ConfirmPassword = "secret",
            AccLevel        = 100
        };

        var result = await _controller.CreateUser(model, default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Users");
        _adminMock.Verify(a => a.CreateUserAsync(
            It.Is<AdminUser>(u => u.Username == "newadmin" && u.AccLevel == 100),
            "secret", default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUser_POST_AcceptsZeroAccLevel_ForNoAccess()
    {
        _adminMock.Setup(a => a.CreateUserAsync(It.IsAny<AdminUser>(), It.IsAny<string>(), default))
                  .Returns(Task.CompletedTask);

        var model = new CreateAdminUserViewModel
        {
            Username        = "readonly",
            Password        = "pass",
            ConfirmPassword = "pass",
            AccLevel        = 0
        };

        var result = await _controller.CreateUser(model, default);

        result.Should().BeOfType<RedirectToActionResult>();
        _adminMock.Verify(a => a.CreateUserAsync(
            It.Is<AdminUser>(u => u.AccLevel == 0), "pass", default), Times.Once);
    }

    // ── EditUser GET ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EditUser_GET_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _adminMock.Setup(a => a.GetUsersAsync(default)).ReturnsAsync([]);

        var result = await _controller.EditUser("ghost", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditUser_GET_ReturnsView_WithCurrentAccLevel()
    {
        _adminMock.Setup(a => a.GetUsersAsync(default))
                  .ReturnsAsync([new AdminUser { Username = "alice", AccLevel = 80 }]);

        var result = await _controller.EditUser("alice", default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<EditAdminUserViewModel>().Subject;
        model.Username.Should().Be("alice");
        model.AccLevel.Should().Be(80);
        model.NewPassword.Should().BeNull();
    }

    // ── EditUser POST ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EditUser_POST_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _adminMock.Setup(a => a.GetUsersAsync(default)).ReturnsAsync([]);

        var result = await _controller.EditUser("ghost", new EditAdminUserViewModel(), default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditUser_POST_ReturnsView_WhenModelInvalid()
    {
        _controller.ModelState.AddModelError("AccLevel", "Invalid");

        var result = await _controller.EditUser("alice", new EditAdminUserViewModel(), default);

        result.Should().BeOfType<ViewResult>();
        _adminMock.Verify(a => a.UpdateUserAsync(It.IsAny<AdminUser>(), It.IsAny<string?>(), default), Times.Never);
    }

    [Fact]
    public async Task EditUser_POST_UpdatesAccLevel_AndRedirects()
    {
        var user = new AdminUser { Username = "alice", AccLevel = 100, Password = "old_hash" };
        _adminMock.Setup(a => a.GetUsersAsync(default)).ReturnsAsync([user]);
        _adminMock.Setup(a => a.UpdateUserAsync(It.IsAny<AdminUser>(), null, default))
                  .Returns(Task.CompletedTask);

        var model = new EditAdminUserViewModel { AccLevel = 80, NewPassword = null };

        var result = await _controller.EditUser("alice", model, default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Users");
        _adminMock.Verify(a => a.UpdateUserAsync(
            It.Is<AdminUser>(u => u.Username == "alice" && u.AccLevel == 80),
            null, default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task EditUser_POST_PassesNewPassword_WhenProvided()
    {
        var user = new AdminUser { Username = "alice", AccLevel = 100, Password = "old_hash" };
        _adminMock.Setup(a => a.GetUsersAsync(default)).ReturnsAsync([user]);
        _adminMock.Setup(a => a.UpdateUserAsync(It.IsAny<AdminUser>(), "newpass", default))
                  .Returns(Task.CompletedTask);

        var model = new EditAdminUserViewModel { AccLevel = 100, NewPassword = "newpass" };

        var result = await _controller.EditUser("alice", model, default);

        result.Should().BeOfType<RedirectToActionResult>();
        _adminMock.Verify(a => a.UpdateUserAsync(It.IsAny<AdminUser>(), "newpass", default), Times.Once);
    }

    // ── DeleteUser POST ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_POST_DeletesUser_AndRedirects()
    {
        _adminMock.Setup(a => a.DeleteUserAsync("alice", default)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteUser("alice", default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Users");
        _adminMock.Verify(a => a.DeleteUserAsync("alice", default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    // ── VoiceComm servers ─────────────────────────────────────────────────────

    [Fact]
    public async Task VoiceCommServers_GET_ReturnsView_WithList()
    {
        var servers = new List<VoiceCommServer>
        {
            new() { ServerId = 1, Name = "TeamSpeak", Addr = "ts.example.com",   ServerType = 0 },
            new() { ServerId = 2, Name = "Ventrilo",  Addr = "vent.example.com", ServerType = 1 }
        };
        _adminMock.Setup(a => a.GetVoiceCommServersAsync(default)).ReturnsAsync(servers);

        var result = await _controller.VoiceCommServers(default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeAssignableTo<IReadOnlyList<VoiceCommServer>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public void CreateVoiceCommServer_GET_ReturnsViewWithEmptyModel()
    {
        var result = _controller.CreateVoiceCommServer();

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<VoiceCommServer>().Subject;
        model.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVoiceCommServer_POST_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.CreateVoiceCommServer(new VoiceCommServer(), default);

        result.Should().BeOfType<ViewResult>();
        _adminMock.Verify(a => a.AddVoiceCommServerAsync(It.IsAny<VoiceCommServer>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateVoiceCommServer_POST_ValidModel_RedirectsAndCallsService()
    {
        _adminMock.Setup(a => a.AddVoiceCommServerAsync(It.IsAny<VoiceCommServer>(), default))
                  .Returns(Task.CompletedTask);

        var model = new VoiceCommServer { Name = "MyTS", Addr = "ts.example.com", ServerType = 0, UdpPort = 9987, QueryPort = 10011 };

        var result = await _controller.CreateVoiceCommServer(model, default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("VoiceCommServers");
        _adminMock.Verify(a => a.AddVoiceCommServerAsync(
            It.Is<VoiceCommServer>(s => s.Name == "MyTS"), default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task EditVoiceCommServer_GET_ReturnsNotFound_WhenServerDoesNotExist()
    {
        _adminMock.Setup(a => a.GetVoiceCommServerByIdAsync(99, default))
                  .ReturnsAsync((VoiceCommServer?)null);

        var result = await _controller.EditVoiceCommServer(99, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditVoiceCommServer_GET_ReturnsView_WhenServerExists()
    {
        var server = new VoiceCommServer { ServerId = 1, Name = "MyTS", Addr = "ts.example.com", ServerType = 0 };
        _adminMock.Setup(a => a.GetVoiceCommServerByIdAsync(1, default)).ReturnsAsync(server);

        var result = await _controller.EditVoiceCommServer(1, default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<VoiceCommServer>().Subject;
        model.Name.Should().Be("MyTS");
    }

    [Fact]
    public async Task EditVoiceCommServer_POST_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.EditVoiceCommServer(1, new VoiceCommServer(), default);

        result.Should().BeOfType<ViewResult>();
        _adminMock.Verify(a => a.UpdateVoiceCommServerAsync(It.IsAny<VoiceCommServer>(), default), Times.Never);
    }

    [Fact]
    public async Task EditVoiceCommServer_POST_ValidModel_SetsServerIdAndRedirects()
    {
        _adminMock.Setup(a => a.UpdateVoiceCommServerAsync(It.IsAny<VoiceCommServer>(), default))
                  .Returns(Task.CompletedTask);

        var model = new VoiceCommServer { Name = "Updated", Addr = "ts2.example.com", ServerType = 0 };

        var result = await _controller.EditVoiceCommServer(1, model, default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("VoiceCommServers");
        _adminMock.Verify(a => a.UpdateVoiceCommServerAsync(
            It.Is<VoiceCommServer>(s => s.ServerId == 1 && s.Name == "Updated"), default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteVoiceCommServer_POST_RedirectsAndCallsService()
    {
        _adminMock.Setup(a => a.DeleteVoiceCommServerAsync(5, default)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteVoiceCommServer(5, default);

        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("VoiceCommServers");
        _adminMock.Verify(a => a.DeleteVoiceCommServerAsync(5, default), Times.Once);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    // ── DaemonControl ─────────────────────────────────────────────────────────

    [Fact]
    public void DaemonControl_GET_ReturnsViewWithDefaultModel()
    {
        var result = _controller.DaemonControl();

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<DaemonControlViewModel>().Subject;
        model.Host.Should().Be("localhost");
        model.Port.Should().Be(27500);
        model.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task DaemonControl_POST_InvalidCommandIndex_ReturnsViewWithError()
    {
        _controller.ControllerContext = MakeHttpContext();
        var model = new DaemonControlViewModel { Host = "localhost", Port = 27500, Command = 99 };

        var result = await _controller.DaemonControl(model, default);

        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    // ── ResetCollations ───────────────────────────────────────────────────────

    [Fact]
    public void ResetCollations_GET_ReturnsViewWithEmptyModel()
    {
        var result = _controller.ResetCollations();

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<ResetCollationsViewModel>().Subject;
        model.Log.Should().BeNull();
    }

    [Fact]
    public async Task ResetCollations_POST_RunMode_CallsServiceAndSetsSuccessMessage()
    {
        var log = new List<string> { "Altered table hlstats_Players", "Altered table hlstats_Clans" };
        _adminMock.Setup(a => a.ResetCollationsAsync(false, default)).ReturnsAsync(log);

        var model = new ResetCollationsViewModel { PrintOnly = false };
        var result = await _controller.ResetCollations(model, default);

        var returned = result.Should().BeOfType<ViewResult>().Subject
                             .Model.Should().BeOfType<ResetCollationsViewModel>().Subject;
        returned.Log.Should().HaveCount(2);
        _controller.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task ResetCollations_POST_PrintMode_CallsServiceAndDoesNotSetSuccessMessage()
    {
        var log = new List<string> { "ALTER TABLE hlstats_Players CONVERT TO CHARACTER SET utf8mb4;" };
        _adminMock.Setup(a => a.ResetCollationsAsync(true, default)).ReturnsAsync(log);

        var model = new ResetCollationsViewModel { PrintOnly = true };
        var result = await _controller.ResetCollations(model, default);

        var returned = result.Should().BeOfType<ViewResult>().Subject
                             .Model.Should().BeOfType<ResetCollationsViewModel>().Subject;
        returned.Log.Should().HaveCount(1);
        _controller.TempData.ContainsKey("Success").Should().BeFalse();
    }
}
