using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Phantom;

/// <summary>Per-character hunting log progress, kill detection and navigation for the hunting log assistant.</summary>
public sealed class HuntingLogTracker : IDisposable
{
    private static readonly TimeSpan CombatWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DisappearWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan AutoTargetDuration = TimeSpan.FromMinutes(10);
    private static readonly Dictionary<string, int> EmptyCounts = new(StringComparer.Ordinal);

    // MonsterNoteManager.RankData slots (Honorific/Hunty "JobInMemory" order): classes first, then GCs, Rogue last.
    private static readonly Dictionary<int, int> MonsterNoteSlots = new()
    {
        [1] = 0,
        [2] = 1,
        [3] = 2,
        [4] = 3,
        [5] = 4,
        [6] = 5,
        [7] = 6,
        [26] = 7,
        [10001] = 8,
        [10002] = 9,
        [10003] = 10,
        [29] = 11,
    };

    private sealed class WatchedNpc
    {
        public uint NameId;
        public DateTime LastSeenUtc;
        public bool Counted;
    }

    private readonly PluginConfiguration configuration;
    private readonly VnavService vnav;
    private readonly Dictionary<nint, WatchedNpc> watched = new();
    private readonly HashSet<nint> seenAddresses = new();
    private readonly HashSet<uint> neededNameIds = new();

    private DateTime lastCombatUtc = DateTime.MinValue;
    private long lastScanTick;
    private uint lastTerritory;
    private bool neededDirty = true;
    private string lastJobKey = string.Empty;
    private string? pendingNextJobKey;

    private uint autoTargetNameId;
    private uint autoTargetTerritory;
    private DateTime autoTargetUntilUtc = DateTime.MinValue;
    private string autoTargetLabel = string.Empty;

    public HuntingLogTracker(PluginConfiguration configuration, VnavService vnav)
    {
        this.configuration = configuration;
        this.vnav = vnav;
        DalamudApi.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudApi.Framework.Update -= OnFrameworkUpdate;
        vnav.Stop();
    }

    public string CurrentTargetLabel => autoTargetNameId == 0 || DateTime.UtcNow >= autoTargetUntilUtc
        ? string.Empty
        : autoTargetLabel;

    public bool IsAutoCounting => configuration.HuntingLogAutoCount;

    public int GetKillCount(string jobKey, HuntingLogEntry entry, HuntingLogMonster monster)
        => GetCounts(create: false).GetValueOrDefault(BuildKey(jobKey, entry, monster.NameId));

    public bool IsMonsterComplete(string jobKey, HuntingLogEntry entry, HuntingLogMonster monster)
        => GetKillCount(jobKey, entry, monster) >= monster.Required;

    public bool IsEntryComplete(string jobKey, HuntingLogEntry entry)
        => entry.Monsters.All(monster => IsMonsterComplete(jobKey, entry, monster));

    public (int Completed, int Total) GetJobProgress(HuntingLogJob job)
    {
        var completed = job.Entries.Count(entry => IsEntryComplete(job.Key, entry));
        return (completed, job.EntryCount);
    }

    public int GetRankCompleted(HuntingLogJob job, HuntingLogRank rank)
        => rank.Entries.Count(entry => IsEntryComplete(job.Key, entry));

    /// <summary>Index of the highest hunting log rank the character has unlocked (0-based), or -1 when unknown.</summary>
    public unsafe int GetUnlockedRankIndex(string jobKey)
    {
        if (DalamudApi.PlayerState.ContentId == 0)
        {
            return -1;
        }

        if (!int.TryParse(jobKey, out var jobId) || !MonsterNoteSlots.TryGetValue(jobId, out var slot))
        {
            return -1;
        }

        var manager = MonsterNoteManager.Instance();
        if (manager == null)
        {
            return -1;
        }

        return manager->RankData[slot].Rank;
    }

