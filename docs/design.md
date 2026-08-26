# 肝武助手设计文档

> 当前版本：0.1.25.0 | 更新日期：2026-08-16

## 目标

肝武助手是一个 Dalamud/卫月插件，用于辅助《最终幻想 XIV》多系列武器与生产工具制作流程，当前已接入幻境武器、雅武、曼德维尔武器、旧资料片肝武、生产采集特殊工具、绝武和妖怪手表联动奖励追踪。

当前目标：

- 以左侧系列导航统一管理古武、魂武、优武、义武、曼武、雅武、幻武、天钢、莫雯、宇宙、绝武和妖表联动。
- 展示幻境武器各阶段所需材料、一次性流程和可重复来源。
- 自动同步当前角色的幻武、雅武、旧肝武、曼武、特殊工具、绝武持有进度和妖表奖励状态。
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
- 雅武（优雅武器兑换）：https://ff14.huijiwiki.com/wiki/%E7%89%A9%E5%93%81:%E5%85%A8%E5%A4%A9%E5%BC%BA%E5%8C%96%E8%8D%AF
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
| 雅武 | Elegant Weapons | 6.x 变体迷宫武器系列；追踪改良型基础武器和“·优雅”强化武器。 |
| 幻武 | Phantom Weapons | 7.x 幻境武器系列。 |
| 天钢 | Skysteel Tools | 生产采集职业工具系列。 |
| 莫雯 | Splendorous Tools | 生产采集职业工具系列。 |
| 宇宙 | Cosmic Tools | 生产采集职业工具系列。 |
| 绝武 | Ultimate Weapons | 绝境战武器奖励统称，包含绝巴哈、绝神兵、绝亚、绝龙诗、绝欧、绝伊甸和绝妖星。 |

## 当前结构

