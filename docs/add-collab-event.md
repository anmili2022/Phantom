# 添加新联动活动步骤

本文档记录在「当前联动」栏目中新增一个联动/季节活动页签的完整流程，以「守护天节」为模板。下次添加新活动时按此步骤执行。

> 参考实现：`Features/Collabs/GuardianFestivalCollabGuide.cs` 与 `UI/PluginUI.cs` 中的 guardian 相关内容。

---

## 0. 前置信息收集

从灰机 wiki（或官方公告）获取以下信息：

1. **活动名称**与页签名（如「守护天节」）。
2. **活动起止时间**（精确到分钟，如 `2026-10-15 16:00 ~ 2026-11-02 22:59`）。
3. **Wiki 链接**（如 `https://ff14.huijiwiki.com/wiki/守护天节_2026`）。
4. **参与方式**：接取任务 NPC（名称、地图、X/Y）、任务名、等级要求。
5. **兑换 NPC**（季节商铺等）名称、地图、X/Y。
6. **奖励清单**：物品名、分类（外观/家具/坐骑/宠物/管弦乐/幻卡/其他）、获取方式（任务奖励/兑换货币/掉落）。
7. **相关成就**名称（成就 ID 见步骤 2）。

> ⚠️ 物品/成就的 `Item.RowId` / `Achievement.RowId` 必须从客户端数据获取，**不能**按 wiki 名称或猜测填写。

---

## 1. 创建活动 Guide 类

在 `Features/Collabs/` 下新建 `XxxCollabGuide.cs`（参考 `GuardianFestivalCollabGuide.cs`）：

```csharp
namespace Phantom;

public static class XxxCollabGuide
{
    public static readonly DateTime EventStart = new(2026, 10, 15, 16, 0, 0);
    public static readonly DateTime EventEnd = new(2026, 11, 2, 22, 59, 0);
    public const string EventTimeText = "2026-10-15 16:00 ~ 2026-11-02 22:59";
    public const string WikiUrl = "https://ff14.huijiwiki.com/wiki/...";
    public const string Summary = "一句话简介。";

    // 分类常量（用于 rewards 分组与页面展示顺序）
    public const string FashionCategory = "外观";
    public const string FurnitureCategory = "家具";
    public const string MiscCategory = "其他";

    public static readonly IReadOnlyList<CollabNpc> Npcs =
    [
        new("xxx-quest-npc", "NPC名", "地图名", 10.2f, 9.4f, "用途说明。"),
        new("xxx-shop-npc", "兑换NPC名", "地图名", 10.3f, 9.0f, "季节商铺说明。"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> Rewards =
    [
        new("xxx-reward-key", "物品名", FashionCategory, [12345], Note: "获取方式说明。"),
        // ...
    ];

    public static readonly IReadOnlyList<CollabAchievementDefinition> Achievements =
    [
        new("achievement-xxx-key", "成就名", 3945, "解锁条件说明。"),
    ];

    public static bool IsEventActive(DateTime time)
        => time >= EventStart && time <= EventEnd;
}
```

要点：
- `CollabNpc(Key, Name, Zone, X, Y, Purpose)`，`Zone` 用游戏地图名（如 `格里达尼亚旧街`），导航走 `vnav.NavigateToMapCoordinate`。
- `CollabRewardDefinition(Key, Name, Category, ItemIds, MountName, Note)`。外观/家具用默认分类；坐骑需传 `MountName`；宠物/武器由分类判断。
- `CollabAchievementDefinition(Key, Name, AchievementId, Note)`。
- 分类常量决定页面分组顺序，按需增删。

---

## 2. 获取 Item.RowId 与 Achievement.RowId

### 2.1 物品 RowId

1. 把奖励物品名加入 `UI/PluginUI.cs` 的 `ExportPityItemIds()` 目标列表（`Series: "活动名"`，`SearchTerms` 给名称或关键词）。
2. 构建并安装插件，进游戏设置页 DEBUG 区点击「导出低保Item.RowId」。
3. 打开 `%APPDATA%\XIVLauncherCN\pluginConfigs\Phantom\pity-item-ids.txt`，取 `EXACT` 行的 RowId 填入 `Rewards`。
4. 若某行 `MISSING` 或只有 `CANDIDATE`，调整 `SearchTerms` 后重导，或确认该物品是否不在 `Item` 表（如货币、家具可能另表）。

### 2.2 成就 RowId

