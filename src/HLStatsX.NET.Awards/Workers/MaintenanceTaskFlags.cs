namespace HLStatsX.NET.Awards.Workers;

// Parsed set of maintenance tasks to run. Defaults match the Perl script's default: -iarp.
internal record MaintenanceTaskFlags(
    bool Inactive,
    bool Awards,
    bool Ribbons,
    bool Prune,
    bool Clans,
    bool Optimize,
    bool GeoIp,
    bool Resolve)
{
    public static readonly MaintenanceTaskFlags Default = new(true, true, true, true, false, false, false, false);
    public static readonly MaintenanceTaskFlags All    = new(true, true, true, true, true,  true,  true,  true);

    // Parses command-line args (e.g. Environment.GetCommandLineArgs()) into a task set.
    // --all             → all tasks
    // <no specific>     → default set (inactive, awards, ribbons, prune)
    // one or more flags → only those flags
    internal static MaintenanceTaskFlags FromArgs(string[] args)
    {
        if (args.Contains("--all")) return All;

        bool inactive = args.Contains("--inactive");
        bool awards   = args.Contains("--awards");
        bool ribbons  = args.Contains("--ribbons");
        bool prune    = args.Contains("--prune");
        bool clans    = args.Contains("--clans");
        bool optimize = args.Contains("--optimize");
        bool geoip    = args.Contains("--geoip");
        bool resolve  = args.Contains("--resolve");

        if (!inactive && !awards && !ribbons && !prune && !clans && !optimize && !geoip && !resolve)
            return Default;

        return new MaintenanceTaskFlags(inactive, awards, ribbons, prune, clans, optimize, geoip, resolve);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Inactive) parts.Add("inactive");
        if (Awards)   parts.Add("awards");
        if (Ribbons)  parts.Add("ribbons");
        if (Prune)    parts.Add("prune");
        if (Clans)    parts.Add("clans");
        if (Optimize) parts.Add("optimize");
        if (GeoIp)    parts.Add("geoip");
        if (Resolve)  parts.Add("resolve");
        return parts.Count > 0 ? string.Join(", ", parts) : "none";
    }
}
