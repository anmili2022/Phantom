using Dalamud.Game.DutyState;

namespace Phantom;

public sealed class DutyCompletionTracker : IDisposable
{
    private static readonly IReadOnlyDictionary<uint, IReadOnlyList<string>> ZodiacObjectiveKeysByTerritory =
        new Dictionary<uint, IReadOnlyList<string>>
        {
            [1037] = new[] { "fire-1-duty-1" }, [1042] = new[] { "fire-1-duty-2" },
            [363] = new[] { "fire-1-duty-3", "water-prison-1-duty-3", "zodiac-zodiac-duty-unknown-craftsman-3" },
            [1038] = new[] { "water-1-duty-1" }, [1330] = new[] { "water-1-duty-2", "zodiac-zodiac-duty-drop-of-kindness-1" },
            [362] = new[] { "water-1-duty-3", "wind-2-duty-3", "zodiac-zodiac-duty-drop-of-kindness-2" },
            [1036] = new[] { "wind-1-duty-1" }, [1331] = new[] { "wind-1-duty-2", "zodiac-zodiac-duty-unknown-craftsman-1" },
            [360] = new[] { "wind-1-duty-3", "fire-prison-1-duty-3" }, [1041] = new[] { "fire-2-duty-1" },
            [159] = new[] { "fire-2-duty-2", "zodiac-zodiac-duty-passion-dream-1" },
            [349] = new[] { "fire-2-duty-3", "wind-2-duty-2", "zodiac-zodiac-duty-passion-dream-2" },
            [1039] = new[] { "water-2-duty-1" }, [167] = new[] { "water-2-duty-2", "earth-1-duty-2", "zodiac-zodiac-duty-mothers-hand-1" },
            [350] = new[] { "water-2-duty-3", "fire-prison-1-duty-2", "zodiac-zodiac-duty-unknown-craftsman-2" },
            [1040] = new[] { "wind-2-duty-1" }, [1267] = new[] { "fire-prison-1-duty-1" },
            [1303] = new[] { "water-prison-1-duty-1" }, [160] = new[] { "water-prison-1-duty-2", "earth-1-duty-3", "zodiac-zodiac-duty-mothers-hand-2" },
            [1245] = new[] { "earth-1-duty-1", "zodiac-zodiac-duty-drop-of-kindness-3" }, [1062] = new[] { "zodiac-zodiac-duty-drop-of-kindness-4" },
            [387] = new[] { "zodiac-zodiac-duty-unknown-craftsman-4" }, [361] = new[] { "zodiac-zodiac-duty-passion-dream-3" },
            [367] = new[] { "zodiac-zodiac-duty-passion-dream-4" }, [373] = new[] { "zodiac-zodiac-duty-mothers-hand-3" },
            [365] = new[] { "zodiac-zodiac-duty-mothers-hand-4" },
        };

    private static readonly IReadOnlyDictionary<uint, string> PhantomObjectiveKeysByTerritory = new Dictionary<uint, string>
    {
        [1167] = "secret-duty-leveling-river", [1193] = "secret-duty-leveling-mountain", [1194] = "secret-duty-leveling-skydeep",
        [1198] = "secret-duty-leveling-vanguard", [1208] = "secret-duty-leveling-origenics", [1199] = "secret-duty-expert-alexandria",
        [1203] = "secret-duty-expert-cactus", [1204] = "secret-duty-expert-strayborough", [1242] = "secret-duty-expert-yuweyawata",
        [1266] = "secret-duty-expert-keeper", [1292] = "secret-duty-expert-terminal", [1314] = "secret-duty-expert-mistwake",
        [1345] = "secret-duty-expert-klythios", [1195] = "secret-duty-trial-valigarmanda", [1200] = "secret-duty-trial-zoraal-ja",
        [1202] = "secret-duty-trial-queen", [1270] = "secret-duty-trial-zelenia", [1295] = "secret-duty-trial-eternal-darkness",
        [1307] = "secret-duty-trial-recollection", [1361] = "secret-duty-trial-necron", [1248] = "secret-duty-alliance-jeuno",
        [1304] = "secret-duty-alliance-san-d-oria", [1368] = "secret-duty-alliance-windurst", [1225] = "secret-duty-arcadion-l1",
        [1227] = "secret-duty-arcadion-l2", [1229] = "secret-duty-arcadion-l3", [1231] = "secret-duty-arcadion-l4",
        [1256] = "secret-duty-arcadion-m1", [1258] = "secret-duty-arcadion-m2", [1260] = "secret-duty-arcadion-m3",
        [1262] = "secret-duty-arcadion-m4", [1320] = "secret-duty-arcadion-h1", [1322] = "secret-duty-arcadion-h2",
        [1324] = "secret-duty-arcadion-h3", [1326] = "secret-duty-arcadion-h4",
    };

    private readonly PluginConfiguration configuration;

    public DutyCompletionTracker(PluginConfiguration configuration)
    {
        this.configuration = configuration;
        DalamudApi.DutyState.DutyCompleted += OnDutyCompleted;
    }

    public void Dispose() => DalamudApi.DutyState.DutyCompleted -= OnDutyCompleted;

    private void OnDutyCompleted(IDutyStateEventArgs args)
    {
        if (!configuration.Enabled)
        {
            return;
        }

        var territoryType = args.TerritoryType.RowId;
        var changedZodiac = MarkZodiacObjectives(territoryType);
        var changedPhantom = PhantomObjectiveKeysByTerritory.TryGetValue(territoryType, out var phantomKey)
            && configuration.CompletedTasks.Add(phantomKey);
        if (!changedZodiac && !changedPhantom)
        {
            return;
        }

        configuration.Save();
        var systems = new List<string>();
        if (changedZodiac) systems.Add("古武");
        if (changedPhantom) systems.Add("幻武秘影");
        DalamudApi.ChatGui.Print($"[Phantom] 已自动标记副本完成：{string.Join("、", systems)}");
    }

    private bool MarkZodiacObjectives(uint territoryType)
    {
        if (!ZodiacObjectiveKeysByTerritory.TryGetValue(territoryType, out var objectiveKeys))
        {
            return false;
        }

        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            return false;
        }

        if (!configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress))
        {
            characterProgress = new ZodiacCharacterProgress();
            configuration.ZodiacProgressByCharacter[characterKey] = characterProgress;
        }

        if (!characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress))
        {
            jobProgress = new ZodiacJobProgress();
            characterProgress.Jobs[configuration.SelectedZodiacJobKey] = jobProgress;
        }

        var selectedBook = ZodiacGuide.AnimusBooks.FirstOrDefault(book => book.Key == jobProgress.SelectedBookKey);
        var selectedBookObjectiveKeys = selectedBook?.Duties
            .Select(duty => duty.Key)
            .ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        var changed = false;
        foreach (var objectiveKey in objectiveKeys)
        {
            if (!objectiveKey.StartsWith("zodiac-zodiac-duty-", StringComparison.Ordinal)
                && !selectedBookObjectiveKeys.Contains(objectiveKey))
            {
                continue;
            }

            changed |= jobProgress.CompletedObjectives.Add(objectiveKey);
        }

        return changed;
    }

    private static string GetCurrentCharacterKey()
    {
        var contentId = DalamudApi.PlayerState.ContentId;
        if (contentId != 0) return contentId.ToString();
        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null) return string.Empty;
        var world = player.HomeWorld.Value.Name.ExtractText();
        return string.IsNullOrWhiteSpace(world) ? player.Name.TextValue : $"{player.Name.TextValue}@{world}";
    }
}
