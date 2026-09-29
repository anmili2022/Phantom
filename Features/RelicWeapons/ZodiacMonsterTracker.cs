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
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (TryMarkBookObjective(text, book, jobProgress, "完成理符任务", book.Leves.Select(leve => (leve.Key, leve.Name)))
            || TryMarkBookObjective(text, book, jobProgress, "完成危命任务", book.Fates.Select(fate => (fate.Key, fate.Name)))
            || TryMarkBookObjective(text, book, jobProgress, "完成副本任务", book.Duties.Select(duty => (duty.Key, duty.Name))))
        {
            configuration.Save();
            return;
        }

        if (!LooksLikeKillMessage(text))
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

            var count = TryExtractObjectiveProgress(text, objective.Name, out var reportedProgress)
                ? Math.Clamp(reportedProgress, 0, objective.Needed)
                : Math.Min(objective.Needed, Math.Clamp(jobProgress.RequirementProgress.GetValueOrDefault(objective.Key), 0, objective.Needed) + 1);
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

    private static bool TryMarkBookObjective(
        string text,
        ZodiacBookGuide book,
        ZodiacJobProgress jobProgress,
        string prefix,
        IEnumerable<(string Key, string Name)> objectives)
    {
        var prefixIndex = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIndex < 0)
        {
            return false;
        }

        var targetName = ExtractQuotedName(text[(prefixIndex + prefix.Length)..]);
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return false;
        }

        var changed = false;
        foreach (var (key, name) in objectives)
        {
            if (!string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            changed |= jobProgress.CompletedObjectives.Add(key);
        }

        if (changed)
        {
            DalamudApi.Log.Information("Auto-marked Zodiac book objective for {Book}: {Name}", book.Name, targetName);
        }

        return changed;
    }

    private static string ExtractQuotedName(string text)
    {
        var openIndex = text.IndexOfAny(new[] { '“', '「', '『', '"' });
        if (openIndex < 0)
        {
            return string.Empty;
        }

        var close = text[openIndex] switch
        {
            '“' => '”',
            '「' => '」',
            '『' => '』',
            _ => '"',
        };
        var end = text.IndexOf(close, openIndex + 1);
        return end > openIndex ? text[(openIndex + 1)..end].Trim() : string.Empty;
    }

    private static bool LooksLikeKillMessage(string text)
        => text.Contains("击败", StringComparison.OrdinalIgnoreCase)
            || text.Contains("讨伐", StringComparison.OrdinalIgnoreCase)
            || text.Contains("被击破", StringComparison.OrdinalIgnoreCase)
            || text.Contains("已死亡", StringComparison.OrdinalIgnoreCase);

    private static bool TryExtractObjectiveProgress(string text, string objectiveName, out int progress)
    {
        progress = 0;
        var objectiveIndex = text.IndexOf(objectiveName, StringComparison.OrdinalIgnoreCase);
        if (objectiveIndex < 0)
        {
            return false;
        }

        var suffix = text[(objectiveIndex + objectiveName.Length)..];
        var separator = suffix.IndexOf('/');
        if (separator <= 0)
        {
            return false;
        }

        var start = separator - 1;
        while (start >= 0 && char.IsDigit(suffix[start]))
        {
            start--;
        }

        return int.TryParse(suffix[(start + 1)..separator], out progress);
    }

    private static string ExtractChatMessageText(object message)
    {
        try
        {
            var value = message.GetType().GetProperty("Message")?.GetValue(message);
            return value?.GetType().GetProperty("TextValue")?.GetValue(value) as string
                ?? value?.ToString()
                ?? message.GetType().GetProperty("Text")?.GetValue(message)?.ToString()
                ?? message.ToString()
                ?? string.Empty;
        }
        catch
        {
            return message.ToString() ?? string.Empty;
        }
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
