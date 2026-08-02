# 肝武助手设计文档

> 当前版本：0.1.12.0 | 更新日期：2026-08-02

## 目标

肝武助手是一个 Dalamud/卫月插件，用于辅助《最终幻想 XIV》多系列武器与生产工具制作流程，当前已接入幻境武器、曼德维尔武器、旧资料片肝武、生产采集特殊工具、绝武和妖怪手表联动奖励追踪。

当前目标：

- 以左侧系列导航统一管理幻武、古武、魂武、优武、义武、曼武、天钢、莫雯、宇宙、绝武和妖表联动。
- 展示幻境武器各阶段所需材料、一次性流程和可重复来源。
- 自动同步当前角色的幻武、旧肝武、曼武、特殊工具、绝武持有进度和妖表奖励状态。
- 用醒目标记区分“仅需完成一次”的流程，并为秘影阶段提供目标与 FATE 追踪。

当前不支持的目标：

- 自动读取任务状态或自动判断阶段完成。
- 自动战斗或自动完成任务。

## 参考资料

- 幻境武器资料：https://ff14.huijiwiki.com/wiki/%E5%B9%BB%E5%A2%83%E6%AD%A6%E5%99%A8
- 古武（上古武器）资料：https://ff14.huijiwiki.com/wiki/%E4%B8%8A%E5%8F%A4%E6%AD%A6%E5%99%A8
- 古武（黄道武器）资料：https://ff14.huijiwiki.com/wiki/%E9%BB%84%E9%81%93%E6%AD%A6%E5%99%A8
- 魂武（元灵武器）资料：https://ff14.huijiwiki.com/wiki/%E5%85%83%E7%81%B5%E6%AD%A6%E5%99%A8
- 优武（禁地兵装）资料：https://ff14.huijiwiki.com/wiki/%E7%A6%81%E5%9C%B0%E5%85%B5%E8%A3%85
- 优武（杰巴特·改）资料：https://ff14.huijiwiki.com/wiki/%E7%89%A9%E5%93%81:%E6%9D%B0%E5%B7%B4%E7%89%B9%C2%B7%E6%94%B9
- 义武（义军武器）资料：https://ff14.huijiwiki.com/wiki/%E4%B9%89%E5%86%9B%E6%AD%A6%E5%99%A8
- 曼武（曼德维尔武器）资料：https://ff14.huijiwiki.com/wiki/%E6%9B%BC%E5%BE%B7%E7%BB%B4%E5%B0%94%E6%AD%A6%E5%99%A8
- 天钢资料：https://ff14.huijiwiki.com/wiki/%E5%A4%A9%E9%92%A2%E5%B7%A5%E5%85%B7
- 莫雯资料：https://ff14.huijiwiki.com/wiki/%E8%8E%AB%E9%9B%AF%E5%8D%93%E8%B6%8A%E5%B7%A5%E5%85%B7
- 宇宙资料：https://ff14.huijiwiki.com/wiki/%E5%AE%87%E5%AE%99%E5%B7%A5%E5%85%B7
- 秘影阶段目标小怪坐标参考：https://www.xivdaily.com/cn/hunts/dt?result
- Dalamud/卫月 API 文档：https://dalamud.dev/api/
- Lumina.Excel 文档：https://github.com/NotAdam/Lumina.Excel
- 导航 IPC 参考项目：`E:\git\Chronicler`

## 系列范围与英文名称

