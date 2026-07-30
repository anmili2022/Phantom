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
