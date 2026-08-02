# 肝武助手 — 项目交接文档

> 更新日期：2026-08-02 | 版本：0.1.9.0 | Git: `main`
> 远程仓库：https://github.com/anmili2022/Phantom

---

## 一、项目概况

肝武助手是一个 Dalamud（卫月）API 15 插件，用于在《最终幻想 XIV》中追踪幻武、旧肝武、曼武、生产采集特殊工具、绝武和妖表联动收藏。参考代码库 `Chronicler`（新月岛史官）的结构和 IPC 调用方式。

### 技术栈

| 项目 | 值 |
|------|-----|
| 框架 | `.NET 8` |
| Dalamud API | `15` |
| SDK | `Dalamud.NET.Sdk/15.0.0` |
| 语言 | `C# 12` (latest) |
| 导航 | `vnavmesh` IPC + `Lifestream` IPC |
| 构建命令 | `dotnet build` |
| 输出目录 | `output/Phantom.dll` |
| 图标 | `images/icon.png` → 构建时复制到 `output/icon.png` |

### 当前已实现

- 阶段页展示、材料与进度手填、一次性流程勾选。
- 秘影目标列表、坐标展示、导航按钮、悬浮追踪窗。
- 聊天事件自动标记：击杀小怪、讨伐任务组、探索记忆组、金牌 FATE。
- 总览页：汇总职业收藏、绝武收藏、妖表奖励、各系列完成度和雇员缓存覆盖；“刷新扫描”同步全部系列。
- 武器进度页：按职业/角色同步并展示已持有阶段，支持按职能分组和武器图标。
- 曼德维尔武器四阶段资料页：任务链和材料支持手动勾选/录入。
- 古武、魂武、优武、义武、天钢工具、莫雯工具、宇宙工具、绝本武器资料页：复用阶段页签、任务勾选、材料手动进度和职业持有同步。
- 绝武覆盖绝巴哈、绝神兵、绝亚、绝龙诗、绝欧米茄、绝伊甸和绝妖星；每个绝本有独立页签，并提供七阶段总进度。
- 雇员武器通过 `ItemFinderModule.RetainerInventories` 客户端缓存扫描。
- 前往幻境村的独立导航入口。

### 目录结构

```
E:\git\Phantom\
├── Phantom.csproj          # 项目文件（Dalamud.NET.Sdk）
├── Phantom.json            # 卫月插件清单（含IconUrl）
├── repo.json               # 仓库发布清单
├── .gitignore
├── packages.lock.json
├── images/
│   └── icon.png            # 插件图标
├── Plugin/
│   └── PhantomPlugin.cs    # 插件入口
├── Infrastructure/
│   └── DalamudApi.cs       # Dalamud 服务单例
├── Configuration/
│   └── PluginConfiguration.cs  # 配置持久化
├── Features/
│   ├── Navigation/
│   │   └── VnavService.cs  # 导航 IPC 服务
│   └── PhantomWeapons/
│       ├── PhantomWeaponGuide.cs  # 武器阶段静态资料
│       ├── SecretKillTracker.cs   # 聊天击杀自动标记
│       └── FateTracker.cs         # 金牌 FATE 自动检测
│   └── Manderville/
│       └── MandervilleWeaponGuide.cs # 曼德维尔武器四阶段资料
│   └── RelicWeapons/
│       └── RelicWeaponGuide.cs # 旧肝武、工具、绝本资料
├── UI/
│   └── PluginUI.cs         # 主窗口、总览、各系列进度、悬浮窗和设置页
├── docs/
│   ├── design.md           # 设计文档（原始）
│   ├── overview-ui-preview.html # 总览 HTML 原型
│   ├── usage.html          # 使用说明 HTML
│   └── AI_HANDOVER.md      # 本文件
└── output/                 # 构建产物（gitignored）
```

---

## 二、核心数据模型

### 记录类型（均在 `PhantomWeaponGuide.cs`）

```csharp
PhantomWeaponStage  // 武器阶段
├── Key             // 唯一标识 (penumbra/umbra/darkness/eclipse/secret)
├── Name            // 显示名
├── ItemLevel       // 物品等级
├── Quest           // 任务名
├── Summary         // 概览文本
├── Requirements[]  // 可输入进度的材料项
├── Tasks[]         // 一次性流程项
├── RepeatableRewards[]  // 可重复来源表
└── Notes[]         // 补充说明

PhantomWeaponTarget  // 秘影目标
├── Key             // 唯一标识
├── Zone            // 地图名
├── Name            // 目标名
├── TerritoryType   // 地图 ID
├── MapX / MapY     // 地图显示坐标
```