| 系列 | 正式英文名称 | 说明 |
| --- | --- | --- |
| 古武 | Zodiac Weapons | 2.x 古代武器系列；上古武器为第一、二阶段，黄道武器为第三至第八阶段。 |
| 魂武 | Anima Weapons | 3.x 元灵武器系列。 |
| 优武 | Eureka Weapons | 4.x 禁地兵装系列；进度阶段为常风、恒冰、涌火、丰水、补正。 |
| 义武 | Resistance Weapons | 5.x 南方博兹雅战线相关系列；`Bozja Weapons` 可作为检索别名，不作为正式名称。 |
| 曼武 | Manderville Weapons | 6.x 曼德维尔武器系列；`Mandervillous Weapons` 可作为检索别名，不作为正式名称。 |
| 幻武 | Phantom Weapons | 7.x 幻境武器系列。 |
| 天钢 | Skysteel Tools | 生产采集职业工具系列。 |
| 莫雯 | Splendorous Tools | 生产采集职业工具系列。 |
| 宇宙 | Cosmic Tools | 生产采集职业工具系列。 |
| 绝武 | Ultimate Weapons | 绝境战武器奖励统称，包含绝巴哈、绝神兵、绝亚、绝龙诗、绝欧、绝伊甸和绝妖星。 |

## 当前结构

- `Phantom.csproj`：Dalamud API 15 插件项目配置，当前版本 0.1.12.0。
- `Phantom.json`：卫月插件清单（含 IconUrl、AssemblyVersion 0.1.12.0）。
- `repo.json`：仓库发布清单，下载链接指向 GitHub Release。
- `Plugin/PhantomPlugin.cs`：插件入口、命令注册、UI 生命周期。
- `Infrastructure/DalamudApi.cs`：Dalamud 服务注入（含 IPlayerState、ITextureProvider）。
- `Configuration/PluginConfiguration.cs`：插件配置与进度持久化。
- `Features/PhantomWeapons/PhantomWeaponGuide.cs`：幻境武器阶段静态资料、秘影目标、讨伐任务分组、武器进度总览资料。
- `Features/PhantomWeapons/SecretKillTracker.cs`：聊天击杀/讨伐任务/探索记忆自动标记。
- `Features/PhantomWeapons/FateTracker.cs`：金牌 FATE 自动检测。
- `Features/Navigation/VnavService.cs`：导航 IPC 服务，含幻境村路线状态机。
- `UI/PluginUI.cs`：主窗口、左侧系列导航、自绘阶段页签、武器进度总览、妖表页面、悬浮窗和设置页。
- `Features/Yokai/YokaiWatchGuide.cs`：妖表联动奖励定义。
- `Features/Yokai/YokaiProgressService.cs`：妖表奖励扫描，支持背包、关键道具、装备栏、兵装库、鞍囊、收藏柜、投影台和雇员缓存。
- `Features/Manderville/MandervilleWeaponGuide.cs`：曼德维尔武器四阶段任务与材料资料。
- `Features/RelicWeapons/RelicWeaponGuide.cs`：古武、魂武、优武、义武、天钢、莫雯、宇宙和绝武的阶段资料。
- `Features/RelicWeapons/WeaponItemIds.cs` / `Features/RelicWeapons/weapon-item-ids.txt`：古武至绝武九个系列的固定 `Item.RowId` 映射及其加载器（802 条物品映射）。
- `docs/`：设计文档、使用说明（usage.html）、发布指南（release.md）、幻境村路由移植指南（occult-village-route-porting.md）。

## 当前进度

已完成的主功能：

