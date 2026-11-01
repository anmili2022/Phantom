using Newtonsoft.Json.Linq;

namespace Phantom;

/// <summary>One hunting log monster spawn point, in map coordinates.</summary>
public sealed record HuntingLogLocation(uint TerritoryType, uint MapId, float X, float Y);

/// <summary>One monster requirement inside a hunting log entry.</summary>
public sealed record HuntingLogMonster(uint NameId, int Required, IReadOnlyList<HuntingLogLocation> Locations);

/// <summary>One hunting log entry ("entry 01" of a rank), containing 1-4 monsters.</summary>
public sealed record HuntingLogEntry(int RankIndex, int EntryIndex, IReadOnlyList<HuntingLogMonster> Monsters);

public sealed record HuntingLogRank(int Index, IReadOnlyList<HuntingLogEntry> Entries);

public sealed record HuntingLogJob(string Key, int JobId, IReadOnlyList<HuntingLogRank> Ranks)
{
    public IEnumerable<HuntingLogEntry> Entries => Ranks.SelectMany(rank => rank.Entries);

    public int EntryCount => Ranks.Sum(rank => rank.Entries.Count);
}

/// <summary>Static hunting log data (monster BNpcName ids, counts and spawn points) exported per job and rank.</summary>
public static class HuntingLogGuide
{
    private const string ResourceName = "Phantom.HuntingLog";

    // Order matches the in-game hunting log list: 9 ARR classes, then the three grand companies.
    private static readonly string[] JobOrder =
    [
        "1", "2", "3", "4", "5", "6", "7", "26", "29",
        "10001", "10002", "10003",
    ];

    private static readonly Dictionary<string, string> JobNames = new(StringComparer.Ordinal);
    private static readonly Dictionary<uint, string> MonsterNames = new();
    private static readonly Dictionary<uint, string> TerritoryNames = new();

    private static readonly Lazy<IReadOnlyList<HuntingLogJob>> JobsLazy =
        new(LoadJobs, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<HuntingLogJob> Jobs => JobsLazy.Value;

    public static HuntingLogJob? FindJob(string jobKey)
        => Jobs.FirstOrDefault(job => job.Key == jobKey);

    public static string GetJobName(string jobKey)
    {
        if (JobNames.TryGetValue(jobKey, out var cached))
        {
            return cached;
        }

        var name = jobKey switch
        {
            "10001" => "黑涡团",
            "10002" => "双蛇党",
            "10003" => "恒辉队",
            _ => GetClassJobName(jobKey),
        };

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"职业 {jobKey}";
        }

        JobNames[jobKey] = name;
        return name;
    }

    public static string GetEntryName(HuntingLogJob job, HuntingLogEntry entry)
        => $"{GetJobName(job.Key)} {job.Ranks.Take(entry.RankIndex).Sum(rank => rank.Entries.Count) + entry.EntryIndex + 1:00}";

    public static string GetMonsterName(uint nameId)
    {
        if (MonsterNames.TryGetValue(nameId, out var cached))
        {
            return cached;
        }

        var name = string.Empty;
        var sheet = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>();
        if (sheet.TryGetRow(nameId, out var row))
        {
            name = row.Singular.ExtractText();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"怪物 {nameId}";
        }

        MonsterNames[nameId] = name;
        return name;
    }

    public static string GetLocationName(HuntingLogLocation location)
    {
        if (TerritoryNames.TryGetValue(location.TerritoryType, out var cached))
        {
            return cached;
        }

        var name = string.Empty;
        var sheet = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>();
        if (sheet.TryGetRow(location.TerritoryType, out var row))
        {
            name = row.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"地图 {location.TerritoryType}";
        }

        TerritoryNames[location.TerritoryType] = name;
        return name;
    }

    private static string GetClassJobName(string jobKey)
    {
        if (!int.TryParse(jobKey, out var jobId) || jobId <= 0)
        {
            return string.Empty;
        }

        var sheet = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>();
        return sheet.TryGetRow((uint)jobId, out var row) ? row.Name.ExtractText() : string.Empty;
    }

    private static IReadOnlyList<HuntingLogJob> LoadJobs()
    {
        using var stream = typeof(HuntingLogGuide).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var root = JObject.Parse(reader.ReadToEnd());
        if (root["Data"]?["JobRanks"] is not JObject jobRanks)
        {
            throw new InvalidOperationException("Hunting log data is missing Data.JobRanks.");
        }

        var jobs = new List<HuntingLogJob>();
        foreach (var jobKey in JobOrder)
        {
            if (jobRanks[jobKey] is not JArray ranks)
            {
                continue;
            }

            var rankList = new List<HuntingLogRank>();
            for (var rankIndex = 0; rankIndex < ranks.Count; rankIndex++)
            {
                var entries = new List<HuntingLogEntry>();
                var tasks = ranks[rankIndex]?["Tasks"] as JArray;
                if (tasks != null)
                {
                    for (var entryIndex = 0; entryIndex < tasks.Count; entryIndex++)
                    {
                        var monsters = new List<HuntingLogMonster>();
                        foreach (var monsterToken in tasks[entryIndex]?["Monsters"] as JArray ?? new JArray())
                        {
                            var nameId = monsterToken["ID"]?.Value<uint>() ?? 0;
                            if (nameId == 0)
                            {
                                continue;
                            }

                            var required = Math.Max(1, monsterToken["Count"]?.Value<int>() ?? 1);
                            var locations = new List<HuntingLogLocation>();
                            foreach (var locationToken in monsterToken["Locations"] as JArray ?? new JArray())
                            {
                                var territory = locationToken["Terri"]?.Value<uint>() ?? 0;
                                var mapId = locationToken["Map"]?.Value<uint>() ?? 0;
                                if (territory == 0 || mapId == 0)
                                {
                                    continue;
                                }

                                locations.Add(new HuntingLogLocation(
                                    territory,
                                    mapId,
                                    locationToken["xCoord"]?.Value<float>() ?? 0f,
                                    locationToken["yCoord"]?.Value<float>() ?? 0f));
                            }

                            monsters.Add(new HuntingLogMonster(nameId, required, locations));
                        }

                        entries.Add(new HuntingLogEntry(rankIndex, entryIndex, monsters));
                    }
                }

                rankList.Add(new HuntingLogRank(rankIndex, entries));
            }

            if (rankList.Count > 0)
            {
                jobs.Add(new HuntingLogJob(jobKey, int.Parse(jobKey), rankList));
            }
        }

        return jobs;
    }
}
