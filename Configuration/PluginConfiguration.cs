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
    public bool AutoHideCompletedFloatingItems { get; set; } = true;
    public bool AutoMarkSecretKills { get; set; } = true;
    public uint FloatingSecretTerritoryType { get; set; }
    public bool FloatingManualMode { get; set; }
    public int SelectedStageIndex { get; set; }
    public Dictionary<string, int> Progress { get; set; } = new();
    public HashSet<string> CompletedTasks { get; set; } = new();

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}