- 幻境武器各阶段的静态资料展示。
- 一次性流程勾选与持久化。
- 秘影阶段按地图的目标清单、坐标、导航。
- 悬浮窗口显示当前地图秘影进度。
- 聊天消息自动标记击杀、讨伐任务组、探索记忆组和金牌 FATE。
- 总览页按当前角色汇总职业收藏、绝武收藏、妖表奖励、雇员缓存覆盖和各系列完成度，支持一键同步全部系列。
- 各系列武器进度页支持按职业分组、物品图标、角色维度保存和库存自动同步。
- 妖表联动奖励扫描，支持隐藏已获得奖励、投影台缓存状态和按类别展示。
- 曼德维尔武器四阶段资料展示，支持任务与材料的手动进度保存。
- 古武、魂武、优武、义武、天钢、莫雯、宇宙和绝武资料页，复用阶段页签、任务勾选、材料手动进度和固定 `Item.RowId` 职业持有同步。
- 绝武按七个绝本独立统计，并提供“总进度”页；各绝本不是线性阶段，只有实际持有对应武器时才点亮。
- 武器扫描读取背包、兵装库、装备栏、鞍囊、收藏柜、投影台和 `ItemFinderModule.RetainerInventories` 雇员缓存。
- 古武本我阶段将原版和“（复制品）”视为同一阶段持有；骑士同时检查剑与盾。
- 优武的资料页签与武器进度均在丰水后显示“补正”；该阶段对应禁地兵装·改装，并识别各职业的“·改”武器，例如学者“杰巴特·改”。
- 幻武进度页在职业卡片上方显示独立的“达成奖励”区域，收录烹调师副手奖励“幻境菜刀”；该奖励不占用五阶段进度条，持有时状态显示为“完成”。
- 幻武普通职业武器的 110 个物品，以及古武、魂武、优武、义武、曼武、天钢、莫雯、宇宙和绝武的 802 个物品映射，均使用固定 `Item.RowId` 匹配；因此不依赖客户端显示语言。国际服实际库存同步验证待完成。
- 武器和妖表同步按钮使用悬浮提示说明扫描范围；每个系列页面提供对应 Wiki 按钮，古武页面分别提供上古武器和黄道武器 Wiki 按钮。
- DEBUG 设置提供“同步时输出物品位置”开关和可选的“导出幻武 Item.RowId”按钮。后者将中文客户端物品表中的职业、阶段、物品 ID 和名称写入插件配置目录下的 `phantom-item-ids.txt`。开启位置输出后，同步会输出每个候选物品的具体位置；未命中时输出“未找到”。妖表的特殊物品、坐骑、肖像和宠物按解锁状态输出，武器和幻境菜刀按库存位置输出。DEBUG 系列名称使用短称，例如“幻武”“绝武”“古武”“魂武”。
- 设置页提供“整理背包”功能：从当前四个普通背包读取物品，按 `itemid` 合并相同物品，支持按名称搜索并选择；点击整理后将选中物品尽可能移动到普通或高级鞍囊，容量不足时保留剩余物品并在聊天栏报告结果。
- 左侧导航显示当前角色与插件状态，右侧阶段页签使用与武器进度卡片一致的自绘配色。
- vnavmesh + Lifestream 的两段式导航，以及“前往幻境村”独立入口。

当前还保留的手动项：

- 材料进度仍然手填。
- 任务状态仍然手动勾选。
- 武器页面的材料进度仍然手填；“整理背包”只处理用户主动选择的物品，不自动将所有背包材料纳入武器进度。
- 幻境菜刀达成奖励仍按当前客户端物品名称匹配，尚未完成跨语言迁移。

## 数据模型

幻境武器资料目前以内置静态数据保存，避免运行期依赖网络。

- `PhantomWeaponStage`：一个武器阶段，例如半影、本影、黯影、蚀影、秘影。
- `PhantomWeaponRequirement`：可录入进度的材料、以太或记忆项目。
- `PhantomWeaponTask`：一次性流程，用配置中的 `CompletedTasks` 保存勾选状态。
- `PhantomWeaponReward`：可重复来源，例如副本、FATE、CE 的奖励数量。
- `PhantomWeaponTarget`：秘影目标小怪，支持地图显示坐标（MapX/MapY）或直接世界坐标（WorldX/WorldY/WorldZ，`UseWorldCoords` 为 true 时使用）。
- `PhantomWeaponDuty` / `PhantomWeaponDutyGroup`：秘影讨伐任务及其分组（练级迷宫、顶级迷宫、讨伐歼灭战、团队任务、阿卡狄亚登天斗技场）。
- `PhantomWeaponProgressStage` / `PhantomWeaponJob`：武器进度总览用的阶段与职业静态资料（各阶段物品名）。
- `PhantomWeaponGuide.ProgressItemIds`：幻武普通职业武器的固定 `Item.RowId` 映射，包含 22 个战斗职业、5 个阶段及骑士剑盾共 110 个物品。
- `PhantomRewardWeapon`：不属于五阶段职业武器进度的幻武达成奖励，目前包含烹调师副手“幻境菜刀”。

