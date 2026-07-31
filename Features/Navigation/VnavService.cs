using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;

namespace Phantom;

public sealed class VnavService : IDisposable
{
    private const uint TuliyollalTerritoryType = 1185;
    private const uint OccultVillageTerritoryType = 1278;
    private const uint TuliyollalAetheryteId = 216;
    private const uint OccultVillageAethernetId = 239;
    private static readonly Vector3 OccultVillageDestination = new(26.98f, 0f, 11.26f);

    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo;
    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPoint;
    private readonly ICallGateSubscriber<object> stop;
    private readonly IDalamudPluginInterface pluginInterface;
    private ICallGateSubscriber<uint, byte, bool>? teleport;
    private ICallGateSubscriber<uint, bool>? aethernetTeleportById;
    private ICallGateSubscriber<bool>? lifestreamIsBusy;
    private ICallGateSubscriber<uint>? getActiveAetheryte;
    private Vector3? pendingTarget;
    private uint pendingTerritoryType;
    private Vector3? pendingAetherytePosition;
    private bool pendingFly;
    private DateTime pendingStartedUtc;
    private Vector3? pendingMoveTarget;
    private bool pendingMoveFly;
    private DateTime pendingMoveStartedUtc;
    private DateTime lastMountAttemptUtc = DateTime.MinValue;
    private OccultVillageRouteStep occultRouteStep;
    private DateTime occultRouteStepStartedUtc;
    private DateTime occultDestinationMoveStartedUtc;
    private Vector3 occultDestinationLastPosition;
    private int occultDestinationRetryCount;

    private enum OccultVillageRouteStep
    {
        None,
        WaitingTuliyollal,
        WaitingOccultVillageAethernet,
        MovingToDestination,
    }

    public VnavService(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        isReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        pathfindAndMoveTo = pluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
        nearestPoint = pluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
        stop = pluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");

        EnsureLifestreamIpc();

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
                PrintEcho("导航失败：vnavmesh 在目标附近找不到可走网格点。");
                return;
            }