- `Phantom.csproj`：Dalamud API 15 插件项目配置，当前发布目标版本 0.1.31.0。
- `Phantom.json`：卫月插件清单（含 IconUrl，当前发布目标 AssemblyVersion 0.1.31.0）。
- `repo.json`：仓库发布清单，下载链接指向 GitHub Release。
- `Plugin/PhantomPlugin.cs`：插件入口、命令注册、UI 生命周期。
- `Infrastructure/DalamudApi.cs`：Dalamud 服务注入（含 IPlayerState、ITextureProvider、IGameGui）。
- `Configuration/PluginConfiguration.cs`：插件配置与进度持久化。
- `Features/PhantomWeapons/PhantomWeaponGuide.cs`：幻境武器阶段静态资料、秘影目标、讨伐任务分组、武器进度总览资料。
- `Features/PhantomWeapons/SecretKillTracker.cs`：聊天击杀/讨伐任务/探索记忆自动标记。
- `Features/PhantomWeapons/FateTracker.cs`：金牌 FATE 自动检测。
- `Features/Navigation/VnavService.cs`：导航 IPC 服务，含幻境村路线状态机。
- `Features/Fates/FateNotificationService.cs`：通用 FATE 关注提醒，合并手动关注和黄道文书自动关注来源。
- `Features/Fates/EdgeTtsService.cs`：可选 EdgeTTS.Dalamud IPC 语音播报服务。
- `Features/Duties/AutoDutyService.cs`：可选 AutoDuty IPC 集成，按副本名解析客户端副本数据并启动单次自动流程。
- `Features/Hunt/HuntAssistant.cs`：狩猎车头 Flag 监听、地图链接坐标解析与自动飞行导航。
- `UI/PluginUI.cs`：主窗口、左侧系列导航、自绘阶段页签、武器进度总览、妖表页面、悬浮窗和设置页。
- `Features/Yokai/YokaiWatchGuide.cs`：妖表联动奖励定义。
- `Features/Yokai/YokaiProgressService.cs`：妖表奖励扫描，支持背包、关键道具、装备栏、兵装库、鞍囊、收藏柜、投影台和雇员缓存。
- `Features/Manderville/MandervilleWeaponGuide.cs`：曼德维尔武器四阶段任务与材料资料。
- `Features/RelicWeapons/RelicWeaponGuide.cs`：雅武、古武、魂武、优武、义武、天钢、莫雯、宇宙和绝武的阶段资料。
- `Features/RelicWeapons/ZodiacProgressModels.cs`：古武角色/职业独立进度，以及怪物、FATE、副本、理符和黄道十二文书目标模型。
- `Features/RelicWeapons/ZodiacGuide.cs`：12 个魂晶地区和 9 本黄道文书的静态目标数据。
- `Features/RelicWeapons/WeaponItemIds.cs` / `Features/RelicWeapons/weapon-item-ids.txt`：雅武、古武至绝武十个系列的固定 `Item.RowId` 映射及其加载器。
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
- 妖表坐骑“地缚猫沙发”使用固定 `MountId = 228` 检查解锁状态，避免仅依赖物品表中的坐骑道具关联数据。
- 妖表武器卡片提示对应的妖表宠物、职业、所需妖怪传奇徽章和可获取地区；右键武器卡片会复制完整提示文本，并通过 `[Phantom] [右键] [妖表]` 聊天消息确认。
- 曼德维尔武器四阶段资料展示，支持任务与材料的手动进度保存。
- 雅武页面追踪 22 个职业的“基础武器”和“优雅”两阶段持有状态，包含青魔法师（`blu`）；职业与武器归属、固定 `Item.RowId` 映射已按当前中文客户端物品表校正，并提供优雅武器兑换 Wiki 跳转。
- 古武、魂武、优武、义武、天钢、莫雯、宇宙和绝武资料页，复用阶段页签、任务勾选、材料手动进度和固定 `Item.RowId` 职业持有同步。
- 绝武按七个绝本独立统计，并提供“总进度”页；各绝本不是线性阶段，只有实际持有对应武器时才点亮。
- 绝欧固定映射已按物品职业限制校正：忍者对应 `39169`“绝境欧米茄夺命镰”，钐镰客对应 `39182`“绝境欧米茄扎戈斧镰”。
- 武器扫描读取背包、兵装库、装备栏、鞍囊、收藏柜、投影台和 `ItemFinderModule.RetainerInventories` 雇员缓存。
- 古武本我阶段将原版和“（复制品）”视为同一阶段持有；骑士同时检查剑与盾。
- 优武的资料页签与武器进度均在丰水后显示“补正”；该阶段对应禁地兵装·改装，并识别各职业的“·改”武器，例如学者“杰巴特·改”。
- 幻武进度页在职业卡片上方显示独立的“达成奖励”区域，收录烹调师副手奖励“幻境菜刀”；该奖励不占用五阶段进度条，持有时状态显示为“完成”。
- 幻武普通职业武器的 110 个物品，以及雅武、古武、魂武、优武、义武、曼武、天钢、莫雯、宇宙和绝武的固定物品映射，均使用固定 `Item.RowId` 匹配；因此不依赖客户端显示语言。国际服实际库存同步验证待完成。
- 雅武基础阶段除改良型武器 ID 外，也包含对应优雅武器 ID；因此持有优雅武器时会自动点亮同职业的基础武器和优雅两个阶段。骑士剑盾在同一职业阶段合并处理。
- 武器和妖表同步按钮使用悬浮提示说明扫描范围；每个系列页面提供对应 Wiki 按钮，古武页面分别提供上古武器和黄道武器 Wiki 按钮。
- DEBUG 设置提供“同步时输出物品位置”开关，以及“导出幻武 Item.RowId”和“读取雅武 Item.RowId”按钮。二者将中文客户端物品表中的职业、阶段、物品 ID 和名称分别写入插件配置目录下的 `phantom-item-ids.txt` 和 `elegant-weapon-item-ids.txt`。开启位置输出后，同步会输出每个候选物品的具体位置；未命中时输出“未找到”。妖表的特殊物品、坐骑、肖像和宠物按解锁状态输出，武器和幻境菜刀按库存位置输出。DEBUG 系列名称使用短称，例如“幻武”“雅武”“绝武”“古武”“魂武”。
- 深层迷宫武器按死者宫殿、天之御柱、正统优雷卡和朝圣交错路四个独立系列展示；除天之御柱外各有两个独立阶段，只按当前实际持有物品统计，不进行阶段补全。骑士剑盾合并为一个职业阶段格，必须同时持有才算完成。
- 深层迷宫武器提供 DEBUG 总导出按钮，可将四个迷宫的 154 个 Item ID 导出到插件配置目录下的 `deep-dungeon-weapon-item-ids.txt`；运行时会与内置固定映射合并参与同步。
- 设置页提供“整理背包”功能：从当前四个普通背包读取物品，按 `itemid` 合并相同物品，支持按名称搜索并选择，已选物品在列表中置顶；点击整理后将选中物品尽可能移动到普通或高级鞍囊，容量不足时保留剩余物品并在聊天栏报告结果。
- 设置页在悬浮窗选项下提供“前往 Flag”互斥选项，可选择“直接前往”或“按导航前往”，并使用分割线与同步设置区分。
- 设置页使用分组卡片布局：常用设置以双列卡片展示，前往 Flag 与同步工具分别使用独立面板；同步工具按钮按固定分组换行，避免在窄窗口中被挤压或裁剪。
- 幻武秘影阶段的“重置当前阶段进度”会同时清除六张地图的目标、FATE 进度和全部讨伐任务勾选，不只重置阶段材料与一次性任务。
- 左侧导航显示当前角色与插件状态，右侧阶段页签使用与武器进度卡片一致的自绘配色。
- vnavmesh + Lifestream 的两段式导航，以及“前往幻境村”独立入口。
- 飞行导航会先尝试上坐骑，8 秒内未成功才直接发起 vnavmesh；抵达飞行导航目标 8 yalms 内时停止路径并以 `ActionType.Mount, 0` 自动下坐骑，每 500ms 重试至游戏确认已下坐骑。
- 飞行导航支持按调用场景控制抵达后的下坐骑行为；狩猎助手抵达目标后保持骑乘，危命和其他普通导航保留原有自动下坐骑逻辑。
- 狩猎助手可指定车头，监听其聊天中的地图 Flag，并支持跨地图自动传送后飞行导航到目标点。
- 车头身份优先从聊天 `PlayerPayload.PlayerName` 读取，该字段不包含服务器名；配置只需填写角色名，例如填写 `津见宇` 即可匹配聊天中显示为“津见宇 + 服务器名”的玩家。若同一车头在 5 秒内重复发送相同地图和坐标的 Flag，只处理第一次，避免重复覆盖传送/导航状态。

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
- `ShowSecretTargetsInFloatingWindow` / `ShowSecretDutiesInFloatingWindow` / `AutoHideCompletedFloatingItems`：悬浮窗显示选项（秘影指定目标、迷宫/讨伐任务分组、自动隐藏已完成项）。
- `ShowAvailableFatesInFloatingWindow`：在悬浮窗显示当前地图可参与的 FATE 及导航按钮。
- `ZodiacFateNotificationsEnabled`：是否启用通用 FATE 关注提醒。
- `AutoTrackSelectedZodiacBookFates`：是否将当前角色、职业所选黄道文书中尚未完成的 FATE 作为自动关注来源。
- `PrioritizeZodiacFatesInCatalog`：是否在全部 FATE 目录中将文书 FATE 排在其他未关注 FATE 前面；已关注目标始终优先置顶。
- `ZodiacFateNotificationSound` / `ZodiacFateNotificationEdgeTts`：游戏 SE 音效编号和可选 EdgeTTS 语音开关。
- `ZodiacFateNotificationIntervalSeconds` / `ZodiacFateNotificationRepeatCount`：同一轮 FATE 的提醒间隔和最多提醒次数，默认 15 秒、3 次。
- `TrackedFates`：手动关注列表，以全局唯一 `FateId` 为身份，名称、地图和坐标作为显示元数据。
- 配置版本 `Version = 2` 包含提醒默认值迁移：旧配置首次加载时将提醒间隔和次数统一迁移为 15 秒、3 次并立即保存；迁移后不再覆盖用户后续设置。
- `ShowZodiacMonitorInFloatingWindow`：是否在悬浮窗显示古武监控卡片。
- `FloatingZodiacJobKey` / `FloatingZodiacStageKey`：悬浮窗古武监控当前选择的职业和阶段。
- `HuntAssistantEchoLeaderMessages`：测试开关，以 Echo/默语复述指定车头在任意频道的发言，用于诊断聊天监听和车头匹配。
- `NavigateToFlagDirectly`：前往 Flag 时使用直接前往还是导航前往。
- `SetFlagOnNavigation`：具有明确世界坐标或地图坐标的插件导航是否同步设置游戏地图 Flag，默认开启；仅传送到地图不设置 Flag。
- `HuntAssistantEnabled`：是否监听指定车头发送的狩猎 Flag。
- `ShowHuntAssistantInFloatingWindow`：是否在悬浮窗显示狩猎助手状态。
- `HuntLeaderName`：指定车头的角色名。
- `HuntTargetHeight`：狩猎 Flag 目标接地后向上抬升的距离，默认 50 yalms。
- `ShowNavigationLogs`：是否在聊天栏显示带 `[导航日志]` 前缀的导航过程与状态消息。
- `GroupWeaponProgressByRole` / `ShowWeaponProgressIcons`：武器进度总览选项。
- `WeaponProgressByCharacter` / `WeaponProgressItemsByCharacter` / `WeaponProgressSyncTimes`：按角色维度的武器进度总览数据。
- `YokaiOwnedRewardKeysByCharacter` / `YokaiSyncTimesByCharacter`：按角色维度保存妖表奖励和同步时间。
- `ZodiacProgressByCharacter`：按角色、职业保存古武制作进度；材料数值写入 `RequirementProgress`，任务/目标勾选写入 `CompletedObjectives`。
- `SelectedZodiacJobKey`：古武阶段页当前选择的职业，默认骑士 `pld`。
- `FateAssistantEnabled`：历史 FATE 助手配置字段；当前通用关注提醒使用 `ZodiacFateNotificationsEnabled`。
- `HideOwnedYokaiRewards`：妖表页面是否隐藏已获得奖励。
- `DebugLogSyncedItemLocations`：同步时是否在聊天栏输出候选物品的库存位置或“未找到”状态。
- `DebugLogMissingItemLocations`：是否输出未找到的候选物品；仅在 `DebugLogSyncedItemLocations` 开启时显示和生效。
- `BackpackOrganizeItemIds`：用户选择的待整理物品 `itemid` 集合；选择结果持久化保存，物品暂时不在背包中时仍保留。
- `TuliyollalAetheryteId`：旧配置字段（默认 13），幻境村路由当前使用固定常量 216，不再读取此字段。

