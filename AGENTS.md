# AGENTS.md — 肝武助手（Phantom）项目管理提示

本文件给在此仓库中工作的 AI/开发者提供统一约定。改动代码前先读本文件和相关文档。

## 项目定位

- Dalamud（卫月）API 15 插件，用于《最终幻想 XIV》肝武/收藏追踪与导航辅助。
- 技术栈：.NET 8 / Dalamud.NET.Sdk 15 / C# latest / ImGui（Dalamud.Bindings.ImGui）/ Lumina.Excel / FFXIVClientStructs。
- 远程仓库：https://github.com/anmili2022/Phantom
- 详细交接：`docs/AI_HANDOVER.md`；设计说明：`docs/design.md`。

## 任务前置：按场景先读文档

- **新增/修改联动活动页签**（妖表、FF15、糖豆人、莫古莫古、守护天节等「当前联动」栏目）：**必须先完整阅读 `docs/add-collab-event.md`，严格按其步骤执行**，不要自创流程。
- 新增/修改肝武材料、阶段、进度相关：先读 `docs/design.md` 对应章节。
- 涉及导航（vnavmesh/Lifestream/地图/以太水晶）：先读 `docs/design.md` 导航章节与 `AGENTS.md` 的「已知约定与坑」。

## 目录结构

- `Plugin/PhantomPlugin.cs`：插件入口（IDalamudPlugin 生命周期）。
- `UI/PluginUI.cs`：主窗口、侧栏、阶段页签、各系列面板、悬浮窗、设置页、DEBUG 区。文件很大，改动集中在此。
- `Configuration/PluginConfiguration.cs`：持久化配置（按角色 ContentId 保存同步结果等）。
- `Infrastructure/DalamudApi.cs`：Dalamud 服务静态访问封装。
- `Features/`：按功能分包（RelicWeapons、Manderville、PhantomWeapons、Collabs、Yokai、Navigation、BIS、Hunt、Fates、Duties、DeepDungeonWeapons、Achievements）。
- `docs/`：设计、交接、预览 HTML、CSV 数据。
- `images/icon.png`：图标，构建时复制到 `output/`。
- 生成物：`output/`、`obj/`，勿手改、勿提交。

## 构建与验证

- 构建：`dotnet build Phantom.csproj`（在仓库根目录）。
- 输出：`output/Phantom.dll`、`output/Phantom.json`。
- 交付方式：构建成功后将 `Phantom.dll` 与 `Phantom.json` 复制到本地插件安装目录
  `%APPDATA%\XIVLauncherCN\installedPlugins\Phantom\<版本>\`（Windows 环境常用 `0.1.51.0` 等已安装目录；以当前实际目录为准）。
- 版本号在 `Phantom.csproj` 的 `<AssemblyVersion>`；发版见 `docs/release.md`、`scripts/release.ps1`、`.github/workflows/release.yml`。
- 改动后必须保证 `dotnet build` 无 error；有 lint/分析器告警时优先修复新增告警。

## 编码约定

- 保持现有代码风格：文件内 `namespace Phantom;`（file-scoped）、`sealed record`/`static class`、集合表达式 `[...]`、`Nullable` 与 `ImplicitUsings` 已启用。
- 不要新增无关注释；注释只解释不明显的意图（如“同图传送可能不产生 BetweenAreas”）。
- 中文界面文案统一使用简体中文；物品/成就等固定数据以客户端 `Item.RowId` / `Achievement.RowId` 为准，不按本地化名称匹配。
- 涉及角色进度一律按 ContentId 分角色保存，不改动他人存档结构时保持向后兼容（新增字段给默认值）。
- unsafe 指针访问 `FFXIVClientStructs` 时做空指针/越界检查；设置页每帧路径严禁访问可能悬空的 `ItemFinderModule.Result`（曾导致 AccessViolation 崩溃）。
- ImGui 交互：标题类交互用按 `ImGui.CalcTextSize` 计算宽度的 `Button`，避免固定宽度 `Selectable`（不同字体/DPI 下点击区域错位）。窗口/卡片尺寸以字体行高计算，兼容高 DPI。

## 数据与外部依赖

- 物品/成就/任务等表数据来自 Lumina.Excel；游戏内存结构来自 FFXIVClientStructs。
- 导航依赖 `vnavmesh` IPC 与 `Lifestream` IPC（传送、小以太 `Lifestream.AethernetTeleportById`）。Lifestream 不可用时降级为提示，不得崩溃。
- 灰机 wiki 作为来源说明与右键跳转链接；物品页默认 `物品:<名称>`，货币/特殊页可用覆盖标题。
- 雇员/鞍囊/投影台库存优先读 `ItemFinderModule` 客户端缓存（`RetainerInventories` 的 `ItemIds[i]` 配 `ItemCount[i]`）；未加载的缓存不保证完整，UI 需提示。

## 调试工具（设置页 DEBUG 区）

- 现有按钮：导出道具检索、导出低保 Item.RowId、查询材料数量、导出投影台缓存、导出以太水晶、收集古武理符 NPC、读取坐标、解析地图参数、打开 Wiki、物品/成就 ID 查询。
- 临时调试按钮在功能确认后应清理或保留为稳定的诊断入口；新增导出类按钮请写入插件配置目录并复制到剪贴板。
- 导出文件位于 `%APPDATA%\XIVLauncherCN\pluginConfigs\Phantom\`。

## 已知约定与坑

- `ItemFinderModule.Instance()` 不承载 `/道具检索` 的结果；检索结果相关的 `Result` 可能为 null，禁止每帧读取。
- 套装幻影化：棱晶盒/投影台缓存中套装被替换为 `MirageStoreSetItem.RowId`（如蟾蜍套装 45342），读取时必须“套装优先”展开，否则会被 `Item` 表同名占位物品吞掉。
- 格里达尼亚旧街（133）无主城以太水晶：需先到新街（132，主城大水晶 2），再用小以太 ID 30（米·凯特露天剧场）传旧街；同图重传送可能不产生 `BetweenAreas`，需有超时兜底且传送期间不得发起 `StartMove`。
- 义武“全职业一次性”材料（不祥/忌讳/遗物/传动轴/弹簧/战斗记录/沉重/粗暴）只需提交一次，不按每把累加；部分卡已移除，`PityItem.Once` 保留待用。
- 城市/金碟区域不上坐骑；导航 Flag 与传送由配置开关控制。

## 文档维护

- 功能/数据结构变化后同步更新 `docs/design.md` 与 `docs/AI_HANDOVER.md`。
- 本文档随项目约定演进维护。

## 变更流程

- 不主动提交 git（除非用户明确要求）。
- 提交信息参考现有风格（如 `Add Eureka and Resistance floating monitors`、`Fix relic once-only material counts`、`Release 0.1.x.0`）。
- 发版需更新 `Phantom.csproj` 版本号并跑 release 流程。