用户进度保存在 `PluginConfiguration`：

- `SelectedStageIndex`：当前选中的阶段。
- `SelectedMandervilleStageIndex`：曼德维尔页面当前选中的阶段。
- `Progress`：键为资料项 Key，值为当前进度。
- `CompletedTasks`：已完成的一次性流程 Key（含秘影目标、讨伐任务）。
- `FloatingSecretTerritoryType` / `FloatingManualMode`：悬浮窗当前地图与手动模式。
- `ShowSecretTargetsInFloatingWindow` / `ShowSecretDutiesInFloatingWindow` / `AutoHideCompletedFloatingItems`：悬浮窗显示选项。
- `ShowAvailableFatesInFloatingWindow`：在悬浮窗显示当前地图可参与的 FATE 及导航按钮。
- `GroupWeaponProgressByRole` / `ShowWeaponProgressIcons`：武器进度总览选项。
- `WeaponProgressByCharacter` / `WeaponProgressItemsByCharacter` / `WeaponProgressSyncTimes`：按角色维度的武器进度总览数据。
- `YokaiOwnedRewardKeysByCharacter` / `YokaiSyncTimesByCharacter`：按角色维度保存妖表奖励和同步时间。
- `HideOwnedYokaiRewards`：妖表页面是否隐藏已获得奖励。
- `DebugLogSyncedItemLocations`：同步时是否在聊天栏输出候选物品的库存位置或“未找到”状态。
- `DebugLogMissingItemLocations`：是否输出未找到的候选物品；仅在 `DebugLogSyncedItemLocations` 开启时显示和生效。
- `BackpackOrganizeItemIds`：用户选择的待整理物品 `itemid` 集合；选择结果持久化保存，物品暂时不在背包中时仍保留。
- `TuliyollalAetheryteId`：旧配置字段（默认 13），幻境村路由当前使用固定常量 216，不再读取此字段。

## 总览与库存同步

总览页只展示能够由现有配置、Lumina Item 表和游戏库存可靠计算的数据，不生成虚构任务或材料统计。

顶部汇总：

- `职业收藏`：十个自动扫描系列中，至少持有一个阶段物品的职业数量。
- `绝武收藏`：按“职业 × 绝本”独立计数；骑士同一绝本的剑盾记录在同一阶段。
- `妖表奖励`：当前角色已保存的妖表奖励数量。
- `库存覆盖`：本次会话从服务器刷新过的雇员数，以及最近一次武器同步时间。

“刷新扫描”调用 `SyncAllCurrentCharacterProgress()`：

- 依次同步幻武、古武、魂武、优武、义武、曼武、天钢、莫雯、宇宙和绝武。
- 同步妖表奖励，并按当前角色 ContentId 保存。
- 系列卡片显示“至少持有一个阶段的职业数 / 该系列职业总数”，点击进入对应栏目。

### 物品匹配

- 静态职业数据保留各阶段中文物品名作为展示资料；幻武普通职业武器使用 `ProgressItemIds`，其余九个系列使用嵌入的 `weapon-item-ids.txt`，均以固定 `Item.RowId` 建立 lookup。运行时按 ID 从当前客户端 `Item` 表获取图标和本地化名称，因此不依赖中文、英文、日文或法文的物品名称。
- 古武本我阶段的原版与复制品、以及骑士剑盾均写入同一阶段的固定 ID 映射；优武补正阶段使用各职业“·改”武器的固定 ID。
- 幻武达成奖励使用独立 lookup 和角色同步键，不计入普通职业阶段数量；当前奖励物品按完整名称“幻境菜刀”匹配。
- 空物品名表示该绝本或阶段当时没有对应职业武器，构建 lookup 时跳过。
- 绝武总进度的每一段独立读取对应绝本的同步结果，不根据最高阶段补亮前段。