    public bool IsRankUnlocked(HuntingLogJob job, int rankIndex)
    {
        if (job.JobId is 10001 or 10002 or 10003)
        {
            var grandCompany = (int)DalamudApi.PlayerState.GrandCompany.RowId;
            if (grandCompany == 0 || job.JobId != 10000 + grandCompany)
            {
                return false;
            }
        }

        var unlockedRank = GetUnlockedRankIndex(job.Key);
        return unlockedRank < 0 || rankIndex <= unlockedRank;
    }

    /// <summary>Reads the character's real hunting log state from MonsterNoteManager and rewrites stored progress.</summary>
    public unsafe (int Jobs, int Completed, int Total) SyncFromGame()
    {
        var manager = MonsterNoteManager.Instance();
        if (manager == null)
        {
            return (0, 0, 0);
        }

        var counts = GetCounts(create: true);
        if (ReferenceEquals(counts, EmptyCounts))
        {
            return (0, 0, 0);
        }

        var jobsSynced = 0;
        var completedEntries = 0;
        var totalEntries = 0;
        foreach (var job in HuntingLogGuide.Jobs)
        {
            if (!MonsterNoteSlots.TryGetValue(job.JobId, out var slot))
            {
                continue;
            }

            if ((job.JobId is 10001 or 10002 or 10003) && !IsRankUnlocked(job, 0))
            {
                // Only the character's own grand company hunting log is readable in game.
                continue;
            }

            var rankInfo = manager->RankData[slot];
            var currentRank = rankInfo.Rank;

            var prefix = $"{job.Key}|";
            foreach (var key in counts.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                counts.Remove(key);
            }

            jobsSynced++;
            for (var rankIndex = 0; rankIndex < job.Ranks.Count; rankIndex++)
            {
                var rank = job.Ranks[rankIndex];
                for (var entryIndex = 0; entryIndex < rank.Entries.Count; entryIndex++)
                {
                    var entry = rank.Entries[entryIndex];
                    totalEntries++;
                    var entryComplete = true;
                    for (var monsterIndex = 0; monsterIndex < entry.Monsters.Count; monsterIndex++)
                    {
                        var monster = entry.Monsters[monsterIndex];
                        int value;
                        if (rankIndex < currentRank)
                        {
                            value = monster.Required;
                        }
                        else if (rankIndex == currentRank
                                 && entryIndex < rankInfo.RankData.Length
                                 && monsterIndex < rankInfo.RankData[entryIndex].Counts.Length)
                        {
                            value = Math.Min(monster.Required, rankInfo.RankData[entryIndex].Counts[monsterIndex]);
                        }
                        else
                        {
                            value = 0;
                        }

                        if (value > 0)
                        {
                            counts[BuildKey(job.Key, entry, monster.NameId)] = value;
                        }

                        if (value < monster.Required)
                        {
                            entryComplete = false;
                        }
                    }

                    if (entryComplete)
                    {
                        completedEntries++;
                    }
                }
            }
        }

        neededDirty = true;
        watched.Clear();
        configuration.Save();
        return (jobsSynced, completedEntries, totalEntries);
    }

    /// <summary>Next incomplete monsters of a job, in rank/entry order.</summary>
    public IReadOnlyList<(HuntingLogEntry Entry, HuntingLogMonster Monster)> GetNextTargets(
        string jobKey,
        int max,
        bool unlockedOnly = false,
        int onlyRank = -1)
    {
        var job = HuntingLogGuide.FindJob(jobKey);
        if (job == null || max <= 0)
        {
            return [];
        }

        var targets = new List<(HuntingLogEntry Entry, HuntingLogMonster Monster)>();
        foreach (var entry in job.Entries)
        {
            if (onlyRank >= 0 && entry.RankIndex != onlyRank)
            {
                continue;
            }

            if (unlockedOnly && !IsRankUnlocked(job, entry.RankIndex))
            {
                continue;
            }

            foreach (var monster in entry.Monsters)
            {
                if (IsMonsterComplete(jobKey, entry, monster))
                {
                    continue;
                }

                targets.Add((entry, monster));
                if (targets.Count >= max)
                {
                    return targets;
                }
            }
        }

        return targets;
    }

