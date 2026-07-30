namespace Phantom;

public sealed class FateTracker : IDisposable
{
    private static readonly string[] GoldKeywords =
    {
        "最高评价",
        "gold",
        "gold rating",
    };

    private readonly PluginConfiguration configuration;
    private DateTime lastGoldFateUtc = DateTime.MinValue;

    public FateTracker(PluginConfiguration configuration)
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
        if (!configuration.Enabled)
        {
            return;
        }

        var text = ExtractChatMessageText(message);
        if (!LooksLikeGoldFateMessage(text))
        {
            return;
        }

        if ((DateTime.UtcNow - lastGoldFateUtc).TotalSeconds < 5)
        {
            return;
        }

        lastGoldFateUtc = DateTime.UtcNow;

        var territory = DalamudApi.ClientState.TerritoryType;
        var key = $"secret-fate-{territory}";
        var count = Math.Clamp(configuration.Progress.GetValueOrDefault(key), 0, 5);
        if (count >= 5)
        {
            return;
        }

        configuration.Progress[key] = count + 1;
        configuration.Save();
        DalamudApi.Log.Information("Auto-marked gold FATE ({Count}/5) in territory {Territory}.", count + 1, territory);
    }

    private static bool LooksLikeGoldFateMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return GoldKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