### 库存与缓存语义

- `InventoryManager` 提供当前已加载的背包、兵装库、装备栏和库存容器。
- 背包、兵装库和装备中属于当前角色实时读取范围。
- `ItemFinderModule.SaddleBagItemIds` 与 `PremiumSaddleBagItemIds` 提供鞍囊缓存；`GlamourDresserItemIds` 提供投影台缓存。
- 收藏柜优先使用 `UIState.Cabinet` 的当前角色状态，未加载时回退到 `ItemFinderModule.CabinetItemUnlockBits` 缓存。
- `ItemFinderModule.RetainerInventories` 保存雇员装备槽与库存槽的客户端缓存；插件直接扫描其中的 `EquippedItemIds` 和 `ItemIds`。
- `ItemFinderModule.IsRetainerCurrent(retainerId)` 为 `true` 表示该雇员已在本次登录后打开，数据由当前会话从服务器加载。
- 重登后本地缓存通常仍可读取，但 `IsRetainerCurrent` 会重置，因此缓存可能不是最新数据。
- `/道具检索 物品` 可刷新当前检索物品的 `ItemFinderModule.Result`，也可用于刷新投影台相关缓存；该结果参与同步并写入插件角色进度缓存，但不会刷新完整雇员库存。
- 要确保完整雇员库存最新，仍需通过传唤铃打开对应雇员后再执行同步。
- 鞍囊、收藏柜、投影台和雇员数据依赖游戏缓存；缓存未加载或已过期时，插件不能将其可靠地当作实时空数据。
- 幻境菜刀与普通幻武使用相同的实时库存和客户端缓存扫描范围；同步时会单独保存其持有结果，DEBUG 开启后也会输出其位置。

### 整理背包

- “选择物品”弹窗每次打开时重新扫描 `Inventory1` 至 `Inventory4`，只展示当前普通背包中的物品。
- 相同 `itemid` 的多个栏位合并为一项，并显示合计数量；名称仅用于展示和搜索，实际选择和移动始终以规范化后的 `itemid` 为准。
- 已选 `itemid` 保存在 `BackpackOrganizeItemIds`，不会因为物品暂时离开背包而自动删除。
- “整理背包”扫描普通背包中的已选物品，并尝试移动到 `SaddleBag1`、`SaddleBag2`、`PremiumSaddleBag1` 和 `PremiumSaddleBag2`。
- 目标栏位为空或已放置相同 `itemid` 时可以作为目标；不同物品占用的栏位会跳过。
- 每个背包堆会尽可能移动全部数量。鞍囊容量不足、目标容器不可用或移动失败时，剩余数量留在普通背包。
- 操作结束后通过 Echo 聊天消息报告每个已选物品的已转移数量和剩余数量。

总览显示文本：

```text
背包、兵装库与装备中：实时读取
鞍囊、收藏柜与投影台：读取游戏缓存（可能不是最新数据）
雇员库存：0/7 个已在本次登录后打开并刷新，7/7 个已缓存（可能不是最新数据）
/道具检索 物品 可刷新投影台
```

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

已实现的反向转换公式（取自 `VnavService.TryResolveWorldPosition`）：

```csharp
worldX = 50f * mapX - map.OffsetX - 102400f / scale - 50f;
worldZ = 50f * mapY - map.OffsetY - 102400f / scale - 50f;
```

部分目标已用已验证的世界坐标直接硬编码（`UseWorldCoords = true`），跳过上述转换，例如：

- 小亚波伦（克扎玛乌卡湿地）：`(-462.42, 119.82, -29.59)`
- 拟鸟枝（亚克特尔树海）：`(-312.23, -144.16, 140.4)`
- 蓝叶灵（亚克特尔树海）：`(-636.24, -158.37, 214.53)`

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