### 配置持久化（`PluginConfiguration.cs`）

```csharp
- Enabled                   // 全局开关
- UseFlightNavigation       // 飞行导航
- ShowFloatingObjectiveWindow  // 悬浮窗显示
- ShowAvailableFatesInFloatingWindow // 悬浮窗显示当前可参与 FATE
- AutoMarkSecretKills       // 自动标记击杀
- FloatingSecretTerritoryType // 悬浮窗当前地图
- FloatingManualMode        // 悬浮窗手动模式
- SelectedStageIndex        // 选中 Tab
- SelectedMandervilleStageIndex // 曼德维尔选中阶段
- Progress                  // Dictionary<string, int> 进度
- CompletedTasks            // HashSet<string> 一次性任务完成
- SelectedRelicStageIndexes // 各资料系列当前阶段
- WeaponProgressItemsByCharacter // 按角色保存各系列同步到的 Item RowId
- WeaponProgressSyncTimes   // 按角色保存最近同步时间
- YokaiOwnedRewardKeysByCharacter // 按角色保存妖表奖励
```

---

## 三、插件生命周期

```
PhantomPlugin()
├── DalamudApi.Initialize()     // 注入所有服务
├── PluginConfiguration load    // 或 new 默认
├── new VnavService()           // IPC 订阅
├── new SecretKillTracker()     // ChatMessage 订阅
├── new FateTracker()           // ChatMessage 订阅
├── new PluginUI()              // UI
├── /phantom 命令注册
└── UiBuilder.Draw + OpenMainUi + OpenConfigUi

Dispose()
├── 反注册所有事件/IPC
└── Save()
```

### 服务注入（`DalamudApi.cs`）

```csharp
- IFramework              // OnFrameworkUpdate（处理传送后导航）
- IChatGui                // ChatMessage（击杀/FATE 检测）
- ICondition              // 坐骑/战斗状态判断
- IObjectTable            // 获取玩家位置
- IClientState            // TerritoryType
- IDataManager            // Lumina 表格读取
- ICommandManager         // /phantom
- IPluginLog              // 日志
- IFateTable              // 最近FATE 导航
```

---

## 四、导航流程

```
NavigateTo(PhantomWeaponTarget)
├── TryResolveWorldPosition()    // 地图坐标 → 世界坐标
│   ├── TerritoryType → Map     // Lumina 读取
│   ├── worldX = 50*mapX - OffsetX - 102400/SizeFactor - 50
│   └── worldZ = 50*mapY - OffsetY - 102400/SizeFactor - 50
├── SnapToNavmesh()              // vnavmesh.NearestPoint
├── 如果跨地图 && Lifestream 可用:
│   ├── FindAetheryteForTerritory()
│   ├── Lifestream.Teleport()
│   └── 等待 Lifestream.IsBusy == false + vnavmesh ready
└── StartMove()
    ├── QueueMountBeforeMove()   // 先上坐骑
    └── pathfindAndMoveTo()      // vnavmesh 导航
```

### 关键公式

反向工程自 Dalamud 反编译代码 `ConvertWorldCoordXZToMapCoord`：

```csharp
// Forward (world → map):  mapCoord = 0.02*offset + 2048/scale + 0.02*world + 1
// Reverse (map → world):   world = 50*mapCoord - offset - 102400/scale - 50
```

---

## 五、秘影自动标记

### SecretKillTracker
- 监听 `ChatGui.ChatMessage`
- 小怪消息要求同时包含“战斗的记忆”“讨伐”“只”和当前地图目标名，避免泛击杀文本误判
- 先匹配秘影副本/探索记忆任务文本，命中后直接标记对应任务组
- 匹配当前 TerritoryType 的秘影目标名
- 命中后加入 `CompletedTasks` 并保存

### FateTracker
- 监听 `ChatGui.ChatMessage`
- 匹配关键词：最高评价、gold、gold rating
- 5 秒去重
- 累加 `Progress["secret-fate-{TerritoryType}"]`（上限 5）

---

## 六、UI 结构