    public void SetKillCount(string jobKey, HuntingLogEntry entry, HuntingLogMonster monster, int value)
    {
        var counts = GetCounts(create: true);
        var key = BuildKey(jobKey, entry, monster.NameId);
        var clamped = Math.Clamp(value, 0, monster.Required);
        if (clamped <= 0)
        {
            counts.Remove(key);
        }
        else
        {
            counts[key] = clamped;
        }

        neededDirty = true;
        configuration.Save();
        NotifyProgressChanged(jobKey, entry);
    }

    public void CompleteEntry(string jobKey, HuntingLogEntry entry)
    {
        var counts = GetCounts(create: true);
        foreach (var monster in entry.Monsters)
        {
            counts[BuildKey(jobKey, entry, monster.NameId)] = monster.Required;
        }

        neededDirty = true;
        configuration.Save();
        NotifyProgressChanged(jobKey, entry);
    }

    public void ResetEntry(string jobKey, HuntingLogEntry entry)
    {
        var counts = GetCounts(create: true);
        foreach (var monster in entry.Monsters)
        {
            counts.Remove(BuildKey(jobKey, entry, monster.NameId));
        }

        neededDirty = true;
        configuration.Save();
    }

    public bool NavigateToMonster(HuntingLogJob job, HuntingLogEntry entry, HuntingLogMonster monster, HuntingLogLocation? preferred = null)
    {
        var resolved = new List<(HuntingLogLocation Location, Vector3 Position)>();
        foreach (var location in monster.Locations)
        {
            if (vnav.TryResolveMapLinkPosition(location.TerritoryType, location.MapId, location.X, location.Y, out var position))
            {
                resolved.Add((location, position));
            }
        }

        if (resolved.Count == 0)
        {
            PrintEcho($"讨伐笔记：无法解析 {HuntingLogGuide.GetMonsterName(monster.NameId)} 的坐标。");
            return false;
        }

        var currentTerritory = DalamudApi.ClientState.TerritoryType;
        var player = DalamudApi.ObjectTable.LocalPlayer;
        var chosen = resolved[0];
        var preferredIndex = preferred == null ? -1 : resolved.FindIndex(candidate => candidate.Location == preferred);
        if (preferredIndex >= 0)
        {
            chosen = resolved[preferredIndex];
        }
        else if (player != null)
        {
            var sameTerritory = resolved
                .Where(candidate => candidate.Location.TerritoryType == currentTerritory)
                .OrderBy(candidate => Vector2.Distance(
                    new Vector2(player.Position.X, player.Position.Z),
                    new Vector2(candidate.Position.X, candidate.Position.Z)))
                .ToList();
            if (sameTerritory.Count > 0)
            {
                chosen = sameTerritory[0];
            }
        }

        vnav.NavigateToGroundTarget(chosen.Location.TerritoryType, chosen.Position);
        autoTargetNameId = monster.NameId;
        autoTargetTerritory = chosen.Location.TerritoryType;
        autoTargetUntilUtc = DateTime.UtcNow + AutoTargetDuration;
        autoTargetLabel = $"{HuntingLogGuide.GetEntryName(job, entry)} · {HuntingLogGuide.GetMonsterName(monster.NameId)}";
        return true;
    }

    public bool NavigateToNextTarget(string jobKey)
    {
        var job = HuntingLogGuide.FindJob(jobKey);
        if (job == null)
        {
            return false;
        }

        foreach (var entry in job.Entries)
        {
            foreach (var monster in entry.Monsters)
            {
                if (IsMonsterComplete(jobKey, entry, monster) || monster.Locations.Count == 0)
                {
                    continue;
                }

                return NavigateToMonster(job, entry, monster);
            }
        }

        PrintEcho("讨伐笔记：该职业的讨伐目标已全部完成。");
        return false;
    }

