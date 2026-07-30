using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace Phantom;

public sealed class VnavService : IDisposable
{
    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo;
    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPoint;
    private readonly ICallGateSubscriber<object> stop;
    private ICallGateSubscriber<uint, byte, bool>? teleport;
    private ICallGateSubscriber<bool>? lifestreamIsBusy;
    private Vector3? pendingTarget;
    private bool pendingFly;
    private DateTime pendingStartedUtc;
    private Vector3? pendingMoveTarget;
    private bool pendingMoveFly;
    private DateTime pendingMoveStartedUtc;
    private DateTime lastMountAttemptUtc = DateTime.MinValue;

    public VnavService(IDalamudPluginInterface pluginInterface)
    {
        isReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        pathfindAndMoveTo = pluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
        nearestPoint = pluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
        stop = pluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");

        if (pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "Lifestream" && plugin.IsLoaded))
        {
            teleport = pluginInterface.GetIpcSubscriber<uint, byte, bool>("Lifestream.Teleport");
            lifestreamIsBusy = pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        }

        DalamudApi.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudApi.Framework.Update -= OnFrameworkUpdate;
    }

    public void NavigateTo(Vector3 targetPos, bool fly)
    {
        try
        {
            var snapped = SnapToNavmesh(targetPos);
            if (!snapped.HasValue)
            {
                DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point for target position.");
                return;
            }

            StartMove(snapped.Value, fly);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Navigation request failed for target position.");
        }
    }

    public void NavigateTo(PhantomWeaponTarget target, bool fly)
    {
        try
        {
            if (!TryResolveWorldPosition(target, out var worldPosition))
            {
                DalamudApi.Log.Warning("Unable to resolve world position for {Name} at {X}, {Y}.", target.Name, target.MapX, target.MapY);
                return;
            }

            var snapped = SnapToNavmesh(worldPosition);
            if (!snapped.HasValue)
            {
                DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point for {Name}.", target.Name);
                return;
            }

            if (DalamudApi.ClientState.TerritoryType != target.TerritoryType && TryTeleportToTerritory(target.TerritoryType, snapped.Value, fly))
            {
                return;
            }

            StartMove(snapped.Value, fly);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Navigation request failed for {Name}.", target.Name);
        }
    }

    private bool TryResolveWorldPosition(PhantomWeaponTarget target, out Vector3 worldPosition)
    {
        worldPosition = default;

        var territories = DalamudApi.DataManager.GetExcelSheet<TerritoryType>();
        if (!territories.TryGetRow(target.TerritoryType, out var territory))
        {
            return false;
        }

        Map map;
        try
        {
            map = territory.Map.Value;
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to resolve map row for territory {TerritoryType}.", target.TerritoryType);
            return false;
        }

        if (map.RowId == 0)
        {
            return false;
        }

        var scale = map.SizeFactor;
        if (scale <= 0)
        {
            return false;
        }

        var worldX = 50f * target.MapX - map.OffsetX - 102400f / scale - 50f;
        var worldZ = 50f * target.MapY - map.OffsetY - 102400f / scale - 50f;
        worldPosition = new Vector3(worldX, 0f, worldZ);
        return true;
    }

    private Vector3? SnapToNavmesh(Vector3 position)
    {
        try
        {
            return nearestPoint.InvokeFunc(position, 120f, 120f);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "vnavmesh nearest point query failed.");
            return null;
        }
    }

    private bool TryTeleportToTerritory(uint territoryType, Vector3 target, bool fly)
    {
        if (teleport == null)
        {
            DalamudApi.Log.Warning("Lifestream is not available; navigate after moving to the target zone manually.");
            return false;
        }

        var aetheryteId = FindAetheryteForTerritory(territoryType);
        if (aetheryteId == 0)
        {
            DalamudApi.Log.Warning("No aetheryte found for territory {TerritoryType}.", territoryType);
            return false;
        }

        try { stop.InvokeAction(); } catch { }

        try
        {
            if (!teleport.InvokeFunc(aetheryteId, 0))
            {
                DalamudApi.Log.Warning("Lifestream teleport did not start for aetheryte {AetheryteId}.", aetheryteId);
                return false;
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Lifestream teleport IPC failed.");
            return false;
        }

        pendingTarget = target;
        pendingFly = fly;
        pendingStartedUtc = DateTime.UtcNow;
        return true;
    }

    private static uint FindAetheryteForTerritory(uint territoryType)
    {
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Aetheryte>())
        {
            if (!aetheryte.IsAetheryte || aetheryte.Territory.RowId != territoryType)
            {
                continue;
            }

            return aetheryte.RowId;
        }

        return 0;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _ = framework;
        ProcessPendingMove();

        if (!pendingTarget.HasValue)
        {
            return;
        }

        if (DateTime.UtcNow - pendingStartedUtc < TimeSpan.FromSeconds(3))
        {
            return;
        }

        if (DateTime.UtcNow - pendingStartedUtc > TimeSpan.FromSeconds(45))
        {
            pendingTarget = null;
            DalamudApi.Log.Warning("Timed out waiting for Lifestream teleport before vnavmesh navigation.");
            return;
        }

        try
        {
            if (lifestreamIsBusy?.InvokeFunc() == true || !isReady.InvokeFunc())
            {
                return;
            }
        }
        catch
        {
            return;
        }

        var target = pendingTarget.Value;
        var fly = pendingFly;
        pendingTarget = null;
        StartMove(target, fly);
    }

    private void StartMove(Vector3 target, bool fly)
    {
        if (QueueMountBeforeMove(target, fly))
        {
            return;
        }

        StartPathfind(target, fly);
    }

    private bool QueueMountBeforeMove(Vector3 target, bool fly)
    {
        if (DalamudApi.Condition[ConditionFlag.Mounted]
            || DalamudApi.Condition[ConditionFlag.InCombat]
            || DalamudApi.ObjectTable.LocalPlayer is not { IsDead: false })
        {
            return false;
        }

        pendingMoveTarget = target;
        pendingMoveFly = fly;
        pendingMoveStartedUtc = DateTime.UtcNow;
        lastMountAttemptUtc = DateTime.MinValue;
        return true;
    }

    private unsafe void ProcessPendingMove()
    {
        if (!pendingMoveTarget.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var target = pendingMoveTarget.Value;
        var fly = pendingMoveFly;

        if (DalamudApi.Condition[ConditionFlag.Mounted])
        {
            pendingMoveTarget = null;
            StartPathfind(target, fly);
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.InCombat]
            || DalamudApi.ObjectTable.LocalPlayer is not { IsDead: false })
        {
            pendingMoveTarget = null;
            StartPathfind(target, fly);
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.BetweenAreas]
            || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            return;
        }

        if (now - pendingMoveStartedUtc > TimeSpan.FromSeconds(8))
        {
            pendingMoveTarget = null;
            StartPathfind(target, fly);
            return;
        }

        if (now - lastMountAttemptUtc < TimeSpan.FromSeconds(2))
        {
            return;
        }

        lastMountAttemptUtc = now;
        ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
    }

    public void Stop()
    {
        pendingTarget = null;
        pendingMoveTarget = null;
        try { stop.InvokeAction(); } catch { }
    }

    private void StartPathfind(Vector3 target, bool fly)
    {
        try
        {
            var ok = pathfindAndMoveTo.InvokeFunc(target, fly);
            if (!ok)
            {
                DalamudApi.Log.Warning("vnavmesh failed to start navigation.");
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "vnavmesh navigation IPC failed.");
        }
    }
}
