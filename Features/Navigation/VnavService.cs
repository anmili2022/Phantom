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

    public void TeleportAndNavigate(Vector3 targetPos, bool fly)
    {
        var snapped = SnapToNavmesh(targetPos);
        if (!snapped.HasValue)
        {
            DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point for target position.");
            return;
        }

        if (teleport == null)
        {
            DalamudApi.Log.Warning("Lifestream is not available; navigating directly.");
            StartMove(snapped.Value, fly);
            return;
        }

        var aetheryteId = FindNearestAetheryteForTerritory(DalamudApi.ClientState.TerritoryType, snapped.Value);
        if (aetheryteId == 0)
        {
            DalamudApi.Log.Warning("No aetheryte found for current territory; navigating directly.");
            StartMove(snapped.Value, fly);
            return;
        }

        try { stop.InvokeAction(); } catch { }

        try
        {
            if (!teleport.InvokeFunc(aetheryteId, 0))
            {
                DalamudApi.Log.Warning("Lifestream teleport did not start; navigating directly.");
                StartMove(snapped.Value, fly);
                return;
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Lifestream teleport IPC failed; navigating directly.");
            StartMove(snapped.Value, fly);
            return;
        }

        pendingTarget = snapped.Value;
        pendingFly = fly;
        pendingStartedUtc = DateTime.UtcNow;
    }

    public Vector3? GetNearestCurrentTerritoryAetherytePosition(Vector3 targetPos)
    {
        var territoryType = DalamudApi.ClientState.TerritoryType;
        Vector3? nearestPosition = null;
        var nearestDistance = float.MaxValue;
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Aetheryte>())
        {
            if (!aetheryte.IsAetheryte || aetheryte.Territory.RowId != territoryType)
            {
                continue;
            }

            if (TryResolveAetheryteRawPosition(aetheryte, out var position))
            {
                var distance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(targetPos.X, targetPos.Z));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPosition = position;
                }
            }
        }

        return nearestPosition;
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

        if (target.UseWorldCoords)
        {
            worldPosition = new Vector3(target.WorldX, target.WorldY, target.WorldZ);
            return true;
        }

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

    private static bool TryResolveAetheryteRawPosition(Aetheryte aetheryte, out Vector3 position)
    {
        position = default;

        var maps = DalamudApi.DataManager.GetExcelSheet<Map>();
        var map = maps.FirstOrDefault(m => m.TerritoryType.RowId == aetheryte.Territory.RowId);
        if (map.RowId == 0 || map.SizeFactor <= 0)
        {
            return false;
        }

        foreach (var markerRow in DalamudApi.DataManager.GetSubrowExcelSheet<MapMarker>())
        {
            foreach (var marker in markerRow)
            {
                if (marker.DataType != 3 || marker.DataKey.RowId != aetheryte.RowId)
                {
                    continue;
                }

                position = new Vector3(
                    ConvertMapMarkerToRawPosition(marker.X, map.SizeFactor),
                    0f,
                    ConvertMapMarkerToRawPosition(marker.Y, map.SizeFactor));
                return true;
            }
        }

        return false;
    }

    private static float ConvertMapMarkerToRawPosition(int pos, float scale)
    {
        return (pos - 1024f) / (scale / 100f);
    }

    private bool TryTeleportToTerritory(uint territoryType, Vector3 target, bool fly)
    {
        if (teleport == null)
        {
            DalamudApi.Log.Warning("Lifestream is not available; navigate after moving to the target zone manually.");
            return false;
        }

        var aetheryteId = FindNearestAetheryteForTerritory(territoryType, target);
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

    private static uint FindNearestAetheryteForTerritory(uint territoryType, Vector3 target)
    {
        var nearestId = 0u;
        var nearestDistance = float.MaxValue;
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Aetheryte>())
        {
            if (!aetheryte.IsAetheryte || aetheryte.Territory.RowId != territoryType)
            {
                continue;
            }

            if (!TryResolveAetheryteRawPosition(aetheryte, out var position))
            {
                continue;
            }

            var distance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(target.X, target.Z));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestId = aetheryte.RowId;
            }
        }

        return nearestId != 0 ? nearestId : FindAetheryteForTerritory(territoryType);
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