### 古武专属进度

- 古武阶段页复用 `PhantomWeaponStage` 的显示结构，但材料和任务不再复用全局 `Progress` / `CompletedTasks`。
- UI 先按当前角色取得 `ZodiacCharacterProgress`，再按所选职业取得 `ZodiacJobProgress`；切换职业会立即切换整套手填进度。
- 未登录时只展示提示，不创建空角色键，避免多个未登录会话共享错误数据。
- `CompletedBooks`、`SelectedBookKey` 和文书四类目标模型已接入黄道十二文书页面。
- `ZodiacGuide.AtmaTerritories` 已提供 12 个魂晶地区的任意 FATE 手动完成目标。
- `ZodiacGuide.AnimusBooks` 已提供 9 本黄道文书的 10 个指定敌人、3 个副本、3 个 FATE 和 3 个理符目标；目标完成状态按角色/职业保存。
- 文书指定敌人已录入 Wiki 核对过的多个刷新坐标；路线型、范围型或缺少明确地点的坐标以文字说明展示，不伪造单点。
- 文书怪物、FATE 和理符目标支持按地图名称请求 Lifestream 传送；目标地图由客户端 Lumina `Map` 表解析，无法解析时保留手动传送方式。
- “传送到地图”只调用 Lifestream，不登记后续移动目标；只有坐标、NPC、FATE 和世界坐标导航才在读图后继续调用 vnavmesh，避免角色传送后围绕大水晶移动。
- 有明确坐标的敌人和 FATE 支持从坐标菜单选择具体刷新点并导航；FATE 的前置 NPC 坐标可单独导航。
- 文书目标同时支持地图二维坐标和实测世界三维坐标。当前地图二维坐标导航以玩家当前高度开始查询 navmesh；跨地图导航在传送落地后以玩家高度重新查询，减少东拉诺西亚、库尔札斯中央高地等多层地图吸附到错误网格。
- 拉诺西亚外地的武伽玛罗矿山目标使用实测洞口世界坐标 `(75.81, 52.79, -540.95)`；主页面先导航到洞口，并允许将洞内小怪刷新点逐个设为 Flag，不从洞外强行规划至地下网格。
- 水天文书第一卷“朗咒巨人”使用三个实测世界坐标：`(-289.82, 292.68, 262.35)`、`(-403.18, 239.67, 279.30)`、`(-437.52, 245.89, 314.31)`。
- 水狱文书第一卷 FATE“青磷大路”位于北萨纳兰，导航至慎重的商人 `(21.8, 29.4)`，与 NPC 对话触发。
- 土天文书第一卷 FATE“试掘地强攻”已按 Wiki 核对：位于拉诺西亚外地 `(23.8, 16.4)`，与黑涡团二等漩兵对话触发。
- 理符目标保存接取 NPC 的地图和坐标，使用 `NPC导航` 前往接取点；列表和悬浮窗显示 `[佣兵理符]` 或 `[军队理符·黑涡团/双蛇党/恒辉队]`，部队归属来自任务本身而非玩家所属军队。
- 古武页面顶部 Wiki 按钮右侧提供“获取当前坐标”，读取当前地图 `(X, Y)` 并复制到剪贴板，用于人工补充或核对 Wiki 坐标。
- 文书页面显示每本书的敌人/副本/FATE/理符四类完成数和总进度，全部 19 个目标完成后自动加入 `CompletedBooks`。
- 文书四类目标区块可折叠，标题显示各类完成数；指定目标的操作列提供地图传送、坐标导航、NPC 导航、洞口/小怪操作或 `AD执行`。
- 黄道十二文书材料项不再允许单独手填，`zodiac-animus-books` 直接由当前角色/职业的 `CompletedBooks` 派生，确保材料进度和文书勾选一致。
- 现有危命助手会将当前角色/职业所选文书中名称匹配的 FATE 显示为 `FATE 名称【文书名】`；这是显示标记，不会写入通用 Phantom FATE 进度。
- `ZodiacMonsterTracker` 复用聊天监听，按当前角色、职业和所选文书累计目标怪物击杀，每个目标需要 3 次；不确定的聊天文本不自动标记。
- 本我阶段提供 12 个独立光阶段，完成数保存到 `RequirementProgress["zodiac-zeta-mahatma"]`。
- 古武页面 Wiki 按钮右侧提供“监控古武”开关，控制是否在悬浮窗显示古武进度和目标；悬浮窗同时显示幻武监控和古武监控时，两个独立卡片按顺序排列，互不覆盖。
- 悬浮窗古武监控卡片可独立选择职业和古武阶段（古武、天极、魂晶、魂灵、新星、镇魂、黄道、本我）；阶段内容读取当前角色/职业的独立进度。
- 古武监控显示：魂晶地区完成数、黄道十二文书完成数和当前文书完成数；其他阶段显示阶段材料进度。
- 古武监控根据当前阶段显示“下一步”：魂晶显示下一个地区 FATE，魂灵显示当前文书的下一个敌人/副本/FATE/理符，其他阶段显示下一个未完成任务或材料。
- 古武监控的“下一步”目标提供操作按钮：魂晶传送到目标地图；敌人、FATE 和理符导航到首个可用精确坐标；副本通过 `AD执行` 请求 AutoDuty 自动完成一次。
- 古武监控卡片标题可点击折叠/展开，第一行提供“停止导航”按钮；幻武监控同样支持标题折叠。两张卡片同时开启时分别保留自己的折叠状态和内容。
- UI 点击区域兼容性规则：不要使用固定宽度的 `Selectable` 作为短标题按钮。不同用户的字体、字体缩放、DPI 或窗口宽度可能使文字超出点击区域，或被同一行右侧控件覆盖，造成“文字可见但点击无效”。标题类交互统一使用按 `ImGui.CalcTextSize(label)` 动态计算宽度的 `Button`，并为同一行的状态文字和操作按钮预留空间。
- 下一步按顺序完成：继续核对剩余 Wiki 坐标和特殊前置条件；实现 FATE 完成自动识别；进入游戏实际验证击杀文本、坐标换算、NPC/FATE 导航和 Lifestream 传送。

