# 前往幻境村流程移植说明

这份文档用于把 Phantom 的“前往幻境村”流程移植到其他 Dalamud 插件。

目标：只进入幻境村并导航到 `(26.98, 0, 11.26)`，不要执行完整 `/li occult` 的新月岛排本流程。

## 依赖

- `Lifestream >= 2.5.4.15`
- `vnavmesh >= 0.7.6.0`

## 关键常量

```csharp
private const uint TuliyollalTerritoryType = 1185;
private const uint TuliyollalAetheryteId = 216;
private const uint OccultVillageAethernetId = 239;
private static readonly Vector3 OccultVillageDestination = new(26.98f, 0f, 11.26f);
```

含义：

- `216`：Lifestream `/li occult` 使用的图莱尤拉主水晶 ID。
- `239`：Lifestream `/li occult` 使用的幻境村都市传送网目标 ID。
- 不要用字符串 `珠串万货街` 匹配传送点，语言环境和表数据都可能不稳定。
- 不要直接调用 `/li occult`，它会继续尝试进入新月岛。

## Lifestream IPC

需要订阅：

```csharp
private ICallGateSubscriber<uint, byte, bool>? teleport;
private ICallGateSubscriber<uint, bool>? aethernetTeleportById;
private ICallGateSubscriber<bool>? lifestreamIsBusy;
```

IPC 名称和签名：

```csharp
teleport = pluginInterface.GetIpcSubscriber<uint, byte, bool>("Lifestream.Teleport");
aethernetTeleportById = pluginInterface.GetIpcSubscriber<uint, bool>("Lifestream.AethernetTeleportById");
lifestreamIsBusy = pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
```

建议按需初始化，不要只在插件构造时初始化一次。插件加载顺序可能导致你的插件先于 Lifestream 加载。

```csharp
private bool EnsureLifestreamIpc()
{
    if (teleport != null && aethernetTeleportById != null && lifestreamIsBusy != null)
        return true;

    if (!pluginInterface.InstalledPlugins.Any(p => p.InternalName == "Lifestream" && p.IsLoaded))
        return false;

    teleport ??= pluginInterface.GetIpcSubscriber<uint, byte, bool>("Lifestream.Teleport");
    aethernetTeleportById ??= pluginInterface.GetIpcSubscriber<uint, bool>("Lifestream.AethernetTeleportById");
    lifestreamIsBusy ??= pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
    return true;
}
```

## vnavmesh IPC

需要订阅：

```csharp
private readonly ICallGateSubscriber<bool> isReady;
private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo;
private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPoint;
private readonly ICallGateSubscriber<object> stop;
```

IPC 名称和签名：

```csharp
isReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
pathfindAndMoveTo = pluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
nearestPoint = pluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
stop = pluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
```

幻境村内导航要用步行：

```csharp
pathfindAndMoveTo.InvokeFunc(target, false);
```

不要传 `true`，图莱尤拉/幻境村不应尝试上坐骑或飞行。

## 状态机

用 `Framework.Update` 驱动，不要在按钮回调里同步等待。

```csharp
private enum OccultVillageRouteStep
{
    None,
    WaitingTuliyollal,
    WaitingOccultVillageAethernet,
    MovingToDestination,
}

private OccultVillageRouteStep occultRouteStep;
private DateTime occultRouteStepStartedUtc;
```

## 按钮入口

```csharp
public void GoToOccultVillage()
{
    StopNavigationAndClearRoute();

    if (!EnsureLifestreamIpc() || teleport == null)
    {
        PrintEcho("前往幻境村失败：Lifestream 不可用。");
        return;
    }

    try
    {
        if (!teleport.InvokeFunc(TuliyollalAetheryteId, 0))
        {
            PrintEcho("前往幻境村失败：Lifestream 没有开始传送到图莱尤拉。");
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
    PrintEcho("已请求传送到图莱尤拉，等待读图完成。");
}
```

## 都市传送网步骤

