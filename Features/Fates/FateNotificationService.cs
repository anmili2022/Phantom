using Dalamud.Game.ClientState.Fates;
using Dalamud.Plugin.Services;

namespace Phantom;

public sealed class FateNotificationService : IDisposable
{
    private sealed record ReminderState(int SentCount, DateTime NextReminderUtc);
    private sealed record NotificationTarget(string Name, string Zone, float MapX, float MapY, string? Source);

    private readonly PluginConfiguration configuration;
    private readonly EdgeTtsService edgeTts;
    private readonly Dictionary<uint, ReminderState> activeTargetFates = new();
    private DateTime lastCheckUtc = DateTime.MinValue;
    private string activeContext = string.Empty;
    private bool edgeTtsUnavailableLogged;

    public FateNotificationService(PluginConfiguration configuration, EdgeTtsService edgeTts)
    {
        this.configuration = configuration;
        this.edgeTts = edgeTts;
        DalamudApi.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudApi.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _ = framework;
        if (DateTime.UtcNow - lastCheckUtc < TimeSpan.FromSeconds(1))
        {
            return;
        }

        lastCheckUtc = DateTime.UtcNow;
        CheckTrackedFates();
    }

    private void CheckTrackedFates()
    {
        if (!configuration.Enabled || !configuration.ZodiacFateNotificationsEnabled)
        {
            Reset();
            return;
        }

        var territoryType = DalamudApi.ClientState.TerritoryType;
        var zodiacContext = GetZodiacContext();
        var context = territoryType.ToString();
        if (!string.Equals(context, activeContext, StringComparison.Ordinal))
        {
            activeContext = context;
            activeTargetFates.Clear();
        }

        var current = new HashSet<uint>();
        var now = DateTime.UtcNow;
        var repeatCount = Math.Clamp(configuration.ZodiacFateNotificationRepeatCount, 1, 10);
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuration.ZodiacFateNotificationIntervalSeconds, 5, 300));
        foreach (var fate in DalamudApi.FateTable
                     .Where(fate => fate != null && DalamudApi.FateTable.IsValid(fate))
                     .Where(fate => fate!.TerritoryType.RowId == territoryType)
                     .Where(fate => fate!.State is FateState.Preparing or FateState.Running)
                     .Where(fate => fate!.Progress < 100)
                     .Select(fate => fate!))
        {
            var target = FindTarget(fate, zodiacContext);
            if (target == null)
            {
                continue;
            }

            current.Add(fate.FateId);
            if (!activeTargetFates.TryGetValue(fate.FateId, out var reminder))
            {
                reminder = new ReminderState(0, now);
            }

            if (reminder.SentCount < repeatCount && now >= reminder.NextReminderUtc)
            {
                Notify(target);
                reminder = new ReminderState(reminder.SentCount + 1, now + interval);
            }

            activeTargetFates[fate.FateId] = reminder;
        }

        foreach (var fateId in activeTargetFates.Keys.Where(fateId => !current.Contains(fateId)).ToArray())
        {
            activeTargetFates.Remove(fateId);
        }
    }

    private NotificationTarget? FindTarget(IFate fate, ZodiacContext zodiacContext)
    {
        var manual = configuration.TrackedFates.FirstOrDefault(target =>
            target.FateId == fate.FateId || NamesMatch(target.Name, fate.Name.ToString()));
        if (manual != null)
        {
            if (manual.TerritoryType == 0 || string.IsNullOrWhiteSpace(manual.Zone))
            {
                manual = UpdateTrackedFateLocation(manual, fate);
            }
            return new NotificationTarget(manual.Name, manual.Zone, manual.MapX, manual.MapY, null);
        }

        if (!configuration.AutoTrackSelectedZodiacBookFates || zodiacContext.Book == null || zodiacContext.Progress == null)
        {
            return null;
        }

        var fateName = fate.Name.ToString();
        var objective = zodiacContext.Book.Fates.FirstOrDefault(candidate =>
            !zodiacContext.Progress.CompletedObjectives.Contains(candidate.Key)
            && NamesMatch(candidate.Name, fateName));
        return objective == null
            ? null
            : new NotificationTarget(objective.Name, objective.Zone, objective.MapX, objective.MapY, ZodiacGuide.GetFateBookAnnotations(objective.Name));
    }

    private void Notify(NotificationTarget target)
    {
        var coordinate = target.MapX > 0f ? $" ({target.MapX:0.0}, {target.MapY:0.0})" : string.Empty;
        var source = target.Source == null ? string.Empty : $" {target.Source}";
        var text = $"[Phantom] 关注的 FATE 出现：{target.Name} · {target.Zone}{coordinate}{source}";
        var sound = Math.Clamp(configuration.ZodiacFateNotificationSound, 0, 16);
        var command = sound == 0 ? $"/echo {text}" : $"/echo {text} <se.{sound}>";
        DalamudApi.Commands.ProcessCommand(command.Replace('\r', ' ').Replace('\n', ' '));

        if (!configuration.ZodiacFateNotificationEdgeTts)
        {
            return;
        }

        if (edgeTts.Speak($"关注的临危受命出现，{target.Name}"))
        {
            edgeTtsUnavailableLogged = false;
        }
        else if (!edgeTtsUnavailableLogged)
        {
            edgeTtsUnavailableLogged = true;
            DalamudApi.Log.Warning("FATE voice notification skipped because EdgeTTS.Dalamud is unavailable.");
        }
    }

    private TrackedFate UpdateTrackedFateLocation(TrackedFate tracked, IFate fate)
    {
        var map = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Map>()
            .FirstOrDefault(candidate => candidate.TerritoryType.RowId == fate.TerritoryType.RowId && candidate.SizeFactor > 0);
        var zone = map.RowId == 0 ? $"地图 {fate.TerritoryType.RowId}" : map.PlaceName.Value.Name.ExtractText();
        var mapX = map.RowId == 0 ? 0f : 0.02f * (map.OffsetX + 102400f / map.SizeFactor + fate.Position.X) + 1f;
        var mapY = map.RowId == 0 ? 0f : 0.02f * (map.OffsetY + 102400f / map.SizeFactor + fate.Position.Z) + 1f;
        var updated = new TrackedFate(tracked.FateId, fate.TerritoryType.RowId, tracked.Name, zone, mapX, mapY);
        var index = configuration.TrackedFates.IndexOf(tracked);
        if (index >= 0)
        {
            configuration.TrackedFates[index] = updated;
            configuration.Save();
        }
        return updated;
    }

    private ZodiacContext GetZodiacContext()
    {
        if (!configuration.AutoTrackSelectedZodiacBookFates)
        {
            return new ZodiacContext("disabled", null, null);
        }

        var characterKey = GetCurrentCharacterKey();
        if (characterKey.Length == 0
            || !configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            || !characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var progress)
            || string.IsNullOrWhiteSpace(progress.SelectedBookKey))
        {
            return new ZodiacContext("none", null, null);
        }

        var book = ZodiacGuide.AnimusBooks.FirstOrDefault(candidate => candidate.Key == progress.SelectedBookKey);
        return new ZodiacContext($"{characterKey}|{configuration.SelectedZodiacJobKey}|{progress.SelectedBookKey}", book, progress);
    }

    private static bool NamesMatch(string left, string right)
        => left.Contains(right, StringComparison.OrdinalIgnoreCase)
            || right.Contains(left, StringComparison.OrdinalIgnoreCase);

    private void Reset()
    {
        activeContext = string.Empty;
        activeTargetFates.Clear();
    }

    private static string GetCurrentCharacterKey()
    {
        var contentId = DalamudApi.PlayerState.ContentId;
        if (contentId != 0)
        {
            return contentId.ToString();
        }

        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null)
        {
            return string.Empty;
        }

        var world = player.HomeWorld.Value.Name.ExtractText();
        return string.IsNullOrWhiteSpace(world) ? player.Name.TextValue : $"{player.Name.TextValue}@{world}";
    }

    private sealed record ZodiacContext(string ContextKey, ZodiacBookGuide? Book, ZodiacJobProgress? Progress);
}