## 总览与库存同步

总览页只展示能够由现有配置、Lumina Item 表和游戏库存可靠计算的数据，不生成虚构任务或材料统计。

顶部汇总：

- `职业收藏`：十一个自动扫描系列中，至少持有一个阶段物品的职业数量。
- `绝武收藏`：按“职业 × 绝本”独立计数；骑士同一绝本的剑盾记录在同一阶段。
- `妖表奖励`：当前角色已保存的妖表奖励数量。
- `库存覆盖`：本次会话从服务器刷新过的雇员数，以及最近一次武器同步时间。

“刷新扫描”调用 `SyncAllCurrentCharacterProgress()`：

- 依次同步古武、魂武、优武、义武、曼武、雅武、幻武、天钢、莫雯、宇宙和绝武。
- 同步妖表奖励，并按当前角色 ContentId 保存。
- 系列卡片显示“至少持有一个阶段的职业数 / 该系列职业总数”，点击进入对应栏目。

### 物品匹配

- 静态职业数据保留各阶段中文物品名作为展示资料；幻武普通职业武器使用 `ProgressItemIds`，其余十个系列使用嵌入的 `weapon-item-ids.txt`，均以固定 `Item.RowId` 建立 lookup。运行时按 ID 从当前客户端 `Item` 表获取图标和本地化名称，因此不依赖中文、英文、日文或法文的物品名称。
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

