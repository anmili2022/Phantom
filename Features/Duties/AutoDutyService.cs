using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Lumina.Excel.Sheets;

namespace Phantom;

public sealed class AutoDutyService
{
    private readonly IDalamudPluginInterface pluginInterface;
    private ICallGateSubscriber<uint, bool>? contentHasPath;
    private ICallGateSubscriber<uint, int, bool, object?>? run;

    public AutoDutyService(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public bool Run(string dutyName)
    {
        if (!TryResolveTerritoryType(dutyName, out var territoryType))
        {
            Print($"无法从客户端副本表解析“{dutyName}”。");
            return false;
        }

        if (!EnsureIpc() || contentHasPath == null || run == null)
        {
            Print("AutoDuty 未安装或尚未加载。");
            return false;
        }

        try
        {
            if (!contentHasPath.InvokeFunc(territoryType))
            {
                Print($"AutoDuty 暂无“{dutyName}”的可用路径。");
                return false;
            }

            run.InvokeAction(territoryType, 1, false);
            Print($"已请求 AutoDuty 执行“{dutyName}”一次。");
            return true;
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to start AutoDuty for {DutyName}.", dutyName);
            Print($"启动 AutoDuty 失败：{ex.Message}");
            return false;
        }
    }

    private static bool TryResolveTerritoryType(string dutyName, out uint territoryType)
    {
        territoryType = 0;
        var normalized = NormalizeDutyName(dutyName);
        var row = DalamudApi.DataManager.GetExcelSheet<ContentFinderCondition>()
            .FirstOrDefault(candidate => candidate.TerritoryType.RowId != 0
                && NormalizeDutyName(candidate.Name.ExtractText()).Equals(normalized, StringComparison.Ordinal));
        territoryType = row.TerritoryType.RowId;
        return territoryType != 0;
    }

    private bool EnsureIpc()
    {
        if (contentHasPath != null && run != null)
        {
            return true;
        }

        if (!pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "AutoDuty" && plugin.IsLoaded))
        {
            return false;
        }

        try
        {
            contentHasPath = pluginInterface.GetIpcSubscriber<uint, bool>("AutoDuty.ContentHasPath");
            run = pluginInterface.GetIpcSubscriber<uint, int, bool, object?>("AutoDuty.Run");
            return true;
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to initialize AutoDuty IPC.");
            return false;
        }
    }

    private static string NormalizeDutyName(string name)
        => new(name.Where(character => !char.IsWhiteSpace(character) && character is not '·' and not '：' and not ':').ToArray());

    private static void Print(string message)
        => DalamudApi.ChatGui.Print($"[Phantom] [AutoDuty] {message}");
}
