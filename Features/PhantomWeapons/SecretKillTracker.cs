namespace Phantom;

public sealed class SecretKillTracker : IDisposable
{
    private static readonly string[] KillKeywords =
    {
        "打倒",
        "击倒",
        "讨伐",
        "消灭",
        "defeat",
        "defeated",
        "slay",
        "slain",
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
        if (TryAutoMarkSecretDuty(text))
        {
            return;
        }

        if (!LooksLikeKillMessage(text))
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

    private bool TryAutoMarkSecretDuty(string text)
    {
        if (!LooksLikeSecretDutyMessage(text))
        {
            return false;
        }

        foreach (var group in PhantomWeaponGuide.SecretDutyGroups)
        {
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

    private static bool LooksLikeKillMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return KillKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