- “选择物品”弹窗每次打开时重新扫描 `Inventory1` 至 `Inventory4`，只展示当前普通背包中的物品；已选择项置顶，已选和未选两组内均按本地化名称排序。
- 相同 `itemid` 的多个栏位合并为一项，并显示合计数量；名称仅用于展示和搜索，实际选择和移动始终以规范化后的 `itemid` 为准。
- 已选 `itemid` 保存在 `BackpackOrganizeItemIds`，不会因为物品暂时离开背包而自动删除。
- 点击“整理背包”时，插件通过 `IGameGui.GetAddonByName()` 检查 `InventoryBuddy` 和 `InventoryBuddy2` 的 `IsVisible`，验证普通或高级陆行鸟鞍囊窗口是否真正打开；不能只依赖库存容器的 `IsLoaded`，因为缓存数据已加载不等于窗口当前可见。窗口未打开时执行 `/陆行鸟鞍囊`，并等待最多 10 秒。
- 只有鞍囊窗口可见且至少一个 `SaddleBag1`、`SaddleBag2`、`PremiumSaddleBag1` 或 `PremiumSaddleBag2` 容器已加载后才开始整理；未开通或未加载的高级鞍囊会跳过。整理期间窗口被关闭或容器卸载时立即停止。
- 每个背包堆分两轮选择目标：第一轮优先寻找鞍囊中同物品、同品质的未满堆；没有可合并堆时，第二轮寻找鞍囊空格并移动整个物品堆。只有所有已加载鞍囊都没有可用空格时才跳过并保留在普通背包。
- 鞍囊窗口与容器首次就绪后额外等待 1 秒再扫描，避免界面刚打开时物品列表尚未填充。移动操作按格串行执行：每次只提交一个请求，优先等待源背包数量减少且目标鞍囊数量增加并稳定 250ms 后确认；部分客户端不会及时刷新鞍囊目标数量，此时源背包数量减少持续稳定 750ms 也视为服务器已接受。源数量恢复会撤销该候选结果；请求被拒绝、鞍囊关闭或 8 秒内始终没有稳定变化时停止，不把短暂空槽当成成功。
- 目标容器不可用或移动失败时停止整理；错误发生后的客户端数量不作为可信结果。正常结束时汇总“已移动”与“跳过”种类数，跳过原因统一为鞍囊没有可用空间。
- 操作结束后通过 Echo 聊天消息报告每个已选物品的本地化名称、`Item.RowId`、已确认转移数量和背包剩余数量；名称始终从 Lumina `Item` 表按 ID 读取。发生拒绝或超时时，剩余数量显示为“未确认”。

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