- 设置页 DEBUG 区「物品/成就 ID 查询」按名称搜索，会同时列出成就 `Achievement.RowId`。
- 也可通过 wiki 成就检索器或第三方数据库确认，但以客户端 `Achievement` 表为准。

> 首次未取得成就 ID 时可先填 `0`（卡片会显示但不判定完成），拿到后再补。

---

## 3. 配置持久化字段

在 `Configuration/PluginConfiguration.cs` 增加（按角色 ContentId 保存）：

```csharp
public bool HideOwnedXxxRewards { get; set; }
public Dictionary<string, List<string>> XxxOwnedRewardKeysByCharacter { get; set; } = new();
public Dictionary<string, string> XxxSyncTimesByCharacter { get; set; } = new();
```

> 新增字段有默认值，旧存档自动兼容。

---

## 4. 注册结果缓存字段

在 `UI/PluginUI.cs` 结果字段区（`mogMogResults` 附近）加：

```csharp
private IReadOnlyList<YokaiRewardProgress> xxxResults = Array.Empty<YokaiRewardProgress>();
```

---

## 5. 接入联动页签

### 5.1 页签列表

在 `DrawCollabWorkspace()` 的 `tabs` 列表中追加：

```csharp
("xxx", "活动名", XxxCollabGuide.IsEventActive(now)),
```

`now` 已在该方法内定义；`Active` 决定「自动隐藏已过期联动」时是否显示。

### 5.2 页签内容分发

在同方法的 `switch (tab.Key)` 中追加：

```csharp
case "xxx":
    DrawCollabEventBanner(XxxCollabGuide.EventTimeText, XxxCollabGuide.IsEventActive(now));
    DrawXxxCollabContent();
    break;
```

### 5.3 内容绘制方法

新增（参考 `DrawGuardianFestivalCollabContent`），复用 `DrawIdBasedCollabContent`：

```csharp
private void DrawXxxCollabContent()
{
    DrawIdBasedCollabContent(
        idPrefix: "xxx",
        label: "活动名",
        rewards: XxxCollabGuide.Rewards,
        npcs: XxxCollabGuide.Npcs,
        achievements: XxxCollabGuide.Achievements,
        categories:
        [
            XxxCollabGuide.FashionCategory,
            XxxCollabGuide.FurnitureCategory,
            XxxCollabGuide.MiscCategory,
        ],
        summaryCategories: [XxxCollabGuide.FashionCategory],
        wikiUrl: XxxCollabGuide.WikiUrl,
        wikiButtonLabel: "打开活动名Wiki",
        introTitle: "参与方式：",
        introLines:
        [
            "在这里写参与方式说明。",
        ],
        getResults: () => xxxResults,
        setResults: value => xxxResults = value,
        ownedKeys: configuration.XxxOwnedRewardKeysByCharacter,
        syncTimes: configuration.XxxSyncTimesByCharacter,
        getHide: () => configuration.HideOwnedXxxRewards,
        setHide: value => configuration.HideOwnedXxxRewards = value,
        syncTooltip: "扫描当前角色已加载的背包、关键道具、装备栏和完整兵装库，统计活动奖励。已同步结果按角色 ContentId 保存。");
}
```

`DrawIdBasedCollabContent` 会自动提供：简介、NPC 导航按钮、Wiki 按钮、同步按钮、上次同步时间、隐藏已获得、汇总卡片、成就区、按分类的奖励网格。

---

## 6. 清理与验证

1. **移除导出目标**：功能确认后，可将步骤 2 加入 `ExportPityItemIds()` 的活动物品名删除，保持 DEBUG 列表整洁（也可保留，视需要）。
2. `dotnet build Phantom.csproj` 确保无 error。
3. 复制 `output/Phantom.dll` 与 `output/Phantom.json` 到 `%APPDATA%\XIVLauncherCN\installedPlugins\Phantom\<版本>\`。
4. 进游戏验证：
   - 页签出现且活动时间正确；
   - NPC 导航可用（城市地图注意坐骑/以太水晶逻辑，见 `docs/design.md`）；
   - 「同步当前角色」能统计已持有奖励；
   - 成就按 ID 正确判定；
   - 「自动隐藏已过期联动」行为正确。

---

## 7. 文档维护

- 更新 `docs/design.md`（联动页签与同步说明）。
- 更新 `docs/AI_HANDOVER.md`（已实现联动列表）。
- 如涉及导航新地图（如无主城以太水晶的区域），参考 `VnavService` 中的格里达尼亚两级传送实现并在 `docs/design.md` 记录。