            StartMove(snapped.Value, fly);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Navigation request failed for target position.");
            PrintEcho($"导航失败：{ex.Message}");
        }
    }

    public void TeleportAndNavigate(Vector3 targetPos, bool fly)
    {
        var snapped = SnapToNavmesh(targetPos);
        if (!snapped.HasValue)
        {
            DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point for target position.");
            PrintEcho("导航失败：vnavmesh 在目标附近找不到可走网格点。");
            return;
        }

        if (!EnsureLifestreamIpc() || teleport == null)
        {
            DalamudApi.Log.Warning("Lifestream is not available; navigating directly.");
            PrintEcho("Lifestream 不可用，改为直接导航。 ");
            StartMove(snapped.Value, fly);
            return;
        }

        var aetheryteId = FindNearestAetheryteForTerritory(DalamudApi.ClientState.TerritoryType, snapped.Value);
        if (aetheryteId == 0)
        {
            DalamudApi.Log.Warning("No aetheryte found for current territory; navigating directly.");
            PrintEcho("未找到当前地图以太水晶，改为直接导航。 ");
            StartMove(snapped.Value, fly);
            return;
        }

        try { stop.InvokeAction(); } catch { }

        try
        {
            if (!teleport.InvokeFunc(aetheryteId, 0))
            {
                DalamudApi.Log.Warning("Lifestream teleport did not start; navigating directly.");
                PrintEcho("Lifestream 没有开始传送，改为直接导航。 ");
                StartMove(snapped.Value, fly);
                return;
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Lifestream teleport IPC failed; navigating directly.");
            PrintEcho($"Lifestream IPC 失败，改为直接导航：{ex.Message}");
            StartMove(snapped.Value, fly);
            return;
        }

        pendingTarget = snapped.Value;
        pendingTerritoryType = DalamudApi.ClientState.TerritoryType;
        pendingAetherytePosition = TryFindAetherytePosition(aetheryteId, out var aetherytePosition) ? aetherytePosition : null;
        pendingFly = fly;
        pendingStartedUtc = DateTime.UtcNow;
    }

    public void GoToOccultVillage()
    {
        Stop();
        if (DalamudApi.ClientState.TerritoryType == OccultVillageTerritoryType)
        {
            if (!TryGetOccultDestinationNavmeshPoint(out var destination))
            {
                PrintEcho("当前已在幻境村，但 vnavmesh 未就绪或目标点不可达。 ");
                return;
            }

            occultRouteStep = OccultVillageRouteStep.MovingToDestination;
            occultRouteStepStartedUtc = DateTime.UtcNow;
            occultDestinationRetryCount = 0;
            PrintEcho("当前已在幻境村，直接步行导航到目标点。 ");
            StartOccultDestinationMove(destination);
            return;
        }

        if (!EnsureLifestreamIpc() || teleport == null)
        {
            PrintEcho("前往幻境村失败：Lifestream 不可用。 ");
            return;
        }

        try
        {
            if (!teleport.InvokeFunc(TuliyollalAetheryteId, 0))
            {
                PrintEcho("前往幻境村失败：Lifestream 没有开始传送到图莱忧菈。 ");
                return;
            }
        }
        catch (Exception ex)
        {
            PrintEcho($"前往幻境村失败：Lifestream IPC 异常：{ex.Message}");
            return;
        }

        occultRouteStep = OccultVillageRouteStep.WaitingTuliyollal;
        occultRouteStepStartedUtc = DateTime.UtcNow;
        occultDestinationMoveStartedUtc = DateTime.MinValue;
        occultDestinationLastPosition = default;
        occultDestinationRetryCount = 0;
        PrintEcho("已请求传送到图莱忧菈，等待读图完成。 ");
    }

    public Vector3? GetNearestCurrentTerritoryAetherytePosition(Vector3 targetPos)
    {
        var territoryType = DalamudApi.ClientState.TerritoryType;
        Vector3? nearestPosition = null;
        var nearestDistance = float.MaxValue;
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>())
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
                PrintEcho($"导航失败：无法解析 {target.Zone} {target.Name} 的世界坐标。 ");
                return;
            }

            if (DalamudApi.ClientState.TerritoryType != target.TerritoryType && TryTeleportToTerritory(target.TerritoryType, worldPosition, fly))
            {
                return;
            }

            var snapped = SnapToNavmesh(worldPosition);
            if (!snapped.HasValue)
            {
                DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point for {Name}.", target.Name);
                PrintEcho($"vnavmesh 在 {target.Name} 附近找不到可走网格点，尝试直接导航到解析坐标 ({worldPosition.X:0.#}, {worldPosition.Y:0.#}, {worldPosition.Z:0.#})。 ");
                StartMove(worldPosition, fly);
                return;
            }

            StartMove(snapped.Value, fly);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Navigation request failed for {Name}.", target.Name);
            PrintEcho($"导航失败：{ex.Message}");
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
            return nearestPoint.InvokeFunc(position, 120f, 300f)
                ?? nearestPoint.InvokeFunc(position, 180f, 600f)
                ?? nearestPoint.InvokeFunc(position, 260f, 1000f);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "vnavmesh nearest point query failed.");
            return null;
        }
    }

    private static bool TryResolveAetheryteRawPosition(Lumina.Excel.Sheets.Aetheryte aetheryte, out Vector3 position)
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
        if (!EnsureLifestreamIpc() || teleport == null)
        {
            DalamudApi.Log.Warning("Lifestream is not available; navigate after moving to the target zone manually.");
            PrintEcho("Lifestream 不可用；请手动到目标地图后再导航。 ");
            return false;
        }

        var aetheryteId = FindNearestAetheryteForTerritory(territoryType, target);
        if (aetheryteId == 0)
        {
            DalamudApi.Log.Warning("No aetheryte found for territory {TerritoryType}.", territoryType);
            PrintEcho($"导航失败：目标地图 {territoryType} 未找到可用以太水晶。 ");
            return false;
        }

        try { stop.InvokeAction(); } catch { }

        try
        {
            if (!teleport.InvokeFunc(aetheryteId, 0))
            {
                DalamudApi.Log.Warning("Lifestream teleport did not start for aetheryte {AetheryteId}.", aetheryteId);
                PrintEcho($"Lifestream 没有开始传送到以太水晶 {aetheryteId}。 ");
                return false;
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Lifestream teleport IPC failed.");
            PrintEcho($"Lifestream IPC 失败：{ex.Message}");
            return false;
        }

        pendingTarget = target;
        pendingTerritoryType = territoryType;
        pendingAetherytePosition = TryFindAetherytePosition(aetheryteId, out var aetherytePosition) ? aetherytePosition : null;
        pendingFly = fly;
        pendingStartedUtc = DateTime.UtcNow;
        PrintEcho($"已请求传送到目标地图 {territoryType}，等待读图完成后继续导航。 ");
        return true;
    }

    private static uint FindAetheryteForTerritory(uint territoryType)
    {
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>())
        {
            if (!aetheryte.IsAetheryte || aetheryte.Territory.RowId != territoryType)
            {
                continue;
            }

            return aetheryte.RowId;
        }

        return 0;
    }

    private bool EnsureLifestreamIpc()
    {
        if (teleport != null && aethernetTeleportById != null && lifestreamIsBusy != null && getActiveAetheryte != null)
        {
            return true;
        }

        if (!pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "Lifestream" && plugin.IsLoaded))
        {
            return false;
        }

        try
        {
            teleport ??= pluginInterface.GetIpcSubscriber<uint, byte, bool>("Lifestream.Teleport");
            aethernetTeleportById ??= pluginInterface.GetIpcSubscriber<uint, bool>("Lifestream.AethernetTeleportById");
            lifestreamIsBusy ??= pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
            getActiveAetheryte ??= pluginInterface.GetIpcSubscriber<uint>("Lifestream.GetActiveAetheryte");
            return true;
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to initialize Lifestream IPC.");
            return false;
        }
    }

    private static uint FindAetheryteByName(uint territoryType, string name)
    {
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>())
        {
            if (!aetheryte.IsAetheryte || aetheryte.Territory.RowId != territoryType)
            {
                continue;
            }

            var aethernetName = aetheryte.AethernetName.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var placeName = aetheryte.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;
            if (aethernetName.Contains(name, StringComparison.Ordinal) || placeName.Contains(name, StringComparison.Ordinal))
            {
                return aetheryte.RowId;
            }
        }

        return 0;
    }

    private static uint FindNearestAetheryteForTerritory(uint territoryType, Vector3 target)
    {
        var nearestId = 0u;
        var nearestDistance = float.MaxValue;
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>())
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

    private static bool TryFindAetherytePosition(uint aetheryteId, out Vector3 position)
    {
        position = default;
        foreach (var aetheryte in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>())
        {
            if (aetheryte.RowId == aetheryteId)
            {
                return TryResolveAetheryteRawPosition(aetheryte, out position);
            }
        }

        return false;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _ = framework;
        ProcessOccultVillageRoute();
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
            pendingTerritoryType = 0;
            pendingAetherytePosition = null;
            DalamudApi.Log.Warning("Timed out waiting for Lifestream teleport before vnavmesh navigation.");
            PrintEcho("等待 Lifestream 传送超时，已取消后续导航。 ");
            return;
        }

        if (pendingTerritoryType != 0 && DalamudApi.ClientState.TerritoryType != pendingTerritoryType)
        {
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.BetweenAreas]
            || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            return;
        }

        if (pendingAetherytePosition.HasValue)
        {
            var player = DalamudApi.ObjectTable.LocalPlayer;
            if (player == null)
            {
                return;
            }

            var playerXZ = new Vector2(player.Position.X, player.Position.Z);
            var aetheryteXZ = new Vector2(pendingAetherytePosition.Value.X, pendingAetherytePosition.Value.Z);
            if (Vector2.Distance(playerXZ, aetheryteXZ) > 90f)
            {
                return;
            }
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
        pendingTerritoryType = 0;
        pendingAetherytePosition = null;
        var snapped = SnapToNavmesh(target);
        if (!snapped.HasValue)
        {
            DalamudApi.Log.Warning("vnavmesh could not find a nearby navmesh point after teleport.");
            PrintEcho("传送完成，但 vnavmesh 在目标附近找不到可走网格点。 ");
            return;
        }

        StartMove(snapped.Value, fly);
    }

    private void TeleportTuliyollalOccultVillage()
    {
        if (!EnsureLifestreamIpc() || aethernetTeleportById == null)
        {
            PrintEcho("前往幻境村失败：Lifestream 不可用。 ");
            occultRouteStep = OccultVillageRouteStep.None;
            return;
        }

        try
        {
            if (!aethernetTeleportById.InvokeFunc(OccultVillageAethernetId))
            {
                PrintEcho("前往幻境村失败：Lifestream 没有开始传送到幻境村。 ");
                occultRouteStep = OccultVillageRouteStep.None;
                return;
            }
        }
        catch (Exception ex)
        {
            PrintEcho($"前往幻境村失败：Lifestream IPC 异常：{ex.Message}");
            occultRouteStep = OccultVillageRouteStep.None;
            return;
        }

        occultRouteStep = OccultVillageRouteStep.WaitingOccultVillageAethernet;
        occultRouteStepStartedUtc = DateTime.UtcNow;
        PrintEcho("已请求传送到幻境村，等待读图/传送完成。 ");
    }

    private bool IsTuliyollalRootAetheryteReady()
    {
        if (DalamudApi.ClientState.TerritoryType != TuliyollalTerritoryType)
        {
            return false;
        }

        if (DateTime.UtcNow - occultRouteStepStartedUtc < TimeSpan.FromSeconds(2))
        {
            return false;
        }

        if (DalamudApi.Condition[ConditionFlag.BetweenAreas] || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            return false;
        }

        try
        {
            if (lifestreamIsBusy?.InvokeFunc() == true)
            {
                return false;
            }

            if (getActiveAetheryte?.InvokeFunc() != TuliyollalAetheryteId)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        return DalamudApi.ObjectTable.LocalPlayer != null;
    }

    private void ProcessOccultVillageRoute()
    {
        if (occultRouteStep == OccultVillageRouteStep.None)
        {
            return;
        }

        if (DateTime.UtcNow - occultRouteStepStartedUtc > TimeSpan.FromSeconds(90))
        {
            PrintEcho("前往幻境村超时，已取消流程。 ");
            occultRouteStep = OccultVillageRouteStep.None;
            return;
        }

        if (DalamudApi.Condition[ConditionFlag.BetweenAreas] || DalamudApi.Condition[ConditionFlag.BetweenAreas51])
        {
            return;
        }

        switch (occultRouteStep)
        {
            case OccultVillageRouteStep.WaitingTuliyollal:
                if (IsTuliyollalRootAetheryteReady())
                {
                    TeleportTuliyollalOccultVillage();
                }
                break;
            case OccultVillageRouteStep.WaitingOccultVillageAethernet:
                if (lifestreamIsBusy?.InvokeFunc() == true)
                {
                    return;
                }

                if (DalamudApi.ClientState.TerritoryType != OccultVillageTerritoryType)
                {
                    return;
                }

                if (DateTime.UtcNow - occultRouteStepStartedUtc < TimeSpan.FromSeconds(2))
                {
                    return;
                }

                if (DalamudApi.ObjectTable.LocalPlayer == null)
                {
                    return;
                }

                if (!TryGetOccultDestinationNavmeshPoint(out var destination))
                {
                    return;
                }

                occultRouteStep = OccultVillageRouteStep.MovingToDestination;
                occultRouteStepStartedUtc = DateTime.UtcNow;
                PrintEcho("开始步行导航到幻境村目标点。 ");
                StartOccultDestinationMove(destination);
                break;
            case OccultVillageRouteStep.MovingToDestination:
                if (IsPlayerNear(OccultVillageDestination, 4f))
                {
                    Stop();
                    PrintEcho("已到达幻境村目标点。 ");
                    return;
                }

                if (DalamudApi.ClientState.TerritoryType != OccultVillageTerritoryType)
                {
                    Stop();
                    PrintEcho("已离开幻境村，取消目标点导航。 ");
                    return;
                }

                RetryOccultDestinationMoveIfStuck();
                break;
        }
    }

    private bool TryGetOccultDestinationNavmeshPoint(out Vector3 destination)
    {
        destination = default;
        try
        {
            if (!isReady.InvokeFunc())
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        var snapped = SnapToNavmesh(OccultVillageDestination);
        if (!snapped.HasValue)
        {
            return false;
        }

        destination = snapped.Value;
        return true;
    }

    private void StartOccultDestinationMove(Vector3 destination)
    {
        var player = DalamudApi.ObjectTable.LocalPlayer;
        occultDestinationMoveStartedUtc = DateTime.UtcNow;
        occultDestinationLastPosition = player?.Position ?? default;
        StartPathfind(destination, false);
    }

    private void RetryOccultDestinationMoveIfStuck()
    {
        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now - occultRouteStepStartedUtc > TimeSpan.FromSeconds(60))
        {
            Stop();
            PrintEcho("前往幻境村目标点超时，已取消导航。 ");
            return;
        }

        if (now - occultDestinationMoveStartedUtc < TimeSpan.FromSeconds(7))
        {
            return;
        }

        var moved = Vector3.Distance(player.Position, occultDestinationLastPosition);
        if (moved >= 2.5f)
        {
            occultDestinationMoveStartedUtc = now;
            occultDestinationLastPosition = player.Position;
            return;
        }

        if (occultDestinationRetryCount >= 3)
        {
            Stop();
            PrintEcho("前往幻境村目标点失败：多次重试后仍未移动。 ");
            return;
        }

        if (!TryGetOccultDestinationNavmeshPoint(out var destination))
        {
            occultDestinationMoveStartedUtc = now;
            occultDestinationLastPosition = player.Position;
            return;
        }

        occultDestinationRetryCount++;
        PrintEcho($"幻境村目标点导航未移动，重试 {occultDestinationRetryCount}/3。 ");
        StartOccultDestinationMove(destination);
    }

    private static bool IsPlayerNear(Vector3 position, float distance)
    {
        var player = DalamudApi.ObjectTable.LocalPlayer;
        return player != null && Vector3.Distance(player.Position, position) <= distance;
    }

    private static unsafe bool TryInteractNearestObject(Vector3 position, float maxDistance)
    {
        var obj = DalamudApi.ObjectTable
            .Where(obj => obj is { IsTargetable: true })
            .OrderBy(obj => Vector3.Distance(obj.Position, position))
            .FirstOrDefault(obj => Vector3.Distance(obj.Position, position) <= maxDistance);
        if (obj == null)
        {
            return false;
        }

        TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address, false);
        return true;
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
        if (!fly)
        {
            return false;
        }

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
        PrintEcho("导航准备：尝试上坐骑；若 8 秒内未成功，将直接发起 vnavmesh 导航。 ");
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
            PrintEcho("未能自动上坐骑，改为直接发起 vnavmesh 导航。 ");
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
        pendingTerritoryType = 0;
        pendingAetherytePosition = null;
        pendingMoveTarget = null;
        occultRouteStep = OccultVillageRouteStep.None;
        occultDestinationMoveStartedUtc = DateTime.MinValue;
        occultDestinationLastPosition = default;
        occultDestinationRetryCount = 0;
        try { stop.InvokeAction(); } catch { }
    }

    private void StartPathfind(Vector3 target, bool fly)
    {
        try
        {
            if (!isReady.InvokeFunc())
            {
                PrintEcho("导航失败：vnavmesh 未就绪，请确认 vnavmesh 已加载并且当前地图网格可用。 ");
                return;
            }

            PrintEcho($"发起 vnavmesh 导航：({target.X:0.#}, {target.Y:0.#}, {target.Z:0.#})，{(fly ? "飞行" : "步行")}。 ");
            var ok = pathfindAndMoveTo.InvokeFunc(target, fly);
            if (!ok)
            {
                DalamudApi.Log.Warning("vnavmesh failed to start navigation.");
                PrintEcho("导航失败：vnavmesh 拒绝开始路径规划，可能当前地图没有网格或目标不可达。 ");
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "vnavmesh navigation IPC failed.");
            PrintEcho($"导航失败：vnavmesh IPC 调用异常：{ex.Message}");
        }
    }

    private static void PrintEcho(string message)
    {
        try
        {
            DalamudApi.ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = new SeStringBuilder()
                    .AddUiForeground("[Phantom] ", 37)
                    .AddUiForeground(message, 24)
                    .Build(),
            });
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to print navigation status to chat.");
        }
    }
}