普通地图传送和目标导航必须保持语义分离：

- `TeleportToMap` 只请求 Lifestream 传送，不保存 `pendingTarget`，因此读图完成后不会向大水晶再次移动。
- 地图坐标、世界坐标、FATE、幻武目标和狩猎目标才保存真实目的地，并在读图稳定、Lifestream 空闲和 vnavmesh ready 后继续导航。
- 开启 `SetFlagOnNavigation` 后，所有具有明确目的地的导航通过 `AgentMap.SetFlagMapMarker` 同步设置 Flag；游戏一次只能保留一个 Flag。
- 仅传送到地图没有真实目的地，不设置 Flag。洞穴刷新点菜单可显式强制设置 Flag，供玩家进入洞穴后定位。
- 东拉诺西亚使用地图级水晶覆盖：所有跨地图导航和地图传送优先选择葡萄酒港，不再按目标距离选择太阳海岸；Flag 和 vnavmesh 终点仍使用真实目标坐标。

## 通用 FATE 关注提醒

`FateNotificationService` 每秒读取一次当前地图 `FateTable`，将手动关注目标和黄道文书自动目标合并后监控。黄道文书只是自动来源，不限制通用提醒功能：

- 手动关注保存在 `PluginConfiguration.TrackedFates`，以全局唯一 `FateId` 作为身份，名称、地图和坐标作为显示元数据。危命助手可从所有静态 FATE 的可搜索下拉目录提前关注，当前地图列表也可快捷关注/取消，关注管理区可查看和移除。悬浮窗只保留状态和导航，不显示关注按钮。
- 全部 FATE 目录不要求先选地图。客户端静态表中的同名 RowId 按显示名称合并，避免旧版本或内部变体重复显示；黄道文书目标追加 `[火天一]`、`[水狱一]`、`[土天一]` 等短标记。
- “文书 FATE 置顶”控制目录排序；整体顺序为已关注优先、文书目标其次、其他 FATE 最后。提前关注时无需已知 Territory，目标首次出现后按 FateId 或名称命中并回填地图与坐标。
- 下拉目录先按搜索词过滤和优先级排序，再最多绘制 300 项，避免一次创建全部静态 FATE 的 ImGui 项导致卡顿；已关注判断使用 FateId 优先、名称回退，与同名合并后的目录行为一致。
- `AutoTrackSelectedZodiacBookFates` 开启时，当前角色、当前古武职业所选黄道文书中尚未完成的指定 FATE 作为自动来源；文书切换不会影响手动关注。
- 仅当前地图可监控；Dalamud `FateTable` 不提供其他地图的实时 FATE。
- FATE 进入 `Preparing` 或 `Running` 后开始提醒；切换到地图时目标已存在也会提醒。
- 提醒格式为 `/echo [Phantom] 关注的 FATE 出现：名称 · 地图 (X, Y) [文书短标记] <se.N>`，通过游戏命令处理 `<se.N>`，而非将标签作为普通聊天文字打印；手动目标不显示文书标记。
- 每个 FATE 独立记录本轮提醒状态。默认每 15 秒提醒一次，共提醒 3 次；设置范围为 5-300 秒、1-10 次。FATE 消失、结束或切换监控上下文后清除本轮计数，下次出现重新计数。
- 可选调用 `EdgeTTS.Speak(string)` 播报“关注的临危受命出现，名称”。EdgeTTS 仅通过 IPC 集成，不引用第三方程序集，语音音色、语速和音量沿用 EdgeTTS.Dalamud 配置。
- 开启语音时检查 `InternalName == "EdgeTTS.Dalamud"` 且插件已加载。未安装时弹窗提供仓库地址 `https://gh.atmoomen.top/raw.githubusercontent.com/AtmoOmen/DalamudPlugins/main/pluginmaster.json` 和复制按钮；已安装但未加载时提示先启用。
- EdgeTTS 运行期间失效只跳过语音并记录一次日志，不影响 Echo 与 SE 音效提醒，也不自动打断或启动导航。

## AutoDuty 联动

`AutoDutyService` 将 AutoDuty 视为可选依赖，不直接引用其程序集：

1. 从客户端 `ContentFinderCondition` 按标准化后的中文副本名解析 `TerritoryType`。
2. 检查 `InstalledPlugins` 中 `InternalName == "AutoDuty"` 且插件已加载。
3. 调用 `AutoDuty.ContentHasPath(uint)` 确认该副本存在可用路径。
4. 调用 `AutoDuty.Run(uint, 1, false)` 启动一次完整自动流程，组队模式、战斗插件和其他行为沿用用户的 AutoDuty 配置。
5. 无法解析副本、插件未加载、无路径或 IPC 异常时只输出明确提示，不执行替代自动操作。

