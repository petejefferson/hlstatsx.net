using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record CountryLeaderboardViewModel(
    PagedResult<CountryLeaderboardRow> Countries,
    string Game,
    string SortBy,
    bool Descending,
    int MinMembers,
    int TotalCountries
);

public record CountryProfileViewModel(
    CountryProfile Profile,
    PagedResult<CountryMember> Members,
    string Game,
    string SortBy,
    bool Descending
);
