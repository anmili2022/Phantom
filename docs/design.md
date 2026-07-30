# 幻境武器助手设计文档

## 目标

幻境武器助手是一个 Dalamud/卫月插件，用于辅助《最终幻想 XIV》幻境武器制作流程。

首版目标：

- 展示幻境武器各阶段所需材料、一次性流程和可重复来源。
- 保存用户手动录入的材料、以太、战斗记忆进度。
- 用醒目标记区分“仅需完成一次”的流程。
- 优先完善“幻境武器·秘影”阶段，提供指定目标小怪清单、坐标和导航按钮。

非首版目标：

- 自动读取背包材料数量。
- 自动读取任务状态或自动判断阶段完成。
- 自动战斗或自动完成任务。

## 参考资料

- 幻境武器资料：https://ff14.huijiwiki.com/wiki/%E5%B9%BB%E5%A2%83%E6%AD%A6%E5%99%A8
- 秘影阶段目标小怪坐标参考：https://www.xivdaily.com/cn/hunts/dt?result
- Dalamud/卫月 API 文档：https://dalamud.dev/api/
- Lumina.Excel 文档：https://github.com/NotAdam/Lumina.Excel
- 导航 IPC 参考项目：`E:\git\Chronicler`

## 当前结构

- `Phantom.csproj`：Dalamud API 15 插件项目配置。
- `Phantom.json`：卫月插件清单。
- `repo.json`：仓库发布清单占位。
- `Plugin/PhantomPlugin.cs`：插件入口、命令注册、UI 生命周期。
- `Infrastructure/DalamudApi.cs`：Dalamud 服务注入。
- `Configuration/PluginConfiguration.cs`：插件配置与进度持久化。
- `Features/PhantomWeapons/PhantomWeaponGuide.cs`：幻境武器阶段静态资料。
- `UI/PluginUI.cs`：主窗口、阶段 Tab、材料进度和一次性流程 UI。

## 数据模型

幻境武器资料目前以内置静态数据保存，避免运行期依赖网络。

- `PhantomWeaponStage`：一个武器阶段，例如半影、本影、黯影、蚀影、秘影。
- `PhantomWeaponRequirement`：可录入进度的材料、以太或记忆项目。
- `PhantomWeaponTask`：一次性流程，用配置中的 `CompletedTasks` 保存勾选状态。
- `PhantomWeaponReward`：可重复来源，例如副本、FATE、CE 的奖励数量。

用户进度保存在 `PluginConfiguration`：

- `SelectedStageIndex`：当前选中的阶段。
- `Progress`：键为资料项 Key，值为当前进度。
- `CompletedTasks`：已完成的一次性流程 Key。

## 秘影阶段需求

秘影阶段使用知见水晶积累战斗记忆。每张 7.0 地图需要：

- 击倒 4 个指定目标小怪，每个目标 1 体。
- 金牌完成 5 个 FATE。

需要在 UI 中为每个目标显示：

- 所属地图。
- 目标名称。
- 地图坐标。
- 完成勾选。
- 导航按钮。

## 坐标来源

灰机 Wiki 给出秘影阶段目标名和坐标。xivdaily 的 DT 狩猎页面也包含 Dawntrail 狩猎小怪数据。

xivdaily 页面是 Next.js 应用，数据嵌在页面的 `__NEXT_DATA__` 中：

- `props.pageProps.zones`
- `props.pageProps.mobs`
- `props.pageProps.aetherytes`

已确认 `props.pageProps.mobs[zoneName][mobName].location` 中包含 `x`、`y`、`z`，但这里的 `x/y` 是玩家可见地图坐标，不是 vnavmesh 直接使用的世界坐标。

## 坐标转换

vnavmesh 的 `PathfindAndMoveTo` 需要游戏世界坐标 `Vector3`。

灰机 Wiki 和 xivdaily 坐标是地图显示坐标，需要转换为世界坐标。Dalamud 提供了 `Dalamud.Utility.MapUtil.WorldToMap`，但当前需求需要反向转换。

后续实现应通过 Lumina.Excel 读取当前地图信息：

- `Lumina.Excel.Sheets.Map`
- `Lumina.Excel.Sheets.TerritoryType`
- `Lumina.Excel.Sheets.TerritoryTypeTransient`
- `Lumina.Excel.Sheets.Aetheryte`

设计约束：

- 不硬编码世界坐标，除非确认地图比例和偏移稳定。
- 优先使用 Lumina 表的 `Map.OffsetX`、`Map.OffsetY`、`Map.SizeFactor` 与 territory transient 的 Z offset 做转换。
- 如果无法可靠转换，应在 UI 中显示坐标但禁用导航，避免把玩家导航到错误位置。

## 导航方案

导航分两段：

1. 使用 Lifestream 传送到目标地图的推荐以太之光。
2. 传送完成后使用 vnavmesh 导航到目标小怪位置。

需要调用的 IPC 参考 `E:\git\Chronicler\Features\CrescentIsle\VnavService.cs`。

vnavmesh IPC：

- `vnavmesh.Nav.IsReady`
- `vnavmesh.SimpleMove.PathfindAndMoveTo`
- `vnavmesh.Query.Mesh.NearestPoint`
- `vnavmesh.Path.Stop`

Lifestream IPC：

- `Lifestream.IsBusy`
- `Lifestream.Abort`
- `Lifestream.Teleport`
- `Lifestream.AethernetTeleportById`
- `Lifestream.AethernetTeleportByPlaceNameId`
- `Lifestream.GetActiveAetheryte`
- `Lifestream.GetActiveCustomAetheryte`

野外跨地图传送应优先使用 `Lifestream.Teleport(uint destination, byte subIndex)`。同城/同区域以太网移动才使用 `AethernetTeleportById` 或 `AethernetTeleportByPlaceNameId`。

## 飞行选项

秘影目标导航需要增加“飞行”选框。

配置建议：

- `UseFlightNavigation`：是否把 `fly = true` 传给 vnavmesh。

行为：

- 勾选后，导航调用 `vnavmesh.SimpleMove.PathfindAndMoveTo(target, true)`。
- 未勾选时，调用 `vnavmesh.SimpleMove.PathfindAndMoveTo(target, false)`。
- 如果目标地图未解锁飞行，vnavmesh 或游戏状态可能导致飞行路径失败，应允许用户取消并改用步行。

## UI 设计

秘影阶段 UI 应拆成两个区块：

- 地图级进度：每张地图显示 `4 个目标 + 5 个 FATE` 的完成情况。
- 目标列表：每个目标显示坐标、勾选框、导航按钮。

一次性流程继续使用醒目标记：

- 标题：`*** 仅需完成一次的流程 ***`
- 勾选项前缀：`[仅一次]`

## 实现注意事项

- 插件内置资料应注明来源日期，后续资料变动需要人工更新。
- 导航服务必须容忍 vnavmesh 或 Lifestream 未安装、未加载、IPC 不可用。
- Lifestream 传送后需要等待 `Lifestream.IsBusy == false` 且 vnavmesh ready，再继续路径导航。
- 当前工作区可能不是 Git 仓库，不依赖 Git 操作。
- 构建命令：`dotnet build`
- 构建产物：`output/Phantom.dll`