古武文书指定副本、幻武秘影副本主页面和悬浮窗均提供 `AD执行`。讨伐战等内容同样先由 `ContentHasPath` 判定，不假设 AutoDuty 必然支持。

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
- 武器工坊栏目顺序为古武、魂武、优武、义武、曼武、雅武、幻武、天钢、莫雯、宇宙、绝武和妖表联动。
- 右侧显示当前系列内容；幻境武器页面顶部为横向阶段页签。
- 幻武页面顶部提供当前阶段进度重置、Wiki 跳转、“监控幻武”与“自动标记击杀”开关，不显示全局导航选项。
- 设置页包含“常用设置”“导航设置”“前往 Flag”和同步工具；“危命助手”与“狩猎助手”作为 Tools 下的独立栏目。
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
- 悬浮窗显示选项：是否在悬浮窗显示秘影指定目标、迷宫/讨伐任务分组，以及是否自动隐藏已完成项。
- “危命助手”页面提供“在悬浮窗显示可参与 FATE”开关；开启后悬浮窗按距离列出当前地图处于准备/进行中的未完成 FATE，每项提供导航按钮和停止导航按钮。
- 危命助手将“提醒设置”“黄道文书自动关注”“手动关注”拆成独立卡片。手动关注卡片按实际条目数动态调整高度，设置上下限；条目较多时卡片内部滚动，不保留大块空白。
- 手动关注目录支持名称搜索、已关注置顶、可选文书 FATE 置顶及文书短标记；当前地图 FATE 行提供快捷关注/取消。悬浮窗 FATE 行不提供关注按钮，关注管理统一在危命助手主页面完成。
- 设置页在常用设置与 DEBUG 区域之间提供“整理背包”区域，包含“选择物品”和“整理背包”按钮，并显示当前已选择的物品种类数量。物品选择弹窗使用固定尺寸和内部滚动列表，已选物品置顶，避免物品较多时难以确认选择或撑大窗口。
- 悬浮窗最上方显示“当前可参与 FATE”，按距离列出当前地图处于准备/进行中的未完成 FATE，每项提供导航按钮。
- “当前可参与 FATE”标题右侧提供“停止导航”按钮，调用 `VnavService.Stop()` 终止当前 FATE 导航、自动上坐骑等待和到达后的自动下坐骑监听。
- FATE 与狩猎导航共用水晶决策：FATE 同地图时比较“角色到目标的距离”和“目标到最近水晶的距离”；角色直达目标更近时直接导航，否则先传送到该水晶再导航目标点。狩猎助手仍按“角色到最近水晶的距离”和“目标到该水晶的距离”比较，避免跨地图后重复传送。
- FATE 找不到附近网格点时尝试直接使用 FATE 原始坐标；狩猎 Flag 使用 `MapLinkPayload` 的 `TerritoryType`、`Map`、`XCoord` 和 `YCoord` 解析地图与坐标。
- 狩猎助手可启用自动跟随、输入或粘贴车头名称、使用当前目标、设为自己、清除车头、设置目标接地距离，并控制是否在悬浮窗显示状态。车头名称匹配会忽略 `@服务器` 后缀，因此不需要填写服务器名。
- 狩猎助手支持跨地图：先按目标地图 ID 使用 Lifestream 传送到目标地图，再等待读图、Lifestream 空闲和 vnavmesh 就绪后导航。目标点先贴地，再按配置高度抬升，狩猎导航抵达后保持骑乘，不自动下坐骑。
- 车头 Flag 会通过 `FFXIVClientStructs` 的 `AgentMap.SetFlagMapMarker` 写入游戏地图，不调用 `IGameGui.OpenMapWithMapLink`，因此只设置地图标记而不自动打开地图。该接口使用世界坐标，必须传入经过 `Map.OffsetX`、`Map.OffsetY` 和 `Map.SizeFactor` 换算后的目标世界坐标。
- Lifestream 传送完成后，若 vnavmesh 仍处于地图网格缓存/加载状态，会保留待导航目标并持续重试；必须确认读图结束、Lifestream 空闲、达到传送后的稳定等待时间且 vnavmesh 可用后，才开始后续导航。不能因首次近邻网格查询失败而丢弃目标。
- 狩猎助手页面和悬浮窗均提供停止导航按钮，可取消传送等待、自动上坐骑等待和 vnavmesh 路径；测试开关可用 Echo/默语复述车头任意频道发言。
- 幻武页面的 Wiki 按钮右侧提供“监控幻武”和“自动标记击杀”开关。“监控幻武”只统一控制“秘影指定目标”与“迷宫/讨伐任务”两个显示开关（点击时一起切换，勾选状态取二者或），不再控制整个悬浮窗的显隐。
- 在蜃景幻界新月岛南征之章（`TerritoryType = 1252`）和北征之章（`TerritoryType = 1346`）点击 FATE 导航或“最近 FATE”时，不执行导航，提示“【新月岛地图】请使用【新月岛史官】插件。”；优雷卡四张地图（`732`、`763`、`795`、`827`）及博兹雅南方战线、扎杜诺尔（`920`、`975`）执行相同操作时，提示“【博兹雅/优雷卡】暂不支持该地图。”。以上八个地图 ID 均已由游戏内 DEBUG 输出确认。
- 悬浮窗可自由拉大拉小：不再使用自动适应尺寸，窗口带最小尺寸约束（240×120），内容超出窗口高度时出现滚动条；幻武监控、狩猎助手、危命助手三张卡片宽度跟随窗口宽度。
- 幻武监控卡片高度跟随窗口纵向拉伸，占满剩余高度（最低 120px，内部内容超出时卡片内滚动）；狩猎助手与危命助手卡片保持固定高度，不随窗口变化。
- 悬浮窗任意子卡片区域右键打开统一上下文菜单。“快捷入口”使用二级菜单，包含“幻武进度”“绝武总进度”“前往幻境村”“妖表联动”：三个页面入口会打开主窗口并切换到对应栏目，“前往幻境村”直接调用现有路线。
- 右键菜单一级提供“古武监控”“幻武监控”“狩猎助手”“危命助手”四个勾选项。古武、狩猎和危命分别切换对应悬浮卡片；幻武统一切换秘影目标与迷宫/讨伐两个区块。狩猎助手菜单项只控制卡片显示，不改变 `HuntAssistantEnabled` 的聊天监听和自动导航状态。
- 右键菜单继续保留“打开主窗口”“飞行导航”“自动标记击杀”“关闭悬浮窗”；所有配置切换均立即调用 `configuration.Save()`。
- 幻武监控、狩猎助手、危命助手三张卡片标题行的“停止导航”按钮均右对齐贴边，与行内“展开/收起”按钮使用相同的右侧对齐方式。

