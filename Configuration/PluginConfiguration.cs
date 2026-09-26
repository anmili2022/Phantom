using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Phantom;

[Serializable]
public sealed class PluginConfiguration : IPluginConfiguration
{
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public int Version { get; set; } = 2;
    public bool Enabled { get; set; } = true;
    public bool UseFlightNavigation { get; set; } = true;
    public bool ShowFloatingObjectiveWindow { get; set; } = true;
    public bool ShowSecretTargetsInFloatingWindow { get; set; } = true;
    public bool ShowSecretDutiesInFloatingWindow { get; set; } = true;
    public bool ShowAvailableFatesInFloatingWindow { get; set; } = true;
    public bool ZodiacFateNotificationsEnabled { get; set; } = true;
    public bool AutoTrackSelectedZodiacBookFates { get; set; } = true;
    public bool PrioritizeZodiacFatesInCatalog { get; set; } = true;
    public int ZodiacFateNotificationSound { get; set; } = 1;
    public bool ZodiacFateNotificationEdgeTts { get; set; }
    public int ZodiacFateNotificationIntervalSeconds { get; set; } = 15;
    public int ZodiacFateNotificationRepeatCount { get; set; } = 3;
    public List<TrackedFate> TrackedFates { get; set; } = new();
    public bool ShowZodiacMonitorInFloatingWindow { get; set; } = true;
    public bool FateAssistantEnabled { get; set; }
    public bool AutoHideCompletedFloatingItems { get; set; } = true;
    public bool NavigateToFlagDirectly { get; set; } = true;
    public bool SetFlagOnNavigation { get; set; } = true;
    public bool HuntAssistantEnabled { get; set; }
    public bool HuntAssistantEchoLeaderMessages { get; set; }
    public bool ShowHuntAssistantInFloatingWindow { get; set; } = true;
    public string HuntLeaderName { get; set; } = string.Empty;
    public float HuntTargetHeight { get; set; } = 50f;
    public bool ShowNavigationLogs { get; set; } = true;
    public bool GroupWeaponProgressByRole { get; set; }
    public bool ShowWeaponProgressIcons { get; set; } = true;
    public bool AutoMarkSecretKills { get; set; } = true;
    public uint TuliyollalAetheryteId { get; set; } = 13;
    public uint FloatingSecretTerritoryType { get; set; }
    public bool FloatingManualMode { get; set; }
    public int SelectedStageIndex { get; set; }
    public int SelectedMandervilleStageIndex { get; set; }
    public Dictionary<string, int> SelectedRelicStageIndexes { get; set; } = new();
    public Dictionary<string, int> Progress { get; set; } = new();
    public HashSet<string> CompletedTasks { get; set; } = new();
    public Dictionary<string, Dictionary<string, uint>> WeaponProgressByCharacter { get; set; } = new();
    public Dictionary<string, Dictionary<string, List<uint>>> WeaponProgressItemsByCharacter { get; set; } = new();
    public Dictionary<string, string> WeaponProgressSyncTimes { get; set; } = new();
    public Dictionary<string, List<string>> YokaiOwnedRewardKeysByCharacter { get; set; } = new();
    public Dictionary<string, string> YokaiSyncTimesByCharacter { get; set; } = new();
    public Dictionary<string, ZodiacCharacterProgress> ZodiacProgressByCharacter { get; set; } = new();
    public string SelectedZodiacJobKey { get; set; } = "pld";
    public string FloatingZodiacJobKey { get; set; } = "pld";
    public string FloatingZodiacStageKey { get; set; } = "zodiac-animus";
    public bool HideOwnedYokaiRewards { get; set; }
    public bool DebugLogSyncedItemLocations { get; set; }
    public bool DebugLogMissingItemLocations { get; set; }
    public HashSet<uint> BackpackOrganizeItemIds { get; set; } = new();
    public List<TrackedEquipment> TrackedEquipment { get; set; } = new();
    public List<TrackedEquipmentList> TrackedEquipmentLists { get; set; } = new();
    public List<TrackedEquipmentSet> TrackedEquipmentSets { get; set; } = new();
    public bool TrackedEquipmentBackpackFirst { get; set; }
    public float TrackedEquipmentTableRowHeight { get; set; } = 32f;
    public float TrackedEquipmentNameColumnWidth { get; set; } = 150f;
    public float TrackedEquipmentBackpackColumnWidth { get; set; } = 35f;
    public float TrackedEquipmentArmoryColumnWidth { get; set; } = 35f;
    public float TrackedEquipmentSaddlebagColumnWidth { get; set; } = 80f;
    public float TrackedEquipmentRetainerColumnWidth { get; set; } = 150f;
    public float TrackedEquipmentDresserColumnWidth { get; set; } = 35f;
    public float TrackedEquipmentCabinetColumnWidth { get; set; } = 35f;
    public Dictionary<string, Dictionary<uint, List<string>>> TrackedEquipmentLocationsByCharacter { get; set; } = new();
    public Dictionary<string, string> TrackedEquipmentSyncTimesByCharacter { get; set; } = new();

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        if (Version < 2)
        {
            ZodiacFateNotificationIntervalSeconds = 15;
            ZodiacFateNotificationRepeatCount = 3;
            Version = 2;
            Save();
        }

        if (TrackedEquipmentLists.Count == 0)
        {
            TrackedEquipmentLists.Add(new TrackedEquipmentList(
                "default",
                "默认多装备列表",
                TrackedEquipment.Select(item => item.ItemId).Distinct().ToList()));
            Save();
        }
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}

[Serializable]
public sealed record TrackedFate(uint FateId, uint TerritoryType, string Name, string Zone, float MapX, float MapY);

[Serializable]
public sealed record TrackedEquipment(uint ItemId, string Name);

[Serializable]
public sealed record TrackedEquipmentList(string Key, string Name, List<uint> ItemIds);

[Serializable]
public sealed class TrackedEquipmentSet
{
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新装备套装";
    public string JobKey { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public Dictionary<string, uint> Slots { get; set; } = new(StringComparer.Ordinal);
}
