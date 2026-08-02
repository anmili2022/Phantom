namespace Phantom;

public sealed class SecretKillTracker : IDisposable
{
    private static readonly (string MemoryName, IReadOnlyList<string> Zones)[] ExplorationMemoryGroups =
    {
        ("场景探索：尤卡图拉尔", new[] { "奥阔帕恰山", "克扎玛乌卡湿地", "亚克特尔树海" }),
        ("场景探索：萨卡图拉尔", new[] { "夏劳尼荒野", "遗产之地" }),
        ("场景探索：无失世界", new[] { "活着的记忆" }),
        ("场景探索：克扎玛乌卡湿地", new[] { "克扎玛乌卡湿地" }),
        ("场景探索：亚克特尔树海", new[] { "亚克特尔树海" }),
        ("场景探索：夏劳尼荒野", new[] { "夏劳尼荒野" }),
        ("场景探索：遗产之地", new[] { "遗产之地" }),
    };

    private readonly PluginConfiguration configuration;

    public SecretKillTracker(PluginConfiguration configuration)
    {
        this.configuration = configuration;
        DalamudApi.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        DalamudApi.ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(object message)
    {
        if (!configuration.Enabled || !configuration.AutoMarkSecretKills)
        {
            return;
        }

        var text = ExtractChatMessageText(message);
        if (TryAutoMarkSecretAllComplete(text))
        {
            return;
        }

        if (TryAutoMarkSecretDuty(text))
        {
            return;
        }

        if (!LooksLikeSecretTargetMessage(text))
        {
            return;
        }

        var territory = DalamudApi.ClientState.TerritoryType;
        foreach (var target in PhantomWeaponGuide.SecretTargets.Where(target => target.TerritoryType == territory))
        {
            if (configuration.CompletedTasks.Contains(target.Key)
                || !text.Contains(target.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            configuration.CompletedTasks.Add(target.Key);
            configuration.Save();
            DalamudApi.Log.Information("Auto-marked Secret target complete: {Target}", target.Name);
            return;
        }
    }

    private bool TryAutoMarkSecretAllComplete(string text)
    {
        if (string.IsNullOrWhiteSpace(text)
            || !text.Contains("战斗的记忆", StringComparison.OrdinalIgnoreCase)
            || !text.Contains("完成", StringComparison.OrdinalIgnoreCase)
            || !text.Contains("所有项目", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var changed = false;

        foreach (var target in PhantomWeaponGuide.SecretTargets)
        {
            changed |= configuration.CompletedTasks.Add(target.Key);
        }

        foreach (var group in PhantomWeaponGuide.SecretDutyGroups)
        {
            foreach (var duty in group.Duties)
            {
                changed |= configuration.CompletedTasks.Add(duty.Key);
            }
        }

        foreach (var territoryType in PhantomWeaponGuide.SecretTargets.Select(target => target.TerritoryType).Distinct())
        {
            var fateKey = GetSecretFateKey(territoryType);
            if (configuration.Progress.GetValueOrDefault(fateKey) < 5)
            {
                configuration.Progress[fateKey] = 5;
                changed = true;
            }
        }

        if (changed)
        {
            configuration.Save();
            DalamudApi.Log.Information("Auto-marked all Secret stage items complete.");
        }

        return true;
    }

    private bool TryAutoMarkSecretDuty(string text)
    {
        if (!LooksLikeSecretDutyMessage(text))
        {
            return false;
        }

        if (TryAutoMarkSecretExploration(text))
        {
            return true;
        }

        foreach (var group in PhantomWeaponGuide.SecretDutyGroups)
        {
            if (MatchesDutyGroup(text, group))
            {
                var changed = false;
                foreach (var duty in group.Duties)
                {
                    changed |= configuration.CompletedTasks.Add(duty.Key);
                }

                if (changed)
                {
                    configuration.Save();
                    DalamudApi.Log.Information("Auto-marked Secret duty group complete: {Group}", group.Name);
                }

                return true;
            }

            foreach (var duty in group.Duties)
            {
                if (configuration.CompletedTasks.Contains(duty.Key) || !MatchesDuty(text, duty.Name))
                {
                    continue;
                }

                configuration.CompletedTasks.Add(duty.Key);
                configuration.Save();
                DalamudApi.Log.Information("Auto-marked Secret duty complete: {Duty}", duty.Name);
                return true;
            }
        }

        return false;
    }

    private bool TryAutoMarkSecretExploration(string text)
    {
        if (!text.Contains("所有项目", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var group in ExplorationMemoryGroups)
        {
            if (!text.Contains(group.MemoryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var changed = false;
            foreach (var zone in group.Zones)
            {
                var targets = PhantomWeaponGuide.SecretTargets
                    .Where(target => string.Equals(target.Zone, zone, StringComparison.Ordinal))
                    .ToArray();
                foreach (var target in targets)
                {
                    changed |= configuration.CompletedTasks.Add(target.Key);
                }

                foreach (var territoryType in targets.Select(target => target.TerritoryType).Distinct())
                {
                    var fateKey = GetSecretFateKey(territoryType);
                    if (configuration.Progress.GetValueOrDefault(fateKey) < 5)
                    {
                        configuration.Progress[fateKey] = 5;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                configuration.Save();
                DalamudApi.Log.Information("Auto-marked Secret exploration complete: {MemoryName}", group.MemoryName);
            }

            return true;
        }

        return false;
    }

    private static string GetSecretFateKey(uint territoryType)
        => $"secret-fate-{territoryType}";

    private static bool MatchesDutyGroup(string text, PhantomWeaponDutyGroup group)
    {
        return text.Contains("所有项目", StringComparison.OrdinalIgnoreCase)
               && (text.Contains(group.Name, StringComparison.OrdinalIgnoreCase)
                   || text.Contains(group.Name.Replace("迷宫或讨伐任务：", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeSecretDutyMessage(string text)
    {
        return !string.IsNullOrWhiteSpace(text)
               && text.Contains("战斗的记忆", StringComparison.OrdinalIgnoreCase)
               && text.Contains("完成", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDuty(string text, string dutyName)
    {
        if (text.Contains(dutyName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var shortName = dutyName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return !string.IsNullOrWhiteSpace(shortName)
               && text.Contains(shortName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeSecretTargetMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains("战斗的记忆", StringComparison.OrdinalIgnoreCase)
               && text.Contains("讨伐", StringComparison.OrdinalIgnoreCase)
               && text.Contains("只", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractChatMessageText(object message)
    {
        try
        {
            var messageProperty = message.GetType().GetProperty("Message");
            var value = messageProperty?.GetValue(message);
            var textValueProperty = value?.GetType().GetProperty("TextValue");
            return textValueProperty?.GetValue(value) as string
                   ?? value?.ToString()
                   ?? message.ToString()
                   ?? string.Empty;
        }
        catch
        {
            return message.ToString() ?? string.Empty;
        }
    }
}