### 妖表联动页面

页面顶部显示分行攻略提示：

1. 找 NPC 开启活动后，带上妖怪手表，先去刷 FATE，拿奖励兑换宠物。
2. 带着宠物后才会掉落兑换武器的材料，材料不是必出，最后用武器材料兑换对应武器。
3. 注意：不同宠物掉落的材料不一样。该行使用红色强调。

奖励按妖怪手表、坐骑、肖像、宠物和武器分类展示。特殊物品、坐骑、肖像和宠物通过对应解锁状态判断；其中“地缚猫沙发”直接使用固定 `MountId = 228` 判断，其余坐骑可从匹配道具的 `ItemAction` 解析坐骑 ID。武器同步扫描背包、关键道具、装备栏、兵装库、鞍囊、收藏柜、投影台和雇员缓存，结果按角色 ContentId 保存。

武器卡片悬浮提示显示武器名称、持有状态和对应宠物；对武器卡片点击右键会复制该提示内容，并在聊天栏显示复制成功消息。

武器卡片还显示对应职业、所需“妖怪传奇徽章”和活动 FATE 获取地区。资料覆盖 17 把活动武器；区域按徽章对应宠物的 FATE 出现地区列出。

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
- 实际分组完成文本格式为 `已完成战斗的记忆“迷宫或讨伐任务：顶级迷宫”中的所有项目！`；必须先匹配引号内的具体分组并返回，不能仅因同时包含“战斗的记忆”“完成”“所有项目”而标记全部内容。

### 探索记忆分组完成

- 消息包含“所有项目”且匹配探索记忆分组名（`ExplorationMemoryGroups`）时：
  - 尤卡图拉尔 → 奥阔帕恰山 + 克扎玛乌卡湿地 + 亚克特尔树海。
  - 萨卡图拉尔 → 夏劳尼荒野 + 遗产之地。
  - 无失世界 → 活着的记忆。
  - 同时支持单地图：克扎玛乌卡湿地、亚克特尔树海、夏劳尼荒野、遗产之地。
- 将对应地图的所有秘影目标勾选，并把该地图的 FATE 进度 `Progress["secret-fate-{TerritoryType}"]` 设为 5。

### 全部完成

- 实际文本为 `已完成战斗的记忆中的所有项目！`，不含引号内的副本分组或场景探索名称。
- 只有未匹配任何具体副本分组和场景探索分组时，才标记全部秘影目标、全部副本任务与六张地图的金牌 FATE 进度。

## 实现注意事项

- 插件内置资料应注明来源日期，后续资料变动需要人工更新。
- 导航服务必须容忍 vnavmesh 或 Lifestream 未安装、未加载、IPC 不可用。
- Lifestream 传送后需要等待 `Lifestream.IsBusy == false` 且 vnavmesh ready，再继续路径导航。
- 幻境村路由使用固定常量 216/239/1185/1278，不使用字符串地名，避免本地化差异。
- 当前工作区可能不是 Git 仓库，不依赖 Git 操作。
- 构建命令：`dotnet build`
- 构建产物：`output/Phantom.dll`
- 发布流程见 `docs/release.md`（tag 推送触发 GitHub Actions）。