## 前往幻境村路由

新增独立入口“前往幻境村”（`VnavService.GoToOccultVillage`），不依赖秘影目标导航。路由为纯步行，不上坐骑、不飞行。

### 固定常量（VnavService.cs）

- `TuliyollalTerritoryType = 1185`：图莱尤拉。
- `OccultVillageTerritoryType = 1278`：幻境村。
- `TuliyollalAetheryteId = 216`：图莱尤拉主城以太之光（勿用字符串“珠串万货街”）。
- `OccultVillageAethernetId = 239`：幻境村以太网目的地。
- `OccultVillageDestination = (26.98, 0, 11.26)`：幻境村目标点。

### 状态机（OccultVillageRouteStep）

```
None → WaitingTuliyollal → WaitingOccultVillageAethernet → MovingToDestination → None
```

流程：

1. 已在幻境村（1278）：直接 `SnapToNavmesh` 后步行导航到目标点。
2. 否则 `Lifestream.Teleport(216, 0)` 传送到图莱尤拉，进入 `WaitingTuliyollal`。
3. `WaitingTuliyollal`：等待 `TerritoryType == 1185`、不在读图、`Lifestream.IsBusy == false`、`Lifestream.GetActiveAetheryte() == 216`（必须确认根水晶已激活，否则 `AethernetTeleportById(239)` 会报 `Destination could not be found (3)`）。满足后调用 `Lifestream.AethernetTeleportById(239)`。
4. `WaitingOccultVillageAethernet`：等待读图完成、`TerritoryType == 1278`、本地玩家存在、vnavmesh ready，取得目标点后进入步行导航。
5. `MovingToDestination`：距离目标点 ≤ 4 单位视为到达；离开 1278 则取消；若 7 秒内移动 < 2.5 单位则重试（最多 3 次）；整体超时 60 秒取消。

### 超时与清理

- 全程超过 90 秒自动取消。
- `Stop()` 会清空所有 pending 状态并把 `occultRouteStep` 重置为 `None`，防止持续触发。

### Lifestream IPC

仅 `Lifestream.Teleport` 使用 `ICallGateSubscriber<uint, byte, bool>`。`AethernetTeleportById` 使用 `ICallGateSubscriber<uint, bool>`。IPC 通过 `InstalledPlugins` 判断 Lifestream 是否已加载，未加载时提示失败而不是崩溃。

## 飞行选项

秘影目标导航需要增加“飞行”选框。

配置建议：

- `UseFlightNavigation`：是否把 `fly = true` 传给 vnavmesh。

行为：

- 勾选后，导航调用 `vnavmesh.SimpleMove.PathfindAndMoveTo(target, true)`。
- 未勾选时，调用 `vnavmesh.SimpleMove.PathfindAndMoveTo(target, false)`。
- 如果目标地图未解锁飞行，vnavmesh 或游戏状态可能导致飞行路径失败，应允许用户取消并改用步行。

前往幻境村路由固定使用步行（`fly = false`），不使用此选项。

## UI 设计

### 主窗口结构

- 左侧固定导航栏分为“武器工坊”和“Tools”两组。
- 左侧显示品牌、系列入口、数量、当前角色和插件启用状态。
- 左侧角色状态上方依次提供“前往幻境村”和“反馈与建议”按钮；后者在默认浏览器打开 Discord 反馈频道。启用状态只读显示，插件默认启用且不在幻武页重复提供开关。
- 右侧显示当前系列内容；幻境武器页面顶部为横向阶段页签。
- 幻武页面顶部只保留当前阶段进度重置操作，不显示全局导航和悬浮窗选项。
- 飞行导航、悬浮窗、自动标记击杀等全局开关集中放在设置页。
- 阶段页签使用自绘按钮，选中项为深青背景、亮青边框和左侧高亮条，未选中项使用更暗的武器进度卡片配色。
- 阶段页签只使用 `InvisibleButton` 作为布局占位，背景和文字通过窗口 DrawList 绘制，避免 `SameLine` 产生阶梯状错位。
- 当前角色信息只在左侧导航显示，妖表页面和幻境武器进度页面不再重复显示。

