using Dalamud.Plugin;

namespace Phantom;

public sealed class PhantomPlugin : IDalamudPlugin
{
    private const string CommandName = "/phantom";
    private const string ChineseCommandName = "/肝武";
    private readonly VnavService vnav;
    private readonly SecretKillTracker killTracker;
    private readonly FateTracker fateTracker;
    private readonly HuntAssistant huntAssistant;
    private readonly PluginUI ui;

    public string Name => "Phantom";

    public PluginConfiguration Configuration { get; }

    public PhantomPlugin(IDalamudPluginInterface pluginInterface)
    {
        DalamudApi.Initialize(pluginInterface);

        Configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Configuration.Initialize(pluginInterface);

        vnav = new VnavService(pluginInterface, Configuration);
        killTracker = new SecretKillTracker(Configuration);
        fateTracker = new FateTracker(Configuration);
        huntAssistant = new HuntAssistant(Configuration, vnav);
        ui = new PluginUI(Configuration, vnav);

        DalamudApi.Commands.AddHandler(CommandName, new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "打开幻境武器助手。"
        });
        DalamudApi.Commands.AddHandler(ChineseCommandName, new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "打开肝武助手。"
        });

        pluginInterface.UiBuilder.Draw += ui.Draw;
        pluginInterface.UiBuilder.OpenMainUi += ui.OpenMainWindow;
        pluginInterface.UiBuilder.OpenConfigUi += ui.OpenMainWindow;

        DalamudApi.Log.Information("Phantom weapon assistant loaded.");
    }

    public void Dispose()
    {
        DalamudApi.PluginInterface.UiBuilder.Draw -= ui.Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi -= ui.OpenMainWindow;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ui.OpenMainWindow;
        DalamudApi.Commands.RemoveHandler(CommandName);
        DalamudApi.Commands.RemoveHandler(ChineseCommandName);
        killTracker.Dispose();
        fateTracker.Dispose();
        huntAssistant.Dispose();
        vnav.Dispose();
        Configuration.Save();
    }

    private void OnCommand(string command, string args)
    {
        ui.OpenMainWindow();
    }
}
