using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Phantom;

[Serializable]
public sealed class PluginConfiguration : IPluginConfiguration
{
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool UseFlightNavigation { get; set; } = true;
    public bool ShowFloatingObjectiveWindow { get; set; } = true;
    public bool ShowSecretTargetsInFloatingWindow { get; set; } = true;
    public bool ShowSecretDutiesInFloatingWindow { get; set; } = true;
    public bool ShowAvailableFatesInFloatingWindow { get; set; } = true;
    public bool AutoHideCompletedFloatingItems { get; set; } = true;
    public bool NavigateToFlagDirectly { get; set; } = true;
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
    public bool HideOwnedYokaiRewards { get; set; }
    public bool DebugLogSyncedItemLocations { get; set; }
    public bool DebugLogMissingItemLocations { get; set; }
    public HashSet<uint> BackpackOrganizeItemIds { get; set; } = new();

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}