秘影阶段 UI 应拆成两个区块：

- 地图级进度：每张地图显示 `4 个目标 + 5 个 FATE` 的完成情况。
- 目标列表：每个目标显示坐标、勾选框、导航按钮。

一次性流程继续使用醒目标记：

- 标题：`*** 仅需完成一次的流程 ***`
- 勾选项前缀：`[仅一次]`

已实现的其他 UI 元素：

- 左侧角色状态上方“前往幻境村”按钮 → `VnavService.GoToOccultVillage()`。
- 左侧“反馈与建议”按钮 → 打开 <https://discord.com/channels/1258981591124938762/1533030634623074466>。
- 武器进度总览（按职业/阶段/角色维度，含物品名片段匹配与图标）。
- 悬浮窗显示选项：是否显示秘影目标、是否显示讨伐任务、自动隐藏已完成项。
- 设置页提供“悬浮窗显示可参与 FATE”开关；开启后悬浮窗按距离列出当前地图处于准备/进行中的未完成 FATE，每项提供导航按钮。
- 设置页在常用设置与 DEBUG 区域之间提供“整理背包”区域，包含“选择物品”和“整理背包”按钮，并显示当前已选择的物品种类数量。物品选择弹窗使用固定尺寸和内部滚动列表，避免物品较多时撑大窗口。
- 悬浮窗最上方显示“当前可参与 FATE”，按距离列出当前地图处于准备/进行中的未完成 FATE，每项提供导航按钮。
- FATE 导航复用 `VnavService`：目标较远时先前往附近以太水晶，再使用 vnavmesh 导航；目标较近时直接导航；找不到附近网格点时尝试直接使用 FATE 原始坐标。
- 在蜃景幻界新月岛南征之章（`TerritoryType = 1252`）和北征之章（`TerritoryType = 1346`）点击 FATE 导航或“最近 FATE”时，不执行导航，提示“【新月岛地图】请使用【新月岛史官】插件。”；优雷卡四张地图（`732`、`763`、`795`、`827`）及博兹雅南方战线、扎杜诺尔（`920`、`975`）执行相同操作时，提示“【博兹雅/优雷卡】暂不支持该地图。”。以上八个地图 ID 均已由游戏内 DEBUG 输出确认。

### 妖表联动页面

页面顶部显示分行攻略提示：

1. 找 NPC 开启活动后，带上妖怪手表，先去刷 FATE，拿奖励兑换宠物。
2. 带着宠物后才会掉落兑换武器的材料，材料不是必出，最后用武器材料兑换对应武器。
3. 注意：不同宠物掉落的材料不一样。该行使用红色强调。

奖励按妖怪手表、坐骑、肖像、宠物和武器分类展示。特殊物品、坐骑、肖像和宠物通过对应解锁状态判断；武器同步扫描背包、关键道具、装备栏、兵装库、鞍囊、收藏柜、投影台和雇员缓存，结果按角色 ContentId 保存。

### 曼德维尔武器页面

曼德维尔页面复用幻武的阶段资料展示组件，但使用独立的阶段选择配置和资料 Key：

- 曼德维尔武器：iLvl 615，稀少陨石 ×3，1500 亚拉戈诗学神典石。
- 曼德维尔武器·惊异：iLvl 630，稀少球粒陨石 ×3，1500 亚拉戈诗学神典石。
- 曼德维尔武器·威严：iLvl 645，稀少无球粒陨石 ×3，1500 亚拉戈诗学神典石。
- 曼德维尔武器·盈满：iLvl 665，雏晶 ×3，1500 亚拉戈诗学神典石。

