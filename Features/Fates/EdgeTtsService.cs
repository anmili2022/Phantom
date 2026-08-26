using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Phantom;

public sealed class EdgeTtsService
{
    public const string RepositoryUrl = "https://gh.atmoomen.top/raw.githubusercontent.com/AtmoOmen/DalamudPlugins/main/pluginmaster.json";

    private readonly IDalamudPluginInterface pluginInterface;
    private ICallGateSubscriber<string, object?>? speak;

    public EdgeTtsService(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public bool IsInstalled => pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "EdgeTTS.Dalamud");

    public bool IsLoaded => pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "EdgeTTS.Dalamud" && plugin.IsLoaded);

    public bool Speak(string text)
    {
        if (!IsLoaded || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            speak ??= pluginInterface.GetIpcSubscriber<string, object?>("EdgeTTS.Speak");
            speak.InvokeAction(text);
            return true;
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "EdgeTTS speech request failed.");
            speak = null;
            return false;
        }
    }
}
