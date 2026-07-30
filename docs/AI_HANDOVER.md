# 幻境武器助手 — 项目交接文档

> 生成日期：2026-07-30 | 版本：0.1.0 | Git: `main` (first commit 969c5a4)
> 远程仓库：https://github.com/anmili2022/Phantom

---

## 一、项目概况

幻境武器助手是一个 Dalamud（卫月）API 15 插件，用于在《最终幻想 XIV》中追踪幻境武器制作进度。参考代码库 `Chronicler`（新月岛史官）的结构和 IPC 调用方式。

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

### 目录结构

```
E:\git\Phantom\
├── Phantom.csproj          # 项目文件（Dalamud.NET.Sdk）
├── Phantom.json            # 卫月插件清单（含IconUrl）
├── repo.json               # 仓库发布清单
├── .gitignore
├── packages.lock.json
├── images/
│   └── icon.png            # 插件图标（64×64，占位，待替换）
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
├── UI/
│   └── PluginUI.cs         # 主窗口 + 悬浮窗 + DEBUF 页
├── docs/
│   ├── design.md           # 设计文档（原始）
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
- AutoMarkSecretKills       // 自动标记击杀
- FloatingSecretTerritoryType // 悬浮窗当前地图
- FloatingManualMode        // 悬浮窗手动模式
- SelectedStageIndex        // 选中 Tab
- Progress                  // Dictionary<string, int> 进度
- CompletedTasks            // HashSet<string> 一次性任务完成
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
- 匹配关键词：打倒、击倒、讨伐、消灭、defeat、defeated、slay、slain
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
header (启用/飞行/悬浮/自动标记/重置)
Separator
TabBar
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
└── DEBUG Tab
    ├── 读取当前坐标
    ├── 测试坐标换算
    └── 解析地图参数
```

### 悬浮窗（`DrawFloatingObjectiveWindow`）
```
header (地图名 + 停止导航 + < > 当)
进度条 (完成数/9)
金牌 FATE 行 (+/- 计数器 + 最近FATE)
Separator
目标列表 (未完成: 名称, 坐标, 导航)
```
- 右键菜单：打开主窗口/飞行/自动标记/关闭
- 手动模式用 `FloatingManualMode` 标志切换
- 切换到手动后，`<`/`>`循环 6 张地图

---

## 七、已收集的地图参数

| 地图 | TerritoryType | MapRowId | SizeFactor | OffsetX | OffsetY |
|------|--------------|----------|-----------|---------|---------|
| 奥阔帕恰山 | 1187 | 857 | 100 | 0 | 0 |
| 活着的记忆 | 1192 | 862 | 100 | 0 | 0 |

> 其他地图（1188、1189、1190、1191）参数未记录，可用 DEBUG 页读取。

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

- [ ] 验证其余 5 张地图的坐标（站到目标位置用 DEBUG 页确认）
- [ ] 记录 1188/1189/1190/1191 的地图参数（SizeFactor/Offset）
- [ ] 自动读取背包材料数量
- [ ] 自动读取任务状态
- [ ] 物品计数进度增加显示（如「半魂晶 3/18」）
- [ ] 替换 `images/icon.png` 为正式图标
- [ ] 多语言支持（EN/JP）
- [ ] 北征之章 CE 计时器
- [ ] 通知/提示音（FATE 出现提醒）

---

## 十一、常见问题

### Q: 导航点了没反应？
A: 确保已安装 vnavmesh 和 Lifestream。检查悬浮窗的「停止导航」按钮是否卡住。

### Q: 坐标导航到错误位置？
A: 使用 DEBUG 页「解析地图参数」+「测试坐标换算」验证当前地图的 SizeFactor 和 Offset 是否已知。需要手动更新 `PhantomWeaponGuide.cs` 中的坐标。

### Q: 自动标记没生效？
A: 检查工具栏的「自动标记击杀」是否勾选，以及聊天语言是否和关键词匹配（中文客户端用中文关键词）。

### Q: 如何给新地图添加目标？
A: 在 `PhantomWeaponGuide.cs` 的 `SecretTargets` 数组中添加 `new PhantomWeaponTarget(...)`，需提供正确的 TerritoryType 和已验证的地图坐标。

---

*本文件由 AI 辅助生成，旨在帮助后续开发者快速理解项目结构和代码逻辑。*
