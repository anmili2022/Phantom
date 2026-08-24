namespace Phantom;

public sealed class ZodiacMonsterTracker : IDisposable
{
    private readonly PluginConfiguration configuration;

    public ZodiacMonsterTracker(PluginConfiguration configuration)
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

        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey)
            || !configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            || !characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress)
            || string.IsNullOrWhiteSpace(jobProgress.SelectedBookKey))
        {
            return;
        }

        var book = ZodiacGuide.AnimusBooks.FirstOrDefault(candidate => candidate.Key == jobProgress.SelectedBookKey);
        if (book == null)
        {
            return;
        }

        var text = ExtractChatMessageText(message);
        if (string.IsNullOrWhiteSpace(text) || !LooksLikeKillMessage(text))
        {
            return;
        }

        var changed = false;
        foreach (var objective in book.Monsters)
        {
            if (jobProgress.CompletedObjectives.Contains(objective.Key)
                || !text.Contains(objective.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var count = Math.Clamp(jobProgress.RequirementProgress.GetValueOrDefault(objective.Key), 0, objective.Needed);
            count = Math.Min(objective.Needed, count + 1);
            jobProgress.RequirementProgress[objective.Key] = count;
            if (count >= objective.Needed)
            {
                jobProgress.CompletedObjectives.Add(objective.Key);
            }

            changed = true;
            break;
        }

        if (changed)
        {
            configuration.Save();
            DalamudApi.Log.Information("Auto-marked Zodiac book monster objective for {Book}: {Text}", book.Name, text);
        }
    }

    private static bool LooksLikeKillMessage(string text)
        => text.Contains("击败", StringComparison.OrdinalIgnoreCase)
            || text.Contains("讨伐", StringComparison.OrdinalIgnoreCase)
            || text.Contains("被击破", StringComparison.OrdinalIgnoreCase)
            || text.Contains("已死亡", StringComparison.OrdinalIgnoreCase);

    private static string ExtractChatMessageText(object message)
    {
        var type = message.GetType();
        var text = type.GetProperty("Text")?.GetValue(message)?.ToString();
        return text ?? message.ToString() ?? string.Empty;
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
}