```csharp
private void TeleportTuliyollalOccultVillage()
{
    if (!EnsureLifestreamIpc() || aethernetTeleportById == null)
    {
        PrintEcho("前往幻境村失败：Lifestream 不可用。");
        occultRouteStep = OccultVillageRouteStep.None;
        return;
    }

    try
    {
        if (!aethernetTeleportById.InvokeFunc(OccultVillageAethernetId))
        {
            PrintEcho("前往幻境村失败：Lifestream 没有开始传送到幻境村。");
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
    PrintEcho("已请求传送到幻境村，等待读图/传送完成。");
}
```

## Update 处理逻辑

```csharp
private void ProcessOccultVillageRoute()
{
    if (occultRouteStep == OccultVillageRouteStep.None)
        return;

    if (DateTime.UtcNow - occultRouteStepStartedUtc > TimeSpan.FromSeconds(90))
    {
        PrintEcho("前往幻境村超时，已取消流程。");
        occultRouteStep = OccultVillageRouteStep.None;
        return;
    }

    if (condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])
        return;

    switch (occultRouteStep)
    {
        case OccultVillageRouteStep.WaitingTuliyollal:
            if (clientState.TerritoryType == TuliyollalTerritoryType && lifestreamIsBusy?.InvokeFunc() != true)
                TeleportTuliyollalOccultVillage();
            break;

        case OccultVillageRouteStep.WaitingOccultVillageAethernet:
            if (lifestreamIsBusy?.InvokeFunc() == true)
                return;

            if (DateTime.UtcNow - occultRouteStepStartedUtc < TimeSpan.FromSeconds(2))
                return;

            occultRouteStep = OccultVillageRouteStep.MovingToDestination;
            occultRouteStepStartedUtc = DateTime.UtcNow;
            PrintEcho("开始步行导航到幻境村目标点。");
            StartMove(OccultVillageDestination, false);
            break;

        case OccultVillageRouteStep.MovingToDestination:
            if (IsPlayerNear(OccultVillageDestination, 4f))
            {
                StopNavigationAndClearRoute();
                PrintEcho("已到达幻境村目标点。");
            }
            break;
    }
}
```

## 导航要点

步行导航时跳过上坐骑：

```csharp
private bool QueueMountBeforeMove(Vector3 target, bool fly)
{
    if (!fly)
        return false;

    // 只有 fly == true 时才尝试上坐骑
    ...
}
```

导航前建议吸附到 navmesh：

```csharp
private Vector3? SnapToNavmesh(Vector3 position)
{
    return nearestPoint.InvokeFunc(position, 120f, 300f)
        ?? nearestPoint.InvokeFunc(position, 180f, 600f)
        ?? nearestPoint.InvokeFunc(position, 260f, 1000f);
}
```

启动导航：

```csharp
private void StartMove(Vector3 target, bool fly)
{
    var snapped = SnapToNavmesh(target);
    if (!snapped.HasValue)
    {
        PrintEcho("导航失败：vnavmesh 在目标附近找不到可走网格点。");
        return;
    }

    if (!isReady.InvokeFunc())
    {
        PrintEcho("导航失败：vnavmesh 未就绪。");
        return;
    }

    pathfindAndMoveTo.InvokeFunc(snapped.Value, fly);
}
```

## 不要移植的 `/li occult` 后半段

完整 `/li occult` 还会继续：

- 移动到 `(-36.835308, 0, -12.507464)`
- 移动到 `(-74.80987, 5, -15.057071)`
- 交互 `DataID = 1053611`
- 选择新月岛南征篇菜单
- 确认 Contents Finder

本需求不需要这些。只做到幻境村内目标点即可。

## 常见坑

- 不要用旧的图莱尤拉 ID `13`，应使用 `216`。
- 不要用 `Lifestream.AethernetTeleport("珠串万货街")`，应使用 `Lifestream.AethernetTeleportById(239)`。
- 不要在失败后保持状态机不变，否则会每帧重试刷屏。
- 不要在图莱尤拉或幻境村里尝试上坐骑/飞行。
- Lifestream 可能比你的插件后加载，所以 IPC 要按需初始化。