### 主窗口（`Draw`）
```
左侧导航
├── Workspace / Tools 系列入口
├── 前往幻境村
└── 当前角色 + 插件状态
右侧内容
├── 总览：全系列汇总、系列收藏卡片、库存覆盖、快捷入口
├── 各武器/工具系列：阶段页签 + 职业进度
└── 幻武页仅显示当前阶段进度重置
Separator
自绘阶段页签
├── 半影 Tab → DrawStage()
├── 本影 Tab → DrawStage()
├── 黯影 Tab → DrawStage()
├── 蚀影 Tab → DrawStage()
├── 秘影 Tab → DrawStage()
│   ├── DrawTasks() (一次性流程)
│   └── DrawSecretTargets()
│       ├── 每地图 CollapsingHeader
│       ├── 进度条 + FATE +/- 计数器
│       └── 表格（勾选/名称/坐标/导航）
└── 设置页
    ├── 飞行导航
    ├── 悬浮窗
    ├── 自动标记击杀
    ├── 悬浮窗自动隐藏已完成项目
    ├── 悬浮窗显示可参与 FATE
    ├── 读取当前坐标
    ├── 测试坐标换算
    └── 解析地图参数
```

### 悬浮窗（`DrawFloatingObjectiveWindow`）
```
header (地图名 + 停止导航 + < > 当)
可参与 FATE 列表（最上方，名称/状态/进度/剩余时间/导航）
进度条 (完成数/9)
金牌 FATE 行 (+/- 计数器 + 最近FATE)
Separator
目标列表 (未完成: 名称, 坐标, 导航)
```
- 右键菜单：打开主窗口/飞行/自动标记/关闭；正式设置入口在设置页。
- 手动模式用 `FloatingManualMode` 标志切换
- 切换到手动后，`<`/`>`循环 6 张地图
- FATE 导航调用 `VnavService.NavigateToFate()`；vnavmesh 近邻网格查询失败时回退到原始 FATE 坐标。

---

## 七、已收集的地图参数

| 地图 | TerritoryType | MapRowId | SizeFactor | OffsetX | OffsetY |
|------|--------------|----------|-----------|---------|---------|
| 奥阔帕恰山 | 1187 | 857 | 100 | 0 | 0 |
| 活着的记忆 | 1192 | 862 | 100 | 0 | 0 |

> 其他地图（1188、1189、1190、1191）参数已不再列为待办，可按需通过 DEBUG 页临时读取。

---

## 八、秘影坐标验证状态

| 目标 | 坐标来源 | 验证状态 |
|------|---------|---------|
| 永恒杉树精 (1192) | 原 17.74, 22.73 → 修正为 17.35, 21.65 | ✅ 游戏内验证 |
| 其余 23 个目标 | huijiwiki / xivdaily 原始值 | ⚠️ 未逐一验证 |

---

## 九、构建与发布

```powershell
# 构建
dotnet build

# 输出
output/Phantom.dll
output/icon.png
output/Phantom.json
```

### 发布更新步骤
1. 更新 `Phantom.csproj` 中的 `AssemblyVersion`
2. 同步更新 `Phantom.json` 和 `repo.json` 的 `AssemblyVersion`
3. `git commit -m "v0.x.x.x" && git push`
4. 更新 `repo.json` 的 `DownloadLinkInstall`/`DownloadLinkUpdate` 为 GitHub Release 下载链接
5. 在 GitHub 创建 Release 并上传 `Phantom.zip`（含 `Phantom.dll` + `Phantom.json` + `icon.png`）

---

## 十、待办/改进方向

- [ ] 自动读取背包材料数量
- [ ] 自动读取任务状态
- [ ] 物品计数进度增加显示（如「半魂晶 3/18」）
- [ ] 替换 `images/icon.png` 为正式图标
- [ ] 多语言支持（EN/JP）

---

## 十一、常见问题

### Q: 导航点了没反应？
A: 确保已安装 vnavmesh 和 Lifestream。检查悬浮窗的「停止导航」按钮是否卡住。

### Q: 坐标导航到错误位置？
A: 使用 DEBUG 页「解析地图参数」+「测试坐标换算」验证当前地图的 SizeFactor 和 Offset 是否已知。需要手动更新 `PhantomWeaponGuide.cs` 中的坐标。

### Q: 自动标记没生效？
A: 检查设置页的「自动标记击杀」是否勾选，以及聊天语言是否和关键词匹配（中文客户端用中文关键词）。

### Q: FATE 导航提示找不到可走网格点？
A: Phantom 会先查询 FATE 附近的 vnavmesh 网格点；查询失败时会回退到 FATE 原始坐标继续尝试导航。仍然失败时，确认 vnavmesh 已加载且角色位于对应地图。

### Q: 如何给新地图添加目标？
A: 在 `PhantomWeaponGuide.cs` 的 `SecretTargets` 数组中添加 `new PhantomWeaponTarget(...)`，需提供正确的 TerritoryType 和已验证的地图坐标。

---

*本文件由 AI 辅助生成，旨在帮助后续开发者快速理解项目结构和代码逻辑。*
