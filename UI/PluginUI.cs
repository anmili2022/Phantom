using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System.Diagnostics;
using System.Numerics;

namespace Phantom;

public sealed class PluginUI
{
    private static readonly string WindowTitle = $"肝武助手 v{typeof(PluginUI).Assembly.GetName().Version?.ToString(4) ?? "0.0.0.0"}";
    private static readonly string IconPath = Path.Combine(Path.GetDirectoryName(typeof(PluginUI).Assembly.Location) ?? string.Empty, "icon.png");
    private static readonly (string Key, string Label, string Count)[] MainSections =
    {
        ("overview", "总览", "6"),
        ("phantom", "幻武 · 幻境武器", "5"),
        ("zodiac", "古武 · Zodiac", "-"),
        ("anima", "魂武 · Anima", "-"),
        ("eureka", "优武 · Eurekan", "-"),
        ("resistance", "义武 · Resistance", "-"),
        ("manderville", "曼武 · Manderville", "-"),
        ("yokai", "妖表联动", "37"),
        ("settings", "设置", "-"),
    };
    private static readonly (string Key, string Label, string Count)[] WorkspaceSections =
        MainSections.Where(section => section.Key != "settings").ToArray();
    private static readonly (string Key, string Label, string Count)[] ToolSections =
        MainSections.Where(section => section.Key == "settings").ToArray();
    private static readonly InventoryType[] WeaponInventoryTypes =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.ArmoryMainHand,
        InventoryType.ArmoryOffHand,
        InventoryType.EquippedItems,
        InventoryType.RetainerPage1,
        InventoryType.RetainerPage2,
        InventoryType.RetainerPage3,
        InventoryType.RetainerPage4,
        InventoryType.RetainerPage5,
        InventoryType.RetainerPage6,
        InventoryType.RetainerPage7,
    };

    private readonly PluginConfiguration configuration;
    private readonly VnavService vnav;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? weaponItemLookup;
    private IReadOnlyList<YokaiRewardProgress> yokaiResults = Array.Empty<YokaiRewardProgress>();
    private readonly YokaiProgressService yokaiProgress = new();
    private bool isMainWindowOpen;
    private int selectedMainSection;
    private bool showWeaponProgressTab;

    public PluginUI(PluginConfiguration configuration, VnavService vnav)
    {
        this.configuration = configuration;
        this.vnav = vnav;
    }

    public void OpenMainWindow()
    {
        isMainWindowOpen = true;
    }

    public void Draw()
    {
        DrawFloatingObjectiveWindow();

        if (!isMainWindowOpen)
        {
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(1050, 720), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin(WindowTitle, ref isMainWindowOpen))
        {
            ImGui.End();
            return;
        }

        DrawMainShell();

        ImGui.End();
    }

    private void DrawMainShell()
    {
        if (!ImGui.BeginTable("grind-weapon-main-shell", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("导航", ImGuiTableColumnFlags.WidthFixed, 226f);
        ImGui.TableSetupColumn("内容", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawMainSidebar();
        ImGui.TableNextColumn();
        DrawMainContent();
        ImGui.EndTable();
    }

    private void DrawMainSidebar()
    {
        DrawSidebarBrand();
        ImGui.Separator();

        DrawSidebarLabel("Workspace");
        foreach (var section in WorkspaceSections)
        {
            DrawSidebarButton(GetMainSectionIndex(section.Key), section.Label, section.Count, GetSidebarIcon(section.Key));
        }

        ImGui.Separator();
        DrawSidebarLabel("Tools");
        foreach (var section in ToolSections)
        {
            DrawSidebarButton(GetMainSectionIndex(section.Key), section.Label, section.Count, GetSidebarIcon(section.Key));
        }

        ImGui.Separator();
        ImGui.TextDisabled($"角色：{GetCurrentCharacterLabel()}");
        ImGui.TextDisabled(configuration.Enabled ? "状态：已启用" : "状态：已停用");
    }

    private static void DrawSidebarBrand()
    {
        if (File.Exists(IconPath))
        {
            var icon = DalamudApi.TextureProvider.GetFromFile(IconPath).GetWrapOrEmpty();
            ImGui.Image(icon.Handle, new Vector2(32f, 32f));
            ImGui.SameLine();
        }

        ImGui.BeginGroup();
        ImGui.TextColored(new Vector4(0.68f, 0.96f, 0.92f, 1f), "肝武助手");
        ImGui.TextDisabled("Weapon Progress Hub");
        ImGui.EndGroup();
    }

    private static void DrawSidebarLabel(string text)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.38f, 0.48f, 0.52f, 1f), text);
    }

    private int GetMainSectionIndex(string key)
    {
        for (var i = 0; i < MainSections.Length; i++)
        {
            if (MainSections[i].Key == key)
            {
                return i;
            }
        }

        return 0;
    }

    private static FontAwesomeIcon GetSidebarIcon(string key)
        => key switch
        {
            "overview" => FontAwesomeIcon.ChartPie,
            "phantom" => FontAwesomeIcon.Gem,
            "zodiac" => FontAwesomeIcon.Diamond,
            "anima" => FontAwesomeIcon.Fire,
            "eureka" => FontAwesomeIcon.Bolt,
            "resistance" => FontAwesomeIcon.ShieldAlt,
            "manderville" => FontAwesomeIcon.Music,
            "yokai" => FontAwesomeIcon.Paw,
            "settings" => FontAwesomeIcon.Cog,
            _ => FontAwesomeIcon.Circle,
        };

    private void DrawSidebarButton(int index, string label, string count, FontAwesomeIcon icon)
    {
        var active = selectedMainSection == index;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetColumnWidth() - 8f;
        var height = 28f;
        var drawList = ImGui.GetWindowDrawList();
        var bg = active ? new Vector4(0.12f, 0.23f, 0.25f, 1f) : new Vector4(0.08f, 0.10f, 0.13f, 0f);
        var bgHovered = new Vector4(0.10f, 0.15f, 0.18f, 1f);

        ImGui.InvisibleButton($"##main-section-{MainSections[index].Key}", new Vector2(width, height));
        var hovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked())
        {
            selectedMainSection = index;
        }

        if (active || hovered)
        {
            drawList.AddRectFilled(cursor, cursor + new Vector2(width, height), ImGui.GetColorU32(active ? bg : bgHovered), 4f);
        }

        if (active)
        {
            drawList.AddRectFilled(cursor, cursor + new Vector2(3f, height), ImGui.GetColorU32(new Vector4(0.40f, 0.83f, 0.79f, 1f)), 2f);
        }

        var textColor = active ? new Vector4(0.76f, 1f, 0.95f, 1f) : new Vector4(0.86f, 0.91f, 0.92f, 1f);
        var mutedColor = active ? new Vector4(0.40f, 0.83f, 0.79f, 1f) : new Vector4(0.55f, 0.64f, 0.68f, 1f);
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.SetCursorScreenPos(cursor + new Vector2(10f, 5f));
        ImGui.TextColored(mutedColor, icon.ToIconString());
        ImGui.PopFont();
        ImGui.SetCursorScreenPos(cursor + new Vector2(30f, 5f));
        ImGui.TextColored(textColor, label);
        ImGui.SetCursorScreenPos(cursor + new Vector2(width - 26f, 5f));
        ImGui.TextColored(mutedColor, count);
        ImGui.SetCursorScreenPos(cursor + new Vector2(0f, height + 2f));
    }

    private void DrawMainContent()
    {
        var section = MainSections[Math.Clamp(selectedMainSection, 0, MainSections.Length - 1)];
        DrawMainToolbar(section.Label);
        ImGui.Separator();

        switch (section.Key)
        {
            case "overview":
                DrawWeaponHubOverview();
                break;
            case "phantom":
                DrawPhantomWeaponWorkspace();
                break;
            case "yokai":
                DrawYokaiWorkspace();
                break;
            case "settings":
                DrawSettingsWorkspace();
                break;
            default:
                DrawWeaponSeriesPlaceholder(section.Label);
                break;
        }
    }

    private void DrawMainToolbar(string title)
    {
        ImGui.TextUnformatted(title);
        ImGui.SameLine();
        ImGui.TextDisabled("/ 当前角色维度保存进度");
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - 124f);
        if (ImGui.Button("刷新扫描##hub-sync"))
        {
            var characterKey = GetCurrentCharacterKey();
            if (characterKey.Length > 0)
            {
                SyncCurrentCharacterWeaponProgress(characterKey, GetPhantomWeaponItemLookup());
            }
        }
    }

    private void DrawWeaponHubOverview()
    {
        var characterKey = GetCurrentCharacterKey();
        var itemLookup = GetPhantomWeaponItemLookup();
        var syncedItems = characterKey.Length > 0 && configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? stored
            : new Dictionary<string, List<uint>>();
        var secretJobs = PhantomWeaponGuide.WeaponJobs.Count(job => GetHighestSyncedStage(job, itemLookup, syncedItems)?.Key == "secret");
        var activeJobs = PhantomWeaponGuide.WeaponJobs.Count(job => GetHighestSyncedStage(job, itemLookup, syncedItems) != null);

        if (ImGui.BeginTable("weapon-hub-summary", 4, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
        {
            DrawSummaryCard("所有系列", "幻武已接入", "其他肝武系列待接入数据");
            DrawSummaryCard("当前进行中", $"{activeJobs} 把", "按当前角色同步结果统计");
            DrawSummaryCard("秘影完成", $"{secretJobs}/{PhantomWeaponGuide.WeaponJobs.Count}", "保留原幻武进度统计");
            var syncTime = characterKey.Length > 0 && configuration.WeaponProgressSyncTimes.TryGetValue(characterKey, out var time) ? time : "未同步";
            DrawSummaryCard("最近同步", syncTime, "背包/兵装库/ItemFinder");
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "今日清单");
        ImGui.BulletText("幻武：同步当前角色，确认已持有的最高阶段。");
        ImGui.BulletText("秘影：继续完成当前地图的 4 个目标和 5 个金牌 FATE。");
        ImGui.BulletText("古武/魂武/优武/义武/曼武：数据模型待接入，界面入口已预留。");

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "幻武进度");
        DrawPhantomWeaponProgressPanel();
    }

    private static void DrawSummaryCard(string title, string value, string note)
    {
        ImGui.TableNextColumn();
        var cursor = ImGui.GetCursorScreenPos();
        var width = Math.Max(150f, ImGui.GetColumnWidth() - 8f);
        var size = new Vector2(width, 78f);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.10f, 0.13f, 0.16f, 0.92f)), 8f);
        drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.22f, 0.31f, 0.36f, 1f)), 8f);
        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 10f));
        ImGui.TextDisabled(title);
        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 30f));
        ImGui.TextUnformatted(value);
        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 52f));
        ImGui.TextDisabled(note);
        ImGui.SetCursorScreenPos(cursor + new Vector2(0f, size.Y + 8f));
    }

    private void DrawPhantomWeaponWorkspace()
    {
        DrawPhantomToolbar();
        ImGui.Separator();
        DrawStageTabs();
    }

    private void DrawPhantomToolbar()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("启用插件", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

        ImGui.SameLine();
        var useFlight = configuration.UseFlightNavigation;
        if (ImGui.Checkbox("飞行导航", ref useFlight))
        {
            configuration.UseFlightNavigation = useFlight;
            configuration.Save();
        }

        ImGui.SameLine();
        var showFloating = configuration.ShowFloatingObjectiveWindow;
        if (ImGui.Checkbox("悬浮目标", ref showFloating))
        {
            configuration.ShowFloatingObjectiveWindow = showFloating;
            configuration.Save();
        }

        ImGui.SameLine();
        var autoMarkKills = configuration.AutoMarkSecretKills;
        if (ImGui.Checkbox("自动标记击杀", ref autoMarkKills))
        {
            configuration.AutoMarkSecretKills = autoMarkKills;
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("重置当前阶段进度"))
        {
            ResetCurrentStage();
        }

        ImGui.SameLine();
        if (ImGui.Button("前往幻境村"))
        {
            vnav.GoToOccultVillage();
        }
    }

    private static void DrawWeaponSeriesPlaceholder(string name)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), name);
        ImGui.TextWrapped("该武器系列入口已预留。后续接入时复用幻武进度页的职业卡片、阶段胶囊、图标和按角色同步逻辑，只替换阶段资料、物品 RowId/名称和系列专属任务面板。 ");
        ImGui.BulletText("通用层：职业、阶段、物品持有、材料进度、任务勾选。");
        ImGui.BulletText("专属层：古武书籍/魂武水晶砂/优武禁地等级/义武战线/曼武任务货币等。 ");
    }

    private static void DrawDependencyStatus()
    {
        var vnavOk = TryGetPluginStatus("vnavmesh", new Version(0, 7, 6, 0), out var vnavText);
        var lifestreamOk = TryGetPluginStatus("Lifestream", new Version(2, 5, 4, 15), out var lifestreamText);
        var okColor = new Vector4(0.38f, 0.88f, 0.62f, 1f);
        var badColor = new Vector4(1f, 0.58f, 0.35f, 1f);

        ImGui.TextUnformatted("依赖插件");
        ImGui.SameLine();
        ImGui.TextColored(vnavOk ? okColor : badColor, vnavText);
        ImGui.SameLine();
        ImGui.TextColored(lifestreamOk ? okColor : badColor, lifestreamText);
        ImGui.Separator();
    }

    private static bool TryGetPluginStatus(string internalName, Version minVersion, out string text)
    {
        var plugin = DalamudApi.PluginInterface.InstalledPlugins
            .FirstOrDefault(plugin => string.Equals(plugin.InternalName, internalName, StringComparison.OrdinalIgnoreCase));
        if (plugin == null)
        {
            text = $"{internalName}: 未安装（需要 >= {minVersion}）";
            return false;
        }

        if (!plugin.IsLoaded)
        {
            text = $"{internalName}: 未加载（{plugin.Version}，需要 >= {minVersion}）";
            return false;
        }

        if (plugin.Version < minVersion)
        {
            text = $"{internalName}: {plugin.Version} 过低（需要 >= {minVersion}）";
            return false;
        }

        text = $"{internalName}: {plugin.Version}";
        return true;
    }

    private void DrawStageTabs()
    {
        var stages = PhantomWeaponGuide.Stages;
        if (configuration.SelectedStageIndex < 0 || configuration.SelectedStageIndex >= stages.Count)
        {
            configuration.SelectedStageIndex = 0;
        }

        ImGui.BeginGroup();
        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            var tabName = stage.Name.Replace("幻境武器·", string.Empty, StringComparison.Ordinal);
            if (DrawContentTabButton($"phantom-stage-{stage.Key}", tabName, !showWeaponProgressTab && configuration.SelectedStageIndex == i))
            {
                showWeaponProgressTab = false;
                configuration.SelectedStageIndex = i;
            }

            if (i < stages.Count - 1)
            {
                ImGui.SameLine(0f, 4f);
            }
        }

        if (stages.Count > 0)
        {
            ImGui.SameLine(0f, 4f);
        }

        if (DrawContentTabButton("phantom-weapon-progress", "幻境武器进度", showWeaponProgressTab))
        {
            showWeaponProgressTab = true;
        }

        ImGui.EndGroup();

        if (showWeaponProgressTab)
        {
            DrawPhantomWeaponProgressPanel();
        }
        else
        {
            DrawStage(stages[configuration.SelectedStageIndex]);
        }
    }

    private static bool DrawContentTabButton(string id, string label, bool active)
    {
        var textSize = ImGui.CalcTextSize(label);
        var width = textSize.X + 24f;
        var height = 28f;
        var cursor = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = active
            ? new Vector4(0.18f, 0.45f, 0.48f, 0.72f)
            : new Vector4(0.10f, 0.11f, 0.15f, 0.72f);
        var borderColor = active
            ? new Vector4(0.38f, 0.92f, 0.92f, 1f)
            : new Vector4(0.25f, 0.27f, 0.34f, 0.9f);
        var textColor = active ? new Vector4(0.78f, 0.96f, 0.94f, 1f) : new Vector4(0.72f, 0.78f, 0.81f, 1f);

        ImGui.InvisibleButton($"##{id}", new Vector2(width, height));
        var hovered = ImGui.IsItemHovered();
        var clicked = ImGui.IsItemClicked();

        var drawBg = bgColor;
        if (!active && hovered)
        {
            drawBg = new Vector4(0.14f, 0.18f, 0.24f, 0.82f);
        }

        drawList.AddRectFilled(cursor, cursor + new Vector2(width, height), ImGui.GetColorU32(drawBg), 4f);
        drawList.AddRect(cursor, cursor + new Vector2(width, height), ImGui.GetColorU32(borderColor), 4f, ImDrawFlags.None, 1f);

        if (active)
        {
            drawList.AddRectFilled(cursor, cursor + new Vector2(3f, height), ImGui.GetColorU32(new Vector4(0.40f, 0.83f, 0.79f, 1f)), 2f);
        }

        drawList.AddText(
            cursor + new Vector2((width - textSize.X) / 2f, 5f),
            ImGui.GetColorU32(textColor),
            label);
        return clicked;
    }

    private void DrawPhantomWeaponProgressPanel()
    {
        var characterKey = GetCurrentCharacterKey();
        var canSync = characterKey.Length > 0;
        var itemLookup = GetPhantomWeaponItemLookup();
        var syncedItems = canSync && configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? stored
            : new Dictionary<string, List<uint>>();
        var completedJobs = 0;

        ImGui.TextDisabled("点击同步时先扫描当前角色背包/兵装库/装备栏，再调用游戏 ItemFinder（/isearch 同源）补查雇员、鞍囊、投影台等缓存位置；多角色按角色 ID 分开保存。 ");
        if (!canSync)
        {
            ImGui.TextColored(new Vector4(1f, 0.72f, 0.28f, 1f), "未登录角色，无法同步。");
        }

        if (ImGui.Button("同步当前角色##sync-weapon-progress") && canSync)
        {
            syncedItems = SyncCurrentCharacterWeaponProgress(characterKey, itemLookup);
        }

        ImGui.SameLine();
        var groupByRole = configuration.GroupWeaponProgressByRole;
        if (ImGui.Checkbox("按职能显示##group-weapon-progress-by-role", ref groupByRole))
        {
            configuration.GroupWeaponProgressByRole = groupByRole;
            configuration.Save();
        }

        ImGui.SameLine();
        var showIcons = configuration.ShowWeaponProgressIcons;
        if (ImGui.Checkbox("显示武器图标##show-weapon-progress-icons", ref showIcons))
        {
            configuration.ShowWeaponProgressIcons = showIcons;
            configuration.Save();
        }

        ImGui.SameLine();
        var syncTime = canSync && configuration.WeaponProgressSyncTimes.TryGetValue(characterKey, out var time) ? time : "未同步";
        ImGui.TextDisabled($"上次同步：{syncTime}");
        ImGui.Spacing();

        foreach (var job in PhantomWeaponGuide.WeaponJobs)
        {
            if (GetHighestSyncedStage(job, itemLookup, syncedItems)?.Key == "secret")
            {
                completedJobs++;
            }
        }

        if (configuration.GroupWeaponProgressByRole)
        {
            DrawWeaponCollectionRow("防护职能", new[] { "pld", "war", "drk", "gnb" }, itemLookup, syncedItems);
            DrawWeaponCollectionRow("治疗职能", new[] { "whm", "sch", "ast", "sge" }, itemLookup, syncedItems);
            DrawWeaponCollectionRow("近战职能 1", new[] { "mnk", "drg", "nin" }, itemLookup, syncedItems);
            DrawWeaponCollectionRow("近战职能 2", new[] { "sam", "rpr", "vpr" }, itemLookup, syncedItems);
            DrawWeaponCollectionRow("远程物理", new[] { "brd", "mch", "dnc" }, itemLookup, syncedItems);
            DrawWeaponCollectionRow("远程魔法", new[] { "blm", "smn", "rdm", "pct" }, itemLookup, syncedItems);
        }
        else
        {
            DrawWeaponCollectionGrid(itemLookup, syncedItems);
        }

        ImGui.TextDisabled($"秘影完成职业 {completedJobs}/{PhantomWeaponGuide.WeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");
    }

    private void DrawWeaponCollectionGrid(
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        ImGui.Spacing();
        var tileWidth = configuration.ShowWeaponProgressIcons ? 120f : 94f;
        const int columns = 5;
        if (!ImGui.BeginTable("phantom-weapon-collection-grid", columns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var job in PhantomWeaponGuide.WeaponJobs)
        {
            ImGui.TableNextColumn();
            var highestStage = GetHighestSyncedStage(job, itemLookup, syncedItems);
            DrawWeaponCollectionTile(job, highestStage, itemLookup, syncedItems, tileWidth);
        }

        ImGui.EndTable();
    }

    private void DrawWeaponCollectionRow(
        string label,
        IReadOnlyList<string> jobKeys,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), label);
        var tileWidth = configuration.ShowWeaponProgressIcons ? 120f : 94f;
        if (!ImGui.BeginTable($"phantom-weapon-row-{label}", jobKeys.Count, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var key in jobKeys)
        {
            var job = PhantomWeaponGuide.WeaponJobs.FirstOrDefault(job => job.Key == key);
            if (job == null)
            {
                continue;
            }

            ImGui.TableNextColumn();
            var highestStage = GetHighestSyncedStage(job, itemLookup, syncedItems);
            DrawWeaponCollectionTile(job, highestStage, itemLookup, syncedItems, tileWidth);
        }

        ImGui.EndTable();
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetPhantomWeaponItemLookup()
    {
        weaponItemLookup ??= BuildPhantomWeaponItemLookup(DalamudApi.DataManager.GetExcelSheet<Item>());
        return weaponItemLookup;
    }

    private unsafe Dictionary<string, List<uint>> SyncCurrentCharacterWeaponProgress(
        string characterKey,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup)
    {
        var ownedItemIds = GetOwnedWeaponItemIds();
        var synced = new Dictionary<string, List<uint>>();
        foreach (var job in PhantomWeaponGuide.WeaponJobs)
        {
            foreach (var stage in PhantomWeaponGuide.ProgressStages)
            {
                if (!itemLookup.TryGetValue((job.Key, stage.Key), out var items))
                {
                    continue;
                }

                var ownedStageItems = items
                    .Where(item => ownedItemIds.Contains(item.RowId) || ItemFinderHasItem(item.RowId))
                    .Select(item => item.RowId)
                    .ToList();
                if (ownedStageItems.Count > 0)
                {
                    synced[GetWeaponProgressKey(job, stage)] = ownedStageItems;
                }
            }
        }

        configuration.WeaponProgressItemsByCharacter[characterKey] = synced;
        configuration.WeaponProgressSyncTimes[characterKey] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        configuration.Save();
        return synced;
    }

    private static unsafe bool ItemFinderHasItem(uint itemId)
    {
        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return false;
        }

        finder->SearchForItem(itemId, false);
        var result = finder->Result;
        if (result == null)
        {
            return false;
        }

        if (result->EquipmentSlot >= 0
            || result->ArmouryChestCount > 0
            || result->InventoryPage1Count > 0
            || result->InventoryPage2Count > 0
            || result->InventoryPage3Count > 0
            || result->InventoryPage4Count > 0
            || result->ArmoireCount > 0
            || result->SaddleBagPage1Count > 0
            || result->SaddleBagPage2Count > 0
            || result->PremiumSaddleBagPage1Count > 0
            || result->PremiumSaddleBagPage2Count > 0
            || result->GlamourDresserCount > 0)
        {
            return true;
        }

        for (var i = 0; i < result->RetainerCount; i++)
        {
            var retainer = result->Retainer[i];
            if (retainer == null)
            {
                continue;
            }

            if (retainer->EquipmentSlot >= 0
                || retainer->Page1Count > 0
                || retainer->Page2Count > 0
                || retainer->Page3Count > 0
                || retainer->Page4Count > 0
                || retainer->Page5Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetCurrentCharacterLabel()
    {
        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null)
        {
            return "未登录";
        }

        var world = player.HomeWorld.Value.Name.ExtractText();
        return string.IsNullOrWhiteSpace(world) ? player.Name.TextValue : $"{player.Name.TextValue}@{world}";
    }

    private static string GetCurrentCharacterKey()
    {
        var contentId = DalamudApi.PlayerState.ContentId;
        if (contentId != 0)
        {
            return contentId.ToString();
        }

        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null)
        {
            return string.Empty;
        }

        return GetCurrentCharacterLabel();
    }

    private unsafe HashSet<uint> GetOwnedWeaponItemIds()
    {
        var result = new HashSet<uint>();
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return result;
        }

        foreach (var inventoryType in WeaponInventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null)
            {
                continue;
            }

            for (var i = 0; i < container->Size; i++)
            {
                var itemId = NormalizeItemId(container->GetInventorySlot(i)->ItemId);
                if (itemId > 0)
                {
                    result.Add(itemId);
                }
            }
        }

        return result;
    }

    private static uint NormalizeItemId(uint itemId)
        => itemId >= 1_000_000 ? itemId % 1_000_000 : itemId;

    private static Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> BuildPhantomWeaponItemLookup(Lumina.Excel.ExcelSheet<Item> itemSheet)
    {
        var itemsByName = itemSheet
            .Where(item => item.RowId > 0)
            .GroupBy(item => item.Name.ExtractText(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var lookup = new Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>();
        var stages = PhantomWeaponGuide.ProgressStages;
        var jobs = PhantomWeaponGuide.WeaponJobs;

        for (var stageIndex = 0; stageIndex < stages.Count; stageIndex++)
        {
            var stage = stages[stageIndex];
            foreach (var job in jobs)
            {
                var matchedItems = job.StageItemNames[stageIndex]
                    .Where(itemsByName.ContainsKey)
                    .Select(name => itemsByName[name])
                    .ToArray();

                if (matchedItems.Length > 0)
                {
                    lookup[(job.Key, stage.Key)] = matchedItems;
                }
            }
        }

        return lookup;
    }

    private static PhantomWeaponProgressStage? GetHighestSyncedStage(
        PhantomWeaponJob job,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        return PhantomWeaponGuide.ProgressStages
            .Where(stage => itemLookup.TryGetValue((job.Key, stage.Key), out var items)
                && syncedItems.TryGetValue(GetWeaponProgressKey(job, stage), out var itemIds)
                && items.Any(item => itemIds.Contains(item.RowId)))
            .LastOrDefault();
    }

    private void DrawWeaponProgressCell(
        PhantomWeaponJob job,
        PhantomWeaponProgressStage stage,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems,
        bool isHighest)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var size = new Vector2(58, 58);
        var items = itemLookup.TryGetValue((job.Key, stage.Key), out var matchedItems) ? matchedItems : Array.Empty<Item>();
        var itemAvailable = items.Count > 0;
        var owned = itemAvailable
            && syncedItems.TryGetValue(GetWeaponProgressKey(job, stage), out var itemIds)
            && items.Any(item => itemIds.Contains(item.RowId));
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = owned
            ? (isHighest ? ImGui.GetColorU32(new Vector4(0.18f, 0.45f, 0.48f, 0.72f)) : ImGui.GetColorU32(new Vector4(0.18f, 0.24f, 0.30f, 0.58f)))
            : ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.10f, 0.50f));
        var borderColor = owned
            ? (isHighest ? ImGui.GetColorU32(new Vector4(0.38f, 0.92f, 0.92f, 1f)) : ImGui.GetColorU32(new Vector4(0.36f, 0.44f, 0.50f, 1f)))
            : ImGui.GetColorU32(new Vector4(0.22f, 0.22f, 0.26f, 1f));

        drawList.AddRectFilled(cursor, cursor + size, bgColor, 5f);
        drawList.AddRect(cursor, cursor + size, borderColor, 5f, ImDrawFlags.None, isHighest ? 2.4f : 1f);
        ImGui.SetCursorScreenPos(cursor + new Vector2(5, 5));

        if (itemAvailable)
        {
            var item = items[0];
            var texture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty();
            ImGui.Image(texture.Handle, new Vector2(40, 40), Vector2.Zero, Vector2.One, owned ? Vector4.One : new Vector4(1f, 1f, 1f, 0.22f));
        }
        else
        {
            ImGui.Dummy(new Vector2(40, 40));
        }

        ImGui.SetCursorScreenPos(cursor + new Vector2(7, 42));
        ImGui.TextColored(owned ? new Vector4(0.78f, 0.96f, 0.94f, 1f) : new Vector4(0.45f, 0.45f, 0.50f, 1f), stage.Name);
        ImGui.SetCursorScreenPos(cursor);
        ImGui.InvisibleButton($"weapon-progress-{job.Key}-{stage.Key}", size);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(itemAvailable
                ? $"{job.Name} / {stage.Name}\n{string.Join("\n", items.Select(item => item.Name.ExtractText()))}\n{(owned ? "已持有" : "未持有")}" 
                : $"{job.Name} / {stage.Name}\n未能在物品表匹配到武器");
        }
    }

    private void DrawWeaponCollectionTile(
        PhantomWeaponJob job,
        PhantomWeaponProgressStage? highestStage,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems,
        float width)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var showIcon = configuration.ShowWeaponProgressIcons;
        var size = new Vector2(width, showIcon ? 104f : 64f);
        var stageKey = highestStage?.Key ?? string.Empty;
        var ownedItems = highestStage == null
            ? Array.Empty<Item>()
            : GetSyncedStageItems(job, highestStage, itemLookup, syncedItems).ToArray();
        var hasWeapon = ownedItems.Length > 0;
        var drawList = ImGui.GetWindowDrawList();
        var bgTop = hasWeapon ? new Vector4(0.10f, 0.18f, 0.24f, 0.96f) : new Vector4(0.10f, 0.11f, 0.15f, 0.72f);
        var bgBottom = hasWeapon ? new Vector4(0.07f, 0.09f, 0.14f, 0.96f) : new Vector4(0.07f, 0.08f, 0.11f, 0.72f);
        var border = hasWeapon ? GetStageColor(stageKey, 0.88f) : new Vector4(0.25f, 0.27f, 0.34f, 0.9f);

        drawList.AddRectFilledMultiColor(cursor, cursor + size,
            ImGui.GetColorU32(bgTop), ImGui.GetColorU32(bgTop), ImGui.GetColorU32(bgBottom), ImGui.GetColorU32(bgBottom));
        drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(border), 12f, ImDrawFlags.None, hasWeapon ? 1.8f : 1f);
        if (hasWeapon)
        {
            drawList.AddCircleFilled(cursor + new Vector2(width - 22f, showIcon ? 23f : 18f), showIcon ? 36f : 28f, ImGui.GetColorU32(GetStageColor(stageKey, 0.13f)), 32);
        }

        if (showIcon && hasWeapon)
        {
            ImGui.SetCursorScreenPos(cursor + new Vector2(10f, 10f));
            var texture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(ownedItems[0].Icon)).GetWrapOrEmpty();
            ImGui.Image(texture.Handle, new Vector2(40f, 40f));
            if (job.Key == "pld" && ownedItems.Length > 1)
            {
                ImGui.SameLine(0f, -12f);
                var shieldTexture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(ownedItems[1].Icon)).GetWrapOrEmpty();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 16f);
                ImGui.Image(shieldTexture.Handle, new Vector2(26f, 26f));
            }
        }

        ImGui.SetCursorScreenPos(cursor + new Vector2(10f, showIcon ? 57f : 31f));
        ImGui.TextUnformatted(job.Name);

        DrawWeaponTileProgress(cursor + new Vector2(10f, showIcon ? 88f : 52f), width - 20f, stageKey);

        ImGui.SetCursorScreenPos(cursor + new Vector2(width - 50f, showIcon ? 8f : 7f));
        DrawStagePill(highestStage?.Name ?? "未持有", stageKey);

        ImGui.SetCursorScreenPos(cursor);
        if (ImGui.InvisibleButton($"weapon-tile-{job.Key}", size))
        {
            ImGui.OpenPopup($"weapon-detail-{job.Key}");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(hasWeapon
                ? $"{job.Name} / {highestStage!.Name}\n{string.Join("\n", ownedItems.Select(item => item.Name.ExtractText()))}\n点击查看全部阶段"
                : $"{job.Name}\n上次同步未找到幻境武器\n点击查看全部阶段");
        }

        DrawWeaponTilePopup(job, itemLookup, syncedItems);
    }

    private static void DrawWeaponTileProgress(Vector2 pos, float width, string stageKey)
    {
        var stageIndex = PhantomWeaponGuide.ProgressStages
            .Select((stage, index) => (stage.Key, index))
            .FirstOrDefault(pair => pair.Key == stageKey).index;
        var filled = string.IsNullOrEmpty(stageKey) ? 0 : stageIndex + 1;
        var drawList = ImGui.GetWindowDrawList();
        var gap = 3f;
        var segmentWidth = (width - gap * 4f) / 5f;
        for (var i = 0; i < 5; i++)
        {
            var start = pos + new Vector2(i * (segmentWidth + gap), 0f);
            var end = start + new Vector2(segmentWidth, 6f);
            var color = i < filled
                ? GetStageColor(PhantomWeaponGuide.ProgressStages[i].Key, 0.92f)
                : new Vector4(0.24f, 0.25f, 0.30f, 0.72f);
            drawList.AddRectFilled(start, end, ImGui.GetColorU32(color), 2f);
        }
    }

    private void DrawWeaponTilePopup(
        PhantomWeaponJob job,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        if (!ImGui.BeginPopup($"weapon-detail-{job.Key}"))
        {
            return;
        }

        ImGui.TextUnformatted(job.Name);
        ImGui.Separator();
        for (var i = 0; i < PhantomWeaponGuide.ProgressStages.Count; i++)
        {
            var stage = PhantomWeaponGuide.ProgressStages[i];
            var ownedItems = GetSyncedStageItems(job, stage, itemLookup, syncedItems).ToArray();
            var stageNames = job.StageItemNames[i];
            var hasStage = ownedItems.Length > 0;
            ImGui.TextColored(hasStage ? GetStageColor(stage.Key, 1f) : new Vector4(0.48f, 0.50f, 0.57f, 1f), stage.Name);
            ImGui.SameLine(64f);
            if (hasStage)
            {
                ImGui.TextUnformatted(string.Join(" + ", ownedItems.Select(item => item.Name.ExtractText())));
            }
            else
            {
                ImGui.TextColored(new Vector4(0.52f, 0.54f, 0.60f, 1f), string.Join(" + ", stageNames));
            }
        }

        ImGui.EndPopup();
    }

    private static IEnumerable<Item> GetSyncedStageItems(
        PhantomWeaponJob job,
        PhantomWeaponProgressStage stage,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        if (!itemLookup.TryGetValue((job.Key, stage.Key), out var items)
            || !syncedItems.TryGetValue(GetWeaponProgressKey(job, stage), out var itemIds))
        {
            return Array.Empty<Item>();
        }

        return items.Where(item => itemIds.Contains(item.RowId));
    }

    private static Vector4 GetStageColor(string stageKey, float alpha)
    {
        var color = stageKey switch
        {
            "secret" => new Vector4(0.30f, 0.84f, 0.78f, alpha),
            "eclipse" => new Vector4(0.67f, 0.56f, 0.94f, alpha),
            "darkness" => new Vector4(0.54f, 0.48f, 0.82f, alpha),
            "umbra" => new Vector4(0.45f, 0.58f, 0.88f, alpha),
            "penumbra" => new Vector4(0.72f, 0.72f, 0.78f, alpha),
            _ => new Vector4(0.48f, 0.48f, 0.52f, alpha),
        };
        return color;
    }

    private static string GetWeaponProgressKey(PhantomWeaponJob job, PhantomWeaponProgressStage stage)
        => $"{job.Key}:{stage.Key}";

    private static void DrawStagePill(string text, string stageKey)
    {
        var color = stageKey switch
        {
            "secret" => new Vector4(0.30f, 0.84f, 0.78f, 1f),
            "eclipse" => new Vector4(0.76f, 0.70f, 0.95f, 1f),
            "darkness" => new Vector4(0.70f, 0.58f, 0.90f, 1f),
            "umbra" => new Vector4(0.56f, 0.66f, 0.90f, 1f),
            "penumbra" => new Vector4(0.72f, 0.72f, 0.78f, 1f),
            _ => new Vector4(0.48f, 0.48f, 0.52f, 1f),
        };

        ImGui.TextColored(color, text);
    }

    private void DrawYokaiWorkspace()
    {
        ImGui.TextDisabled("扫描当前角色已加载的背包、关键道具、装备栏和完整兵装库，统计妖怪手表联动奖励。已同步结果按角色 ContentId 保存。");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "一句话攻略：");
        ImGui.TextWrapped("找 NPC 开启活动后，带上妖怪手表，先去刷 FATE，拿奖励兑换宠物；");
        ImGui.TextWrapped("带着宠物后才会掉落兑换武器的材料（不是必出），最后用武器材料兑换对应武器。");
        ImGui.TextColored(new Vector4(1f, 0.35f, 0.30f, 1f), "注意：不同宠物掉落的材料不一样。");

        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            ImGui.TextColored(new Vector4(1f, 0.72f, 0.28f, 1f), "未登录角色，无法同步。");
            return;
        }

        if (ImGui.Button("同步当前角色##sync-yokai-progress"))
        {
            yokaiResults = yokaiProgress.ScanCurrentCharacter();
            configuration.YokaiOwnedRewardKeysByCharacter[characterKey] = yokaiResults
                .Where(reward => reward.Owned)
                .Select(reward => reward.Key)
                .ToList();
            configuration.YokaiSyncTimesByCharacter[characterKey] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("打开妖表 Wiki##open-yokai-wiki"))
        {
            OpenUrl("https://ff14.huijiwiki.com/wiki/%E5%A6%96%E6%80%AA%E6%89%8B%E8%A1%A8");
        }

        var glamourStatus = yokaiProgress.GetGlamourDresserStatus();
        ImGui.TextDisabled($"投影台缓存：{(glamourStatus.IsCached ? "已加载" : "未加载")}，物品数：{glamourStatus.CachedItemCount}");
        if (glamourStatus.SearchGlamourDresserCount > 0)
        {
            ImGui.TextDisabled($"最近检索投影台命中：{glamourStatus.SearchGlamourDresserCount}，名称：{glamourStatus.SearchItemName}");
        }

        if (yokaiResults.Count == 0 && configuration.YokaiOwnedRewardKeysByCharacter.TryGetValue(characterKey, out var storedKeys))
        {
            yokaiResults = YokaiWatchGuide.Rewards
                .Select(reward => new YokaiRewardProgress(reward.Key, reward.Name, reward.Category, Array.Empty<uint>(), storedKeys.Contains(reward.Key)))
                .ToArray();
        }

        ImGui.SameLine();
        var syncTime = configuration.YokaiSyncTimesByCharacter.TryGetValue(characterKey, out var time) ? time : "未同步";
        ImGui.TextDisabled($"上次同步：{syncTime}");

        var hideOwned = configuration.HideOwnedYokaiRewards;
        if (ImGui.Checkbox("隐藏已获得##hide-owned-yokai", ref hideOwned))
        {
            configuration.HideOwnedYokaiRewards = hideOwned;
            configuration.Save();
        }

        ImGui.Spacing();
        var totalOwned = yokaiResults.Count(reward => reward.Owned);
        if (ImGui.BeginTable("yokai-summary", 4, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
        {
            DrawSummaryCard("奖励总数", $"{totalOwned}/{yokaiResults.Count}", "妖表联动全部奖励");
            DrawYokaiSummaryCard(YokaiWatchGuide.WatchCategory);
            DrawYokaiSummaryCard(YokaiWatchGuide.MinionCategory);
            DrawYokaiSummaryCard(YokaiWatchGuide.WeaponCategory);
            ImGui.EndTable();
        }

        ImGui.Spacing();
        DrawYokaiCategoryGrid(YokaiWatchGuide.WatchCategory);
        DrawYokaiCategoryGrid(YokaiWatchGuide.MountCategory);
        DrawYokaiCategoryGrid(YokaiWatchGuide.PortraitCategory);
        DrawYokaiCategoryGrid(YokaiWatchGuide.MinionCategory);
        DrawYokaiCategoryGrid(YokaiWatchGuide.WeaponCategory);
    }

    private void DrawYokaiSummaryCard(string category)
    {
        var rewards = yokaiResults.Where(reward => reward.Category == category).ToArray();
        DrawSummaryCard(category, $"{rewards.Count(reward => reward.Owned)}/{rewards.Length}", "奖励进度");
    }

    private void DrawYokaiCategoryGrid(string category)
    {
        var rewards = yokaiResults.Where(reward => reward.Category == category && (!configuration.HideOwnedYokaiRewards || !reward.Owned)).ToArray();
        var total = yokaiResults.Count(reward => reward.Category == category);
        var completed = yokaiResults.Count(reward => reward.Category == category && reward.Owned);
        if (configuration.HideOwnedYokaiRewards && rewards.Length == 0)
        {
            return;
        }

        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), $"{category}  {completed}/{total}");
        if (ImGui.BeginTable($"yokai-reward-grid-{category}", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            foreach (var reward in rewards)
            {
                ImGui.TableNextColumn();
                DrawYokaiRewardTile(reward);
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
    }

    private static void DrawYokaiRewardTile(YokaiRewardProgress reward)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var size = new Vector2(150f, 88f);
        var bg = reward.Owned ? new Vector4(0.10f, 0.22f, 0.24f, 0.96f) : new Vector4(0.10f, 0.11f, 0.15f, 0.72f);
        var border = reward.Owned ? new Vector4(0.30f, 0.84f, 0.78f, 0.92f) : new Vector4(0.25f, 0.27f, 0.34f, 0.9f);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(cursor, cursor + size, ImGui.GetColorU32(bg), 8f);
        drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(border), 8f, ImDrawFlags.None, reward.Owned ? 1.8f : 1f);
        ImGui.SetCursorScreenPos(cursor + new Vector2(9f, 9f));
        ImGui.TextColored(reward.Owned ? new Vector4(0.78f, 0.96f, 0.94f, 1f) : new Vector4(0.78f, 0.80f, 0.84f, 1f), reward.Name);
        ImGui.SetCursorScreenPos(cursor + new Vector2(9f, 38f));
        ImGui.TextDisabled(reward.Category);
        ImGui.SetCursorScreenPos(cursor + new Vector2(9f, 55f));
        ImGui.TextDisabled(reward.Owned ? "已获得" : "未获得");
        ImGui.SetCursorScreenPos(cursor);
        ImGui.InvisibleButton($"yokai-reward-{reward.Key}", size);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(reward.Owned ? $"{reward.Name}\n已获得" : $"{reward.Name}\n未获得");
        }
    }

    private void DrawSettingsWorkspace()
    {
        DrawDependencyStatus();

        var autoHideCompletedFloatingItems = configuration.AutoHideCompletedFloatingItems;
        if (ImGui.Checkbox("悬浮窗自动隐藏已完成项目", ref autoHideCompletedFloatingItems))
        {
            configuration.AutoHideCompletedFloatingItems = autoHideCompletedFloatingItems;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "DEBUG");

        if (ImGui.Button("读取当前坐标##debug-print-coords"))
        {
            var player = DalamudApi.ObjectTable[0];
            var terr = DalamudApi.ClientState.TerritoryType;
            if (player != null)
            {
                var pos = player.Position;
                PrintChat($"DEBUG: TerritoryType={terr}, Position=({pos.X:0.##}, {pos.Y:0.##}, {pos.Z:0.##})");
            }
            else
            {
                PrintChat($"DEBUG: TerritoryType={terr}, (no local player)");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("测试坐标换算##debug-test-convert"))
        {
            var terr = DalamudApi.ClientState.TerritoryType;
            var player = DalamudApi.ObjectTable[0];
            var territories = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>();
            if (player != null && territories.TryGetRow(terr, out var territory))
            {
                var pos = player.Position;
                try
                {
                    var map = territory.Map.Value;
                    var s = map.SizeFactor;
                    var ox = map.OffsetX;
                    var oy = map.OffsetY;
                    var fwdX = 0.02f * ox + 2048f / s + 0.02f * pos.X + 1f;
                    var fwdZ = 0.02f * oy + 2048f / s + 0.02f * pos.Z + 1f;
                    PrintChat($"DEBUG: 当前位置→地图显示 ≈ ({fwdX:0.##}, {fwdZ:0.##})");
                    PrintChat($"DEBUG: 若地图坐标(20.7, 14.3)→世界 ≈ ({50f*20.7f - ox - 102400f/s - 50f:0.##}, {50f*14.3f - oy - 102400f/s - 50f:0.##})");
                }
                catch { }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("解析地图参数##debug-map-info"))
        {
            var terr = DalamudApi.ClientState.TerritoryType;
            var territories = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>();
            if (territories.TryGetRow(terr, out var territory))
            {
                try
                {
                    var map = territory.Map.Value;
                    PrintChat($"DEBUG: TerritoryType={terr}, MapRowId={map.RowId}, SizeFactor={map.SizeFactor}, OffsetX={map.OffsetX}, OffsetY={map.OffsetY}");
                }
                catch (Exception ex)
                {
                    PrintChat($"DEBUG: TerritoryType={terr}, Failed to resolve map: {ex.Message}");
                }
            }
            else
            {
                PrintChat($"DEBUG: TerritoryType={terr}, Territory not found in sheet.");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("打开Wiki##debug-open-wiki"))
        {
            OpenUrl("https://ff14.huijiwiki.com/wiki/%E5%B9%BB%E5%A2%83%E6%AD%A6%E5%99%A8");
        }

        ImGui.SameLine();
        if (ImGui.Button("读取战斗记忆（未完成）##debug-read-memory-ui"))
        {
            PrintChat("未完成功能：后续可通过读取战斗记忆界面或任务状态同步进度。");
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            PrintChat("已打开幻境武器 Wiki。");
        }
        catch (Exception ex)
        {
            PrintChat($"打开Wiki失败: {ex.Message}");
        }
    }

    private static void PrintChat(string message)
    {
        try
        {
            DalamudApi.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry
            {
                Type = Dalamud.Game.Text.XivChatType.Echo,
                Message = new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                    .AddUiForeground("[Phantom] ", 37)
                    .AddUiForeground(message, 24)
                    .Build(),
            });
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to print to chat.");
        }
    }

    private void DrawStage(PhantomWeaponStage stage)
    {
        ImGui.TextUnformatted($"{stage.ItemLevel}  {stage.Quest}");
        ImGui.TextWrapped(stage.Summary);

        ImGui.Spacing();
        DrawTasks(stage);

        ImGui.Spacing();
        if (stage.Key != "secret")
        {
            DrawRequirements(stage);
        }

        if (stage.RepeatableRewards.Count > 0)
        {
            ImGui.Spacing();
            DrawRewards(stage);
        }

        if (stage.Key == "secret")
        {
            ImGui.Spacing();
            DrawSecretTargets();
            ImGui.Spacing();
            DrawSecretDuties(stage);
        }

        if (stage.Notes.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("补充说明");
            foreach (var note in stage.Notes)
            {
                ImGui.BulletText(note);
            }
        }
    }

    private void DrawTasks(PhantomWeaponStage stage)
    {
        if (stage.Tasks.Count == 0)
        {
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.18f, 1f));
        ImGui.TextUnformatted("*** 仅需完成一次的流程 ***");
        ImGui.PopStyleColor();
        ImGui.SameLine();
        ImGui.TextDisabled("完成后通常对后续同阶段武器通用");

        foreach (var task in stage.Tasks)
        {
            var done = configuration.CompletedTasks.Contains(task.Key);
            if (ImGui.Checkbox($"[仅一次] {task.Name}##{task.Key}", ref done))
            {
                if (done)
                {
                    configuration.CompletedTasks.Add(task.Key);
                }
                else
                {
                    configuration.CompletedTasks.Remove(task.Key);
                }

                configuration.Save();
            }

            ImGui.TextWrapped(task.Detail);
        }
    }

    private void DrawRequirements(PhantomWeaponStage stage)
    {
        ImGui.TextUnformatted("材料与进度");
        if (ImGui.BeginTable($"requirements-{stage.Key}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("项目", ImGuiTableColumnFlags.WidthStretch, 1.6f);
            ImGui.TableSetupColumn("进度", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("来源", ImGuiTableColumnFlags.WidthStretch, 2.2f);
            ImGui.TableHeadersRow();

            foreach (var requirement in stage.Requirements)
            {
                DrawRequirementRow(requirement);
            }

            ImGui.EndTable();
        }
    }

    private void DrawSecretDuties(PhantomWeaponStage stage)
    {
        _ = stage;
        if (PhantomWeaponGuide.SecretDutyGroups.Count == 0)
        {
            return;
        }

        ImGui.TextUnformatted("秘影迷宫/讨伐任务");
        ImGui.SameLine();
        var showDuties = configuration.ShowSecretDutiesInFloatingWindow;
        if (DrawFloatingVisibilityCheckbox("##secret-duty-floating-toggle", ref showDuties))
        {
            configuration.ShowSecretDutiesInFloatingWindow = showDuties;
            configuration.Save();
        }
        foreach (var group in PhantomWeaponGuide.SecretDutyGroups)
        {
            var completed = group.Duties.Count(duty => configuration.CompletedTasks.Contains(duty.Key));
            var total = group.Duties.Count;
            var header = completed == total
                ? $"{group.Name} 已全部完成（{completed}/{total}）"
                : $"{group.Name} ({completed}/{total})";
            if (!ImGui.CollapsingHeader($"{header}##{group.Key}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                continue;
            }

            ImGui.ProgressBar(total == 0 ? 1f : (float)completed / total, new Vector2(-1, 0), $"{completed}/{total}");
            if (ImGui.BeginTable($"secret-duty-table-{group.Key}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56);
                ImGui.TableSetupColumn("指定迷宫/讨伐", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableHeadersRow();

                foreach (var duty in group.Duties)
                {
                    DrawSecretDutyRow(duty);
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawSecretDutyRow(PhantomWeaponDuty duty)
    {
        var done = configuration.CompletedTasks.Contains(duty.Key);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (ImGui.Checkbox($"##secret-duty-{duty.Key}", ref done))
        {
            if (done)
            {
                configuration.CompletedTasks.Add(duty.Key);
            }
            else
            {
                configuration.CompletedTasks.Remove(duty.Key);
            }

            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.TextWrapped(duty.Name);
    }

    private void DrawRequirementRow(PhantomWeaponRequirement requirement)
    {
        var current = configuration.Progress.GetValueOrDefault(requirement.Key);
        current = Math.Clamp(current, 0, requirement.Needed);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextWrapped(requirement.Name);

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputInt($"##progress-{requirement.Key}", ref current, 1, Math.Max(10, requirement.Needed / 10)))
        {
            configuration.Progress[requirement.Key] = Math.Clamp(current, 0, requirement.Needed);
            configuration.Save();
        }

        var fraction = requirement.Needed == 0 ? 1f : Math.Clamp((float)current / requirement.Needed, 0f, 1f);
        ImGui.ProgressBar(fraction, new Vector2(-1, 0), $"{current}/{requirement.Needed}");

        ImGui.TableNextColumn();
        ImGui.TextWrapped($"剩余 {Math.Max(0, requirement.Needed - current)}。{requirement.Source}");
    }

    private void DrawRewards(PhantomWeaponStage stage)
    {
        ImGui.TextUnformatted("可重复来源");
        if (ImGui.BeginTable($"rewards-{stage.Key}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("任务", ImGuiTableColumnFlags.WidthStretch, 2.4f);
            ImGui.TableSetupColumn("奖励数量", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableHeadersRow();

            foreach (var reward in stage.RepeatableRewards)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextWrapped(reward.Activity);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(reward.Reward);
            }

            ImGui.EndTable();
        }
    }

    private void DrawSecretTargets()
    {
        ImGui.TextUnformatted("秘影指定目标");
        ImGui.SameLine();
        var showTargets = configuration.ShowSecretTargetsInFloatingWindow;
        if (DrawFloatingVisibilityCheckbox("##secret-target-floating-toggle", ref showTargets))
        {
            configuration.ShowSecretTargetsInFloatingWindow = showTargets;
            configuration.Save();
        }
        ImGui.TextDisabled("导航会先尝试 Lifestream 传送到目标地图，再用 vnavmesh 前往坐标。坐标来自灰机 Wiki / xivdaily。 ");

        foreach (var group in PhantomWeaponGuide.SecretTargets.GroupBy(target => target.Zone))
        {
            var targets = group.ToArray();
            var completed = targets.Count(target => configuration.CompletedTasks.Contains(target.Key));
            var fateCount = GetSecretFateCount(targets[0].TerritoryType);
            var doneStr = completed == 4 && fateCount >= 5
                ? $"{group.Key} 已全部完成（{completed}/4, {Math.Min(fateCount, 5)}/5）"
                : $"{group.Key} ({completed}/4, {Math.Min(fateCount, 5)}/5)";
            if (!ImGui.CollapsingHeader($"{doneStr}##secret-zone-{group.Key}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                continue;
            }

            ImGui.ProgressBar((completed + Math.Min(fateCount, 5)) / 9f, new Vector2(-1, 0), $"总进度 {completed + Math.Min(fateCount, 5)}/9（目标 {completed}/4，FATE {Math.Min(fateCount, 5)}/5）");

            ImGui.TextUnformatted("金牌 FATE");
            ImGui.SameLine();
            if (ImGui.SmallButton($"-##fate-minus-{targets[0].TerritoryType}"))
            {
                SetSecretFateCount(targets[0].TerritoryType, fateCount - 1);
            }

            ImGui.SameLine();
            ImGui.TextUnformatted($"{Math.Min(fateCount, 5)}/5");
            ImGui.SameLine();
            if (ImGui.SmallButton($"+##fate-plus-{targets[0].TerritoryType}"))
            {
                SetSecretFateCount(targets[0].TerritoryType, fateCount + 1);
            }

            if (ImGui.BeginTable($"secret-targets-{group.Key}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56);
                ImGui.TableSetupColumn("目标", ImGuiTableColumnFlags.WidthStretch, 1.2f);
                ImGui.TableSetupColumn("坐标", ImGuiTableColumnFlags.WidthFixed, 160);
                ImGui.TableSetupColumn("导航", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableHeadersRow();

                foreach (var target in targets)
                {
                    DrawSecretTargetRow(target);
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawSecretTargetRow(PhantomWeaponTarget target)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var done = configuration.CompletedTasks.Contains(target.Key);
        if (ImGui.Checkbox($"##done-{target.Key}", ref done))
        {
            if (done)
            {
                configuration.CompletedTasks.Add(target.Key);
            }
            else
            {
                configuration.CompletedTasks.Remove(target.Key);
            }

            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(target.Name);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(target.UseWorldCoords
            ? $"W:{target.WorldX:F1}, {target.WorldY:F1}, {target.WorldZ:F1}"
            : $"X:{target.MapX:F2} Y:{target.MapY:F2}");

        ImGui.TableNextColumn();
        if (ImGui.SmallButton($"导航##nav-{target.Key}"))
        {
            vnav.NavigateTo(target, configuration.UseFlightNavigation);
        }
    }

    private bool DrawFloatingVisibilityCheckbox(string id, ref bool value)
    {
        if (ImGui.Checkbox($"显示到悬浮窗{id}", ref value))
        {
            return true;
        }

        return false;
    }

    private void DrawFloatingObjectiveWindow()
    {
        if (!configuration.Enabled || !configuration.ShowFloatingObjectiveWindow)
        {
            return;
        }

        var territory = DalamudApi.ClientState.TerritoryType;
        var localTargets = PhantomWeaponGuide.SecretTargets
            .Where(target => target.TerritoryType == territory)
            .ToArray();

        PhantomWeaponTarget[] targets;
        if (configuration.FloatingManualMode)
        {
            targets = GetFloatingSecretTargets();
        }
        else if (localTargets.Length > 0)
        {
            targets = localTargets;
            if (configuration.FloatingSecretTerritoryType != territory)
            {
                configuration.FloatingSecretTerritoryType = territory;
                configuration.Save();
            }
        }
        else
        {
            targets = GetFloatingSecretTargets();
        }

        ImGui.SetNextWindowSize(new Vector2(300, 0), ImGuiCond.FirstUseEver);
        var floatingOpen = configuration.ShowFloatingObjectiveWindow;
        if (!ImGui.Begin("秘影目标##floating-secret-targets", ref floatingOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            if (configuration.ShowFloatingObjectiveWindow != floatingOpen)
            {
                configuration.ShowFloatingObjectiveWindow = floatingOpen;
                configuration.Save();
            }

            ImGui.End();
            return;
        }

        if (configuration.ShowFloatingObjectiveWindow != floatingOpen)
        {
            configuration.ShowFloatingObjectiveWindow = floatingOpen;
            configuration.Save();
        }

        DrawFloatingContextMenu();

        if (configuration.ShowSecretTargetsInFloatingWindow)
        {
            var zone = targets[0].Zone;
            var completed = targets.Count(target => configuration.CompletedTasks.Contains(target.Key));
            var targetTerritory = targets[0].TerritoryType;
            var fateCount = GetSecretFateCount(targetTerritory);
            ImGui.TextUnformatted(territory == targetTerritory && !configuration.FloatingManualMode ? zone : $"{zone}");
            ImGui.SameLine();
            if (ImGui.SmallButton("停止导航##float-stop-nav"))
            {
                vnav.Stop();
            }

        ImGui.SameLine();
        if (ImGui.SmallButton("<##float-prev-zone"))
        {
            configuration.FloatingManualMode = true;
            SwitchFloatingSecretZone(-1);
            configuration.Save();
            ImGui.End();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换上一张");

        ImGui.SameLine();
        if (ImGui.SmallButton(">##float-next-zone"))
        {
            configuration.FloatingManualMode = true;
            SwitchFloatingSecretZone(1);
            configuration.Save();
            ImGui.End();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换下一张");

        ImGui.SameLine();
        if (ImGui.SmallButton("当##float-auto-zone"))
        {
            configuration.FloatingManualMode = false;
            configuration.Save();
            ImGui.End();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换到当前地图");

            ImGui.ProgressBar((completed + Math.Min(fateCount, 5)) / 9f, new Vector2(-1, 0), $"{completed + Math.Min(fateCount, 5)}/9");

        ImGui.TextUnformatted($"金牌 FATE {Math.Min(fateCount, 5)}/5");
        ImGui.SameLine();
        if (ImGui.SmallButton("-##float-fate-minus"))
        {
            SetSecretFateCount(targetTerritory, fateCount - 1);
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("+##float-fate-plus"))
        {
            SetSecretFateCount(targetTerritory, fateCount + 1);
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("最近FATE##float-nav-fate"))
        {
            var player = DalamudApi.ObjectTable[0];
            if (player != null)
            {
                var nearest = DalamudApi.FateTable
                    .Where(f => f != null && DalamudApi.FateTable.IsValid(f))
                    .Where(f => f!.State is FateState.Preparing or FateState.Running or FateState.Ending)
                    .Select(f => f!)
                    .OrderBy(f => Vector3.Distance(player.Position, f.Position))
                    .FirstOrDefault();

                if (nearest != null)
                {
                    var fatePos = nearest.Position;
                    var playerDist = Vector2.Distance(new Vector2(player.Position.X, player.Position.Z), new Vector2(fatePos.X, fatePos.Z));
                    var aetherytePos = vnav.GetNearestCurrentTerritoryAetherytePosition(fatePos);
                    if (aetherytePos.HasValue)
                    {
                        var aetheryteDist = Vector2.Distance(new Vector2(aetherytePos.Value.X, aetherytePos.Value.Z), new Vector2(fatePos.X, fatePos.Z));
                        if (playerDist <= aetheryteDist)
                        {
                            vnav.NavigateTo(new Vector3(fatePos.X, fatePos.Y, fatePos.Z), configuration.UseFlightNavigation);
                            PrintChat($"导航到最近FATE: {nearest.Name}（自身距FATE {playerDist:F0} ≤ 水晶距FATE {aetheryteDist:F0}，直接前往）。");
                        }
                        else
                        {
                            vnav.TeleportAndNavigate(new Vector3(fatePos.X, fatePos.Y, fatePos.Z), configuration.UseFlightNavigation);
                            PrintChat($"导航到最近FATE: {nearest.Name}（自身距FATE {playerDist:F0} > 水晶距FATE {aetheryteDist:F0}，先传送）。");
                        }
                    }
                    else if (playerDist > 200f)
                    {
                        vnav.TeleportAndNavigate(new Vector3(fatePos.X, fatePos.Y, fatePos.Z), configuration.UseFlightNavigation);
                        PrintChat($"导航到最近FATE: {nearest.Name}（未找到水晶坐标，距离{playerDist:F0}，先传送再前往）。");
                    }
                    else
                    {
                        vnav.NavigateTo(new Vector3(fatePos.X, fatePos.Y, fatePos.Z), configuration.UseFlightNavigation);
                        PrintChat($"导航到最近FATE: {nearest.Name}（未找到水晶坐标，距离{playerDist:F0}，直接前往）。");
                    }
                }
                else
                {
                    PrintChat("当前地图没有活跃的FATE。");
                }
            }
        }

        ImGui.Separator();
        foreach (var target in targets)
        {
            var done = configuration.CompletedTasks.Contains(target.Key);
            if (configuration.AutoHideCompletedFloatingItems && done)
            {
                continue;
            }

            if (ImGui.Checkbox($"##float-target-done-{target.Key}", ref done))
            {
                if (done)
                {
                    configuration.CompletedTasks.Add(target.Key);
                }
                else
                {
                    configuration.CompletedTasks.Remove(target.Key);
                }

                configuration.Save();
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(target.UseWorldCoords
                ? $"{target.Name}  W:{target.WorldX:F0},{target.WorldY:F0},{target.WorldZ:F0}"
                : $"{target.Name}  X:{target.MapX:F1} Y:{target.MapY:F1}");
            ImGui.SameLine();
            if (ImGui.SmallButton($"导航##float-nav-{target.Key}"))
            {
                vnav.NavigateTo(target, configuration.UseFlightNavigation);
                PrintChat($"开始导航到 {target.Zone} {target.Name}");
            }
        }

            if (completed == targets.Length && fateCount >= 5)
            {
                ImGui.TextUnformatted("当前地图秘影目标已完成。");
            }
        }

        if (configuration.ShowSecretDutiesInFloatingWindow)
        {
            DrawFloatingSecretDuties();
        }

        ImGui.End();
    }

    private void DrawFloatingSecretDuties()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("迷宫/讨伐");
        var anyVisible = false;
        foreach (var group in PhantomWeaponGuide.SecretDutyGroups)
        {
            var completed = group.Duties.Count(duty => configuration.CompletedTasks.Contains(duty.Key));
            if (configuration.AutoHideCompletedFloatingItems && completed == group.Duties.Count)
            {
                continue;
            }

            anyVisible = true;
            if (!ImGui.CollapsingHeader($"{GetFloatingDutyGroupName(group)} ({completed}/{group.Duties.Count})##float-{group.Key}"))
            {
                continue;
            }

            foreach (var duty in group.Duties)
            {
                var done = configuration.CompletedTasks.Contains(duty.Key);
                if (configuration.AutoHideCompletedFloatingItems && done)
                {
                    continue;
                }

                if (ImGui.Checkbox($"{duty.Name}##float-duty-{duty.Key}", ref done))
                {
                    if (done)
                    {
                        configuration.CompletedTasks.Add(duty.Key);
                    }
                    else
                    {
                        configuration.CompletedTasks.Remove(duty.Key);
                    }

                    configuration.Save();
                }
            }
        }

        if (!anyVisible)
        {
            ImGui.TextUnformatted("秘影迷宫/讨伐已完成。");
        }
    }

    private static string GetFloatingDutyGroupName(PhantomWeaponDutyGroup group)
    {
        return group.Name.Replace("迷宫或讨伐任务：", string.Empty, StringComparison.Ordinal);
    }

    private void DrawFloatingContextMenu()
    {
        if (!ImGui.BeginPopupContextWindow("floating-secret-targets-context", ImGuiPopupFlags.MouseButtonRight))
        {
            return;
        }

        if (ImGui.MenuItem("打开主窗口"))
        {
            OpenMainWindow();
        }

        var useFlight = configuration.UseFlightNavigation;
        if (ImGui.MenuItem("飞行导航", string.Empty, useFlight))
        {
            configuration.UseFlightNavigation = !useFlight;
            configuration.Save();
        }

        var autoMarkKills = configuration.AutoMarkSecretKills;
        if (ImGui.MenuItem("自动标记击杀", string.Empty, autoMarkKills))
        {
            configuration.AutoMarkSecretKills = !autoMarkKills;
            configuration.Save();
        }

        var showDuties = configuration.ShowSecretDutiesInFloatingWindow;
        if (ImGui.MenuItem("悬浮迷宫/讨伐", string.Empty, showDuties))
        {
            configuration.ShowSecretDutiesInFloatingWindow = !showDuties;
            configuration.Save();
        }

        if (ImGui.MenuItem("关闭悬浮窗"))
        {
            configuration.ShowFloatingObjectiveWindow = false;
            configuration.Save();
        }

        ImGui.EndPopup();
    }

    private PhantomWeaponTarget[] GetFloatingSecretTargets()
    {
        var configuredTargets = PhantomWeaponGuide.SecretTargets
            .Where(target => target.TerritoryType == configuration.FloatingSecretTerritoryType)
            .ToArray();

        if (configuredTargets.Length > 0)
        {
            return configuredTargets;
        }

        var initialTargets = PhantomWeaponGuide.SecretTargets
            .GroupBy(target => target.TerritoryType)
            .OrderBy(group => group.Count(target => configuration.CompletedTasks.Contains(target.Key)) + Math.Min(GetSecretFateCount(group.Key), 5))
            .First()
            .ToArray();

        configuration.FloatingSecretTerritoryType = initialTargets[0].TerritoryType;
        configuration.Save();
        return initialTargets;
    }

    private void SwitchFloatingSecretZone(int delta)
    {
        var territories = PhantomWeaponGuide.SecretTargets
            .Select(target => target.TerritoryType)
            .Distinct()
            .ToArray();

        var index = Array.IndexOf(territories, configuration.FloatingSecretTerritoryType);
        if (index < 0)
        {
            index = 0;
        }

        index = (index + delta + territories.Length) % territories.Length;
        configuration.FloatingSecretTerritoryType = territories[index];
        configuration.Save();
    }

    private int GetSecretFateCount(uint territoryType)
        => Math.Clamp(configuration.Progress.GetValueOrDefault(GetSecretFateKey(territoryType)), 0, 5);

    private void SetSecretFateCount(uint territoryType, int value)
    {
        configuration.Progress[GetSecretFateKey(territoryType)] = Math.Clamp(value, 0, 5);
        configuration.Save();
    }

    private static string GetSecretFateKey(uint territoryType)
        => $"secret-fate-{territoryType}";

    private void ResetCurrentStage()
    {
        var stages = PhantomWeaponGuide.Stages;
        if (configuration.SelectedStageIndex < 0 || configuration.SelectedStageIndex >= stages.Count)
        {
            return;
        }

        var stage = stages[configuration.SelectedStageIndex];
        foreach (var requirement in stage.Requirements)
        {
            configuration.Progress.Remove(requirement.Key);
        }

        foreach (var task in stage.Tasks)
        {
            configuration.CompletedTasks.Remove(task.Key);
        }

        configuration.Save();
    }
}