    public void StopNavigation()
    {
        vnav.Stop();
        pendingNextJobKey = null;
        autoTargetUntilUtc = DateTime.MinValue;
        autoTargetNameId = 0;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _ = framework;
        if (!configuration.Enabled)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (DalamudApi.Condition[ConditionFlag.InCombat])
        {
            lastCombatUtc = now;
        }

        UpdateAutoTarget(now);
        UpdatePendingNextTarget();

        if (!configuration.HuntingLogAutoCount)
        {
            watched.Clear();
            return;
        }

        var tick = Environment.TickCount64;
        if (tick - lastScanTick < 100)
        {
            return;
        }

        lastScanTick = tick;
        UpdateKillCounting(now);
    }

    private void UpdatePendingNextTarget()
    {
        if (pendingNextJobKey == null)
        {
            return;
        }

        if (!configuration.HuntingLogAutoNavigateNext)
        {
            pendingNextJobKey = null;
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.InCombat]
            || DalamudApi.Condition[ConditionFlag.BetweenAreas]
            || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            return;
        }

        var jobKey = pendingNextJobKey;
        pendingNextJobKey = null;
        NavigateToNextTarget(jobKey);
    }

    private void UpdateKillCounting(DateTime now)
    {
        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null || player.IsDead)
        {
            watched.Clear();
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.BetweenAreas] || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            watched.Clear();
            return;
        }

        var territory = DalamudApi.ClientState.TerritoryType;
        if (territory != lastTerritory)
        {
            lastTerritory = territory;
            watched.Clear();
            neededDirty = true;
        }

        var jobKey = configuration.HuntingLogSelectedJobKey;
        var job = HuntingLogGuide.FindJob(jobKey);
        if (job == null)
        {
            return;
        }

        if (jobKey != lastJobKey)
        {
            lastJobKey = jobKey;
            neededDirty = true;
            watched.Clear();
        }

        if (neededDirty)
        {
            neededDirty = false;
            neededNameIds.Clear();
            foreach (var entry in job.Entries)
            {
                foreach (var monster in entry.Monsters)
                {
                    if (!IsMonsterComplete(jobKey, entry, monster))
                    {
                        neededNameIds.Add(monster.NameId);
                    }
                }
            }
        }

        if (neededNameIds.Count == 0)
        {
            watched.Clear();
            return;
        }

        var playerPosition = player.Position;
        seenAddresses.Clear();
        foreach (var obj in DalamudApi.ObjectTable)
        {
            if (obj is not IBattleNpc npc || npc.NameId == 0 || !neededNameIds.Contains(npc.NameId))
            {
                continue;
            }

            var address = npc.Address;
            if (!seenAddresses.Add(address))
            {
                continue;
            }

            if (Vector3.Distance(playerPosition, npc.Position) > 45f)
            {
                continue;
            }

            if (npc.IsDead)
            {
                if (watched.TryGetValue(address, out var deadWatch))
                {
                    deadWatch.LastSeenUtc = now;
                    if (!deadWatch.Counted)
                    {
                        deadWatch.Counted = true;
                        CountKill(jobKey, npc.NameId);
                    }
                }
                else
                {
                    watched[address] = new WatchedNpc { NameId = npc.NameId, LastSeenUtc = now, Counted = true };
                    if (now - lastCombatUtc <= CombatWindow)
                    {
                        CountKill(jobKey, npc.NameId);
                    }
                }

                continue;
            }

            if (watched.TryGetValue(address, out var watch))
            {
                watch.LastSeenUtc = now;
            }
            else
            {
                watched[address] = new WatchedNpc { NameId = npc.NameId, LastSeenUtc = now };
            }
        }

        foreach (var (address, watch) in watched.ToArray())
        {
            if (seenAddresses.Contains(address))
            {
                continue;
            }

            watched.Remove(address);
            if (watch.Counted || now - watch.LastSeenUtc > DisappearWindow || now - lastCombatUtc > CombatWindow)
            {
                continue;
            }

            CountKill(jobKey, watch.NameId);
        }
    }

    private void CountKill(string jobKey, uint nameId)
    {
        var job = HuntingLogGuide.FindJob(jobKey);
        if (job == null)
        {
            return;
        }

        var counts = GetCounts(create: true);
        var changed = false;
        var completedEntries = new List<HuntingLogEntry>();
        foreach (var entry in job.Entries)
        {
            var updated = false;
            foreach (var monster in entry.Monsters)
            {
                if (monster.NameId != nameId)
                {
                    continue;
                }

                var key = BuildKey(jobKey, entry, monster.NameId);
                var current = counts.GetValueOrDefault(key);
                if (current >= monster.Required)
                {
                    continue;
                }

                counts[key] = current + 1;
                updated = true;
                changed = true;
            }

            if (updated && IsEntryComplete(jobKey, entry))
            {
                completedEntries.Add(entry);
            }
        }

        if (!changed)
        {
            return;
        }

        neededDirty = true;
        configuration.Save();
        if (completedEntries.Count > 0 && configuration.HuntingLogAutoNavigateNext)
        {
            pendingNextJobKey = jobKey;
        }
    }

    private void UpdateAutoTarget(DateTime now)
    {
        if (!configuration.HuntingLogAutoTarget || autoTargetNameId == 0 || now >= autoTargetUntilUtc)
        {
            return;
        }

        if (DalamudApi.ClientState.TerritoryType != autoTargetTerritory || DalamudApi.Condition[ConditionFlag.InCombat])
        {
            return;
        }

        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null || player.IsDead)
        {
            return;
        }

        var currentTarget = DalamudApi.TargetManager.Target;
        if (currentTarget is ICharacter { NameId: var currentNameId } && currentNameId == autoTargetNameId)
        {
            return;
        }

        IBattleNpc? nearest = null;
        var nearestDistance = float.MaxValue;
        foreach (var obj in DalamudApi.ObjectTable)
        {
            if (obj is not IBattleNpc npc || npc.NameId != autoTargetNameId || npc.IsDead)
            {
                continue;
            }

            var distance = Vector3.Distance(player.Position, npc.Position);
            if (distance > 30f || distance >= nearestDistance)
            {
                continue;
            }

            nearest = npc;
            nearestDistance = distance;
        }

        if (nearest != null)
        {
            DalamudApi.TargetManager.Target = nearest;
        }
    }

    private void NotifyProgressChanged(string jobKey, HuntingLogEntry entry)
    {
        if (!configuration.HuntingLogAutoNavigateNext || pendingNextJobKey != null)
        {
            return;
        }

        if (IsEntryComplete(jobKey, entry))
        {
            pendingNextJobKey = jobKey;
        }
    }

    private Dictionary<string, int> GetCounts(bool create)
    {
        var characterKey = GetCharacterKey();
        if (characterKey.Length == 0)
        {
            return EmptyCounts;
        }

        if (configuration.HuntingLogKillCountsByCharacter.TryGetValue(characterKey, out var counts))
        {
            return counts;
        }

        if (!create)
        {
            return EmptyCounts;
        }

        counts = new Dictionary<string, int>(StringComparer.Ordinal);
        configuration.HuntingLogKillCountsByCharacter[characterKey] = counts;
        return counts;
    }

    private static string BuildKey(string jobKey, HuntingLogEntry entry, uint nameId)
        => $"{jobKey}|{entry.RankIndex}.{entry.EntryIndex}|{nameId}";

    private static string GetCharacterKey()
    {
        var contentId = DalamudApi.PlayerState.ContentId;
        if (contentId != 0)
        {
            return contentId.ToString();
        }

        var player = DalamudApi.ObjectTable.LocalPlayer;
        return player?.Name.TextValue ?? string.Empty;
    }

    private static void PrintEcho(string message)
    {
        try
        {
            DalamudApi.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry
            {
                Type = Dalamud.Game.Text.XivChatType.Echo,
                Message = $"[讨伐笔记] {message}",
            });
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to print hunting log status.");
        }
    }
}