当前版本提供 Wiki 按钮、一次性任务勾选、材料手动进度和武器持有自动扫描。资料来源：<https://ff14.huijiwiki.com/wiki/%E6%9B%BC%E5%BE%B7%E7%BB%B4%E5%B0%94%E6%AD%A6%E5%99%A8>。

## 战斗记忆界面读取（未完成）

目标：从游戏内“战斗的记忆”界面读取秘影阶段进度，减少手动勾选。

可选方案：

- 方案 A：读取已打开的 Addon UI。
- 需要玩家先打开“战斗的记忆”界面，再点击插件 DEBUG 按钮读取。
- 通过 `GameGui.GetAddonByName()` 定位 Addon，遍历 Atk 节点读取文字和勾选图标状态。
- 优点：实现风险低，不需要逆向任务内存结构。
- 缺点：依赖 Addon 名称和节点顺序；游戏更新或 UI 改版后可能失效；必须打开界面。

- 方案 B：读取任务/状态内存数据。
- 直接定位游戏保存战斗记忆进度的数据结构，不依赖界面是否打开。
- 优点：可自动同步，不需要用户打开界面。
- 缺点：需要逆向内存结构，维护成本高，版本更新风险高，不建议首选。

当前决策：该功能尚未实现，DEBUG 页已移除“读取幻境村入口坐标”按钮，仅保留读取当前坐标、测试坐标换算、解析地图参数三个调试能力。后续实现前需要先增加“列出当前 Addon”调试能力，确认“战斗的记忆”界面的 Addon 名称和节点结构。

## 聊天自动标记

`SecretKillTracker` 监听 `ChatGui.ChatMessage`，命中后自动写入进度并保存。以下功能均已实现。

### 击杀小怪

- 消息必须同时包含“战斗的记忆”“讨伐”“只”和目标名，例如“战斗的记忆：讨伐1只图拉尔蜜獾”。
- 不再使用“打倒/击倒/消灭/defeat/slay”等泛关键词，避免普通击杀聊天误判。
- 匹配当前 `TerritoryType` 下未完成的秘影目标名，命中即加入 `CompletedTasks`。

### 讨伐任务分组完成

- 识别“战斗的记忆……完成……”类消息（`LooksLikeSecretDutyMessage`）。
- 若消息包含“所有项目”且匹配到某分组名（练级迷宫/顶级迷宫/讨伐歼灭战/团队任务/阿卡狄亚登天斗技场），一次勾选该分组全部副本（`MatchesDutyGroup`）。
- 否则逐个匹配分组内的副本名，命中单个副本即标记其完成（`MatchesDuty`）。

### 探索记忆分组完成

- 消息包含“所有项目”且匹配探索记忆分组名（`ExplorationMemoryGroups`）时：
  - 尤卡图拉尔 → 奥阔帕恰山 + 克扎玛乌卡湿地 + 亚克特尔树海。
  - 萨卡图拉尔 → 夏劳尼荒野 + 遗产之地。
  - 无失世界 → 活着的记忆。
  - 同时支持单地图：克扎玛乌卡湿地、亚克特尔树海、夏劳尼荒野、遗产之地。
- 将对应地图的所有秘影目标勾选，并把该地图的 FATE 进度 `Progress["secret-fate-{TerritoryType}"]` 设为 5。

## 实现注意事项

- 插件内置资料应注明来源日期，后续资料变动需要人工更新。
- 导航服务必须容忍 vnavmesh 或 Lifestream 未安装、未加载、IPC 不可用。
- Lifestream 传送后需要等待 `Lifestream.IsBusy == false` 且 vnavmesh ready，再继续路径导航。
- 幻境村路由使用固定常量 216/239/1185/1278，不使用字符串地名，避免本地化差异。
- 当前工作区可能不是 Git 仓库，不依赖 Git 操作。
- 构建命令：`dotnet build`
- 构建产物：`output/Phantom.dll`
- 发布流程见 `docs/release.md`（tag 推送触发 GitHub Actions）。
