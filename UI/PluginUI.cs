using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Diagnostics;
using System.Numerics;

namespace Phantom;

public sealed class PluginUI
{
    private const string OuterLaNoscea = "拉诺西亚外地";
    private static readonly ZodiacWorldCoordinate OuterLaNosceaMineEntrance = new(75.81f, 52.79f, -540.95f, "武伽玛罗矿山洞口");
    private sealed record OverviewSeries(
        string SectionKey,
        string SeriesKey,
        string Name,
        string Note,
        IReadOnlyList<PhantomWeaponJob> Jobs,
        IReadOnlyList<PhantomWeaponProgressStage> Stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> ItemLookup);

    private static readonly string WindowTitle = $"肝武助手 v{typeof(PluginUI).Assembly.GetName().Version?.ToString(4) ?? "0.0.0.0"}";
    private static readonly string IconPath = Path.Combine(Path.GetDirectoryName(typeof(PluginUI).Assembly.Location) ?? string.Empty, "icon.png");
    private static readonly (string Key, string Label, string Count)[] MainSections =
    {
        ("overview", "总览", ""),
        ("zodiac", "古武 · Zodiac", "-"),
        ("anima", "魂武 · Anima", "-"),
        ("eureka", "优武 · Eurekan", "-"),
        ("resistance", "义武 · Resistance", "-"),
        ("manderville", "曼武 · Manderville", "-"),
        ("elegant", "雅武 · Elegant", "-"),
        ("phantom", "幻武 · Phantom", "5"),
        ("skysteel", "天钢 · Skysteel", "-"),
        ("splendorous", "莫雯 · Splendorous", "-"),
        ("cosmic", "宇宙 · Cosmic", "-"),
        ("ultimate", "绝武 · Ultimate", "-"),
        ("deep-dungeon", "深武 · Deep Dungeon", "-"),
        ("yokai", "妖表联动", "37"),
        ("settings", "设置", "-"),
        ("fate", "危命助手", "-"),
        ("hunt", "狩猎助手", "-"),
    };
    private static readonly (string Key, string Label, string Count)[] WorkspaceSections =
        MainSections.Where(section => section.Key is not "settings" and not "fate" and not "hunt").ToArray();
    private static readonly (string Key, string Label, string Count)[] ToolSections =
        MainSections.Where(section => section.Key is "settings" or "fate" or "hunt").ToArray();
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
    private readonly AutoDutyService autoDuty;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? weaponItemLookup;
    private Dictionary<string, IReadOnlyList<Item>>? phantomRewardWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? mandervilleWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? elegantWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? cosmicToolItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? zodiacWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? animaWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? eurekaWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? resistanceWeaponItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? skysteelToolItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? splendorousToolItemLookup;
    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>? ultimateWeaponItemLookup;
    private readonly Dictionary<string, Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>> deepDungeonWeaponItemLookups = new(StringComparer.Ordinal);
    private IReadOnlyList<YokaiRewardProgress> yokaiResults = Array.Empty<YokaiRewardProgress>();
    private readonly YokaiProgressService yokaiProgress = new();
    private bool isMainWindowOpen;
    private int selectedMainSection;
    private bool showWeaponProgressTab = true;
    private string? progressSeriesKey = "phantom";
    private readonly HashSet<string> stageSelectedSeries = new(StringComparer.Ordinal);
    private int selectedDeepDungeonIndex;
    private bool floatingPhantomTargetsOpen;
    private bool floatingPhantomDutiesOpen;
    private bool floatingZodiacMonitorOpen = true;
    private bool floatingPhantomMonitorOpen = true;
    private bool floatingFateAssistantOpen = true;
    private bool floatingHuntAssistantOpen = true;
    private string backpackOrganizeSearch = string.Empty;
    private List<BackpackItemSummary> backpackOrganizeItems = new();
    private PendingBackpackMove? pendingBackpackMove;
    private readonly Dictionary<uint, int> backpackMovedByItem = new();
    private readonly HashSet<uint> backpackSkippedItemIds = new();
    private bool backpackOrganizerRunning;
    private bool backpackOrganizerWaitingForSaddlebag;
    private bool backpackOrganizerWaitingForSaddlebagWindow;
    private DateTime backpackOrganizerStartedUtc;
    private DateTime backpackOrganizerReadyUtc;

    private sealed record BackpackItemSummary(uint ItemId, string Name, int Quantity);
    private sealed record PendingBackpackMove(
        InventoryType SourceType,
        int SourceSlot,
        InventoryType TargetType,
        int TargetSlot,
        uint ItemId,
        uint RawItemId,
        int SourceQuantity,
        int TargetQuantity,
        DateTime StartedUtc,
        DateTime? ConfirmedUtc = null,
        DateTime? SourceChangedUtc = null,
        int SourceReducedQuantity = 0);

    private static readonly InventoryType[] BackpackOrganizeSources =
    {
        InventoryType.Inventory1, InventoryType.Inventory2,
        InventoryType.Inventory3, InventoryType.Inventory4,
    };

    private static readonly InventoryType[] BackpackOrganizeTargets =
    {
        InventoryType.SaddleBag1, InventoryType.SaddleBag2,
        InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag2,
    };
    private static readonly HashSet<uint> ChroniclerFateTerritories = new() { 1252, 1346 };
    private static readonly HashSet<uint> UnsupportedFateTerritories = new() { 732, 763, 795, 827, 920, 975 };

    public PluginUI(PluginConfiguration configuration, VnavService vnav, AutoDutyService autoDuty)
    {
        this.configuration = configuration;
        this.vnav = vnav;
        this.autoDuty = autoDuty;
    }

    public void OpenMainWindow()
    {
        isMainWindowOpen = true;
    }

    public void Draw()
    {
        ProcessBackpackOrganizer();
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

        DrawSidebarLabel("武器工坊");
        foreach (var section in WorkspaceSections)
        {
            DrawSidebarButton(GetMainSectionIndex(section.Key), section.Label, GetSidebarCount(section), GetSidebarIcon(section.Key));
        }

        ImGui.Separator();
        DrawSidebarLabel("Tools");
        foreach (var section in ToolSections)
        {
            DrawSidebarButton(GetMainSectionIndex(section.Key), section.Label, GetSidebarCount(section), GetSidebarIcon(section.Key));
        }

        ImGui.Separator();
        if (ImGui.Button("前往幻境村##sidebar-occult-village", new Vector2(-1f, 0f)))
        {
            vnav.GoToOccultVillage();
        }

        if (ImGui.Button("反馈与建议##sidebar-feedback", new Vector2(-1f, 0f)))
        {
            OpenUrl(
                "https://discord.com/channels/1258981591124938762/1533030634623074466",
                "已打开反馈页面。",
                "打开反馈页面");
        }

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

    private string GetSidebarCount((string Key, string Label, string Count) section)
    {
        if (section.Key == "phantom")
        {
            return GetOwnedWeaponCount("phantom", PhantomWeaponGuide.WeaponJobs, PhantomWeaponGuide.ProgressStages, GetPhantomWeaponItemLookup()).ToString();
        }

        if (section.Key == "manderville")
        {
            return GetOwnedWeaponCount("manderville", MandervilleWeaponGuide.WeaponJobs, MandervilleWeaponGuide.ProgressStages, GetMandervilleWeaponItemLookup()).ToString();
        }

        if (section.Key == "elegant")
        {
            return GetOwnedWeaponCount("elegant", RelicWeaponGuide.ElegantWeaponJobs, RelicWeaponGuide.ElegantProgressStages, GetElegantWeaponItemLookup()).ToString();
        }

        if (section.Key == "cosmic")
        {
            return GetOwnedWeaponCount("cosmic", RelicWeaponGuide.CosmicToolJobs, RelicWeaponGuide.CosmicProgressStages, GetCosmicToolItemLookup()).ToString();
        }

        if (section.Key == "zodiac")
        {
            return GetOwnedWeaponCount("zodiac", RelicWeaponGuide.ZodiacWeaponJobs, RelicWeaponGuide.ZodiacProgressStages, GetZodiacWeaponItemLookup()).ToString();
        }

        if (section.Key == "anima")
        {
            return GetOwnedWeaponCount("anima", RelicWeaponGuide.AnimaWeaponJobs, RelicWeaponGuide.AnimaProgressStages, GetAnimaWeaponItemLookup()).ToString();
        }

        if (section.Key == "eureka")
        {
            return GetOwnedWeaponCount("eureka", RelicWeaponGuide.EurekaWeaponJobs, RelicWeaponGuide.EurekaProgressStages, GetEurekaWeaponItemLookup()).ToString();
        }

        if (section.Key == "resistance")
        {
            return GetOwnedWeaponCount("resistance", RelicWeaponGuide.ResistanceWeaponJobs, RelicWeaponGuide.ResistanceProgressStages, GetResistanceWeaponItemLookup()).ToString();
        }

        if (section.Key == "skysteel")
        {
            return GetOwnedWeaponCount("skysteel", RelicWeaponGuide.SkysteelToolJobs, RelicWeaponGuide.SkysteelProgressStages, GetSkysteelToolItemLookup()).ToString();
        }

        if (section.Key == "splendorous")
        {
            return GetOwnedWeaponCount("splendorous", RelicWeaponGuide.SplendorousToolJobs, RelicWeaponGuide.SplendorousProgressStages, GetSplendorousToolItemLookup()).ToString();
        }

        if (section.Key == "ultimate")
        {
            return GetOwnedWeaponCount("ultimate", RelicWeaponGuide.UltimateWeaponJobs, RelicWeaponGuide.UltimateProgressStages, GetUltimateWeaponItemLookup()).ToString();
        }

        if (section.Key == "deep-dungeon")
        {
            return DeepDungeonWeaponGuide.Series.Sum(series => GetOwnedStageCount(series.SeriesKey, series.Jobs, series.Stages, GetDeepDungeonWeaponItemLookup(series))).ToString();
        }

        if (RelicWeaponGuide.Series.ContainsKey(section.Key))
        {
            return "-";
        }

        return section.Count;
    }

    private int GetOwnedWeaponCount(
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup)
    {
        var characterKey = GetCurrentCharacterKey();
        if (characterKey.Length == 0 || !configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var syncedItems))
        {
            return 0;
        }

        return jobs.Count(job => GetHighestSyncedStage(seriesKey, job, stages, itemLookup, syncedItems) != null);
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
            "elegant" => FontAwesomeIcon.Magic,
            "skysteel" => FontAwesomeIcon.Hammer,
            "splendorous" => FontAwesomeIcon.Wrench,
            "cosmic" => FontAwesomeIcon.Rocket,
            "ultimate" => FontAwesomeIcon.Trophy,
            "deep-dungeon" => FontAwesomeIcon.Archway,
            "yokai" => FontAwesomeIcon.Paw,
            "settings" => FontAwesomeIcon.Cog,
            "fate" => FontAwesomeIcon.Flag,
            "hunt" => FontAwesomeIcon.Crosshairs,
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
            case "elegant":
                DrawElegantWeaponWorkspace();
                break;
            case "yokai":
                DrawYokaiWorkspace();
                break;
            case "manderville":
                DrawMandervilleWorkspace();
                break;
            case "deep-dungeon":
                DrawDeepDungeonWeaponWorkspace();
                break;
            case "settings":
                DrawSettingsWorkspace();
                break;
            case "fate":
                DrawFateAssistantWorkspace();
                break;
            case "hunt":
                DrawHuntAssistantWorkspace();
                break;
            default:
                if (RelicWeaponGuide.Series.TryGetValue(section.Key, out var series))
                {
                    DrawRelicSeriesWorkspace(series);
                }
                else
                {
                    DrawWeaponSeriesPlaceholder(section.Label);
                }

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
            SyncAllCurrentCharacterProgress();
        }
    }

    private void DrawWeaponHubOverview()
    {
        var characterKey = GetCurrentCharacterKey();
        var syncedItems = characterKey.Length > 0 && configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? stored
            : new Dictionary<string, List<uint>>();
        var series = GetOverviewSeries();
        var ownedJobs = series.Sum(entry => CountOwnedJobs(entry.SeriesKey, entry.Jobs, entry.Stages, entry.ItemLookup, syncedItems));
        var totalJobs = series.Sum(GetOverviewTotal);
        var ultimateOwned = CountOwnedStages("ultimate", RelicWeaponGuide.UltimateWeaponJobs, RelicWeaponGuide.UltimateProgressStages, GetUltimateWeaponItemLookup(), syncedItems);
        var ultimateTotal = RelicWeaponGuide.UltimateWeaponJobs.Sum(job => RelicWeaponGuide.UltimateProgressStages.Count(stage => GetUltimateWeaponItemLookup().ContainsKey((job.Key, stage.Key))));
        var yokaiOwned = characterKey.Length > 0 && configuration.YokaiOwnedRewardKeysByCharacter.TryGetValue(characterKey, out var yokaiKeys) ? yokaiKeys.Count : 0;
        var retainerCoverage = GetRetainerCacheCoverage();

        if (ImGui.BeginTable("weapon-hub-summary", 4, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
        {
            DrawSummaryCard("武器收藏", $"{ownedJobs}/{totalJobs}", "深武按独立职业阶段格统计");
            DrawSummaryCard("绝武收藏", $"{ultimateOwned}/{ultimateTotal}", "七个绝本独立统计");
            DrawSummaryCard("妖表奖励", $"{yokaiOwned}/{YokaiWatchGuide.Rewards.Count}", "宠物、武器与坐骑");
            var syncTime = characterKey.Length > 0 && configuration.WeaponProgressSyncTimes.TryGetValue(characterKey, out var time) ? time : "未同步";
            DrawSummaryCard("库存覆盖", $"{retainerCoverage.Current}/{retainerCoverage.Total} 雇员", $"最近同步：{syncTime}");
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "系列收藏");
        if (ImGui.BeginTable("overview-series-grid", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
        {
            foreach (var entry in series)
            {
                var owned = CountOwnedJobs(entry.SeriesKey, entry.Jobs, entry.Stages, entry.ItemLookup, syncedItems);
                DrawOverviewSeriesCard(entry, owned);
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        if (ImGui.BeginTable("overview-bottom-grid", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
        {
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "同步覆盖");
            ImGui.TextDisabled("背包、兵装库与装备中：实时读取");
            ImGui.TextDisabled("鞍囊、收藏柜与投影台：读取游戏缓存（可能不是最新数据）");
            ImGui.TextDisabled($"雇员库存：{retainerCoverage.Current}/{retainerCoverage.Total} 个已在本次登录后打开并刷新，{retainerCoverage.Cached}/{retainerCoverage.Total} 个已缓存（可能不是最新数据）");
            if (retainerCoverage.Total > retainerCoverage.Current)
            {
                ImGui.TextColored(new Vector4(0.92f, 0.72f, 0.38f, 1f), "未打开的雇员可能使用旧缓存。");
            }

            ImGui.TextDisabled("/道具检索 物品 可刷新投影台");

            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "快捷入口");
            if (ImGui.Button("幻武进度##overview-phantom", new Vector2(120f, 0f))) selectedMainSection = GetMainSectionIndex("phantom");
            ImGui.SameLine();
            if (ImGui.Button("绝武总进度##overview-ultimate", new Vector2(120f, 0f)))
            {
                selectedMainSection = GetMainSectionIndex("ultimate");
                stageSelectedSeries.Remove("ultimate");
                progressSeriesKey = "ultimate";
            }
            if (ImGui.Button("前往幻境村##overview-village", new Vector2(120f, 0f))) vnav.GoToOccultVillage();
            ImGui.SameLine();
            if (ImGui.Button("妖表联动##overview-yokai", new Vector2(120f, 0f))) selectedMainSection = GetMainSectionIndex("yokai");
            ImGui.EndTable();
        }
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

    private IReadOnlyList<OverviewSeries> GetOverviewSeries()
        => new[]
    {
        new OverviewSeries("zodiac", "zodiac", "古武", "Zodiac", RelicWeaponGuide.ZodiacWeaponJobs, RelicWeaponGuide.ZodiacProgressStages, GetZodiacWeaponItemLookup()),
        new OverviewSeries("anima", "anima", "魂武", "Anima", RelicWeaponGuide.AnimaWeaponJobs, RelicWeaponGuide.AnimaProgressStages, GetAnimaWeaponItemLookup()),
        new OverviewSeries("eureka", "eureka", "优武", "Eurekan", RelicWeaponGuide.EurekaWeaponJobs, RelicWeaponGuide.EurekaProgressStages, GetEurekaWeaponItemLookup()),
        new OverviewSeries("resistance", "resistance", "义武", "Resistance", RelicWeaponGuide.ResistanceWeaponJobs, RelicWeaponGuide.ResistanceProgressStages, GetResistanceWeaponItemLookup()),
        new OverviewSeries("manderville", "manderville", "曼德维尔武器", "Manderville", MandervilleWeaponGuide.WeaponJobs, MandervilleWeaponGuide.ProgressStages, GetMandervilleWeaponItemLookup()),
        new OverviewSeries("elegant", "elegant", "雅武", "Elegant Weapons", RelicWeaponGuide.ElegantWeaponJobs, RelicWeaponGuide.ElegantProgressStages, GetElegantWeaponItemLookup()),
        new OverviewSeries("phantom", "phantom", "幻境武器", "5 个阶段", PhantomWeaponGuide.WeaponJobs, PhantomWeaponGuide.ProgressStages, GetPhantomWeaponItemLookup()),
        new OverviewSeries("skysteel", "skysteel", "天钢工具", "Skysteel", RelicWeaponGuide.SkysteelToolJobs, RelicWeaponGuide.SkysteelProgressStages, GetSkysteelToolItemLookup()),
        new OverviewSeries("splendorous", "splendorous", "莫雯工具", "Splendorous", RelicWeaponGuide.SplendorousToolJobs, RelicWeaponGuide.SplendorousProgressStages, GetSplendorousToolItemLookup()),
        new OverviewSeries("cosmic", "cosmic", "宇宙工具", "Cosmic", RelicWeaponGuide.CosmicToolJobs, RelicWeaponGuide.CosmicProgressStages, GetCosmicToolItemLookup()),
        new OverviewSeries("ultimate", "ultimate", "绝境战武器", "7 个绝本", RelicWeaponGuide.UltimateWeaponJobs, RelicWeaponGuide.UltimateProgressStages, GetUltimateWeaponItemLookup()),
    }.Concat(DeepDungeonWeaponGuide.Series.Select(series => new OverviewSeries("deep-dungeon", series.SeriesKey, series.Name, $"{series.Version} / 等级 {series.EntryLevel}", series.Jobs, series.Stages, GetDeepDungeonWeaponItemLookup(series)))).ToArray();

    private static int CountOwnedJobs(
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
        => seriesKey.StartsWith("deep-dungeon-", StringComparison.Ordinal)
            ? jobs.Sum(job => stages.Count(stage => IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems)))
            : jobs.Count(job => GetHighestSyncedStage(seriesKey, job, stages, itemLookup, syncedItems) != null);

    private static int GetOverviewTotal(OverviewSeries entry)
        => entry.SeriesKey.StartsWith("deep-dungeon-", StringComparison.Ordinal)
            ? entry.Jobs.Count * entry.Stages.Count
            : entry.Jobs.Count;

    private static int CountOwnedStages(
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
        => jobs.Sum(job => stages.Count(stage => GetSyncedStageItems(seriesKey, job, stage, itemLookup, syncedItems).Any()));

    private int GetOwnedStageCount(
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup)
    {
        var characterKey = GetCurrentCharacterKey();
        if (characterKey.Length == 0 || !configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var syncedItems))
        {
            return 0;
        }

        return jobs.Sum(job => stages.Count(stage => IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems)));
    }

    private void DrawOverviewSeriesCard(OverviewSeries entry, int owned)
    {
        ImGui.TableNextColumn();
        var cursor = ImGui.GetCursorScreenPos();
        var width = Math.Max(220f, ImGui.GetColumnWidth() - 8f);
        var size = new Vector2(width, 82f);
        var drawList = ImGui.GetWindowDrawList();
        var total = GetOverviewTotal(entry);
        var ratio = total == 0 ? 0f : owned / (float)total;
        var accent = entry.SeriesKey == "ultimate"
            ? new Vector4(0.66f, 0.58f, 0.91f, 1f)
            : IsToolSeries(entry.SeriesKey)
                ? new Vector4(0.87f, 0.58f, 0.38f, 1f)
                : new Vector4(0.40f, 0.83f, 0.79f, 1f);

        drawList.AddRectFilled(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.09f, 0.13f, 0.16f, 0.94f)), 8f);
        drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.19f, 0.27f, 0.31f, 1f)), 8f);
        drawList.AddRectFilled(cursor + new Vector2(12f, 60f), cursor + new Vector2(width - 12f, 66f), ImGui.GetColorU32(new Vector4(0.16f, 0.21f, 0.23f, 1f)), 3f);
        drawList.AddRectFilled(cursor + new Vector2(12f, 60f), cursor + new Vector2(12f + (width - 24f) * ratio, 66f), ImGui.GetColorU32(accent), 3f);

        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 10f));
        ImGui.TextUnformatted(entry.Name);
        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 32f));
        ImGui.TextDisabled(entry.Note);
        ImGui.SetCursorScreenPos(cursor + new Vector2(width - 60f, 12f));
        ImGui.TextColored(accent, $"{owned}/{total}");
        ImGui.SetCursorScreenPos(cursor);
        if (ImGui.InvisibleButton($"overview-series-{entry.SeriesKey}", size))
        {
            selectedMainSection = GetMainSectionIndex(entry.SectionKey);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{entry.Name}\n已持有 {(entry.SeriesKey.StartsWith("deep-dungeon-", StringComparison.Ordinal) ? "职业阶段格" : "职业")} {owned}/{total}\n点击进入系列页面");
        }

        ImGui.SetCursorScreenPos(cursor + new Vector2(0f, size.Y + 6f));
    }

    private void SyncAllCurrentCharacterProgress()
    {
        var characterKey = GetCurrentCharacterKey();
        if (characterKey.Length == 0)
        {
            return;
        }

        foreach (var entry in GetOverviewSeries())
        {
            SyncCurrentCharacterWeaponProgress(characterKey, entry.SeriesKey, entry.Jobs, entry.Stages, entry.ItemLookup);
        }

        yokaiResults = yokaiProgress.ScanCurrentCharacter();
        configuration.YokaiOwnedRewardKeysByCharacter[characterKey] = yokaiResults.Where(reward => reward.Owned).Select(reward => reward.Key).ToList();
        configuration.YokaiSyncTimesByCharacter[characterKey] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        configuration.Save();
    }

    private static unsafe (int Current, int Cached, int Total) GetRetainerCacheCoverage()
    {
        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return (0, 0, 0);
        }

        var current = 0;
        var cached = 0;
        var total = 0;
        foreach (var entry in finder->RetainerInventories)
        {
            total++;
            if (!entry.Item2.IsNull)
            {
                cached++;
            }

            if (finder->IsRetainerCurrent(entry.Item1))
            {
                current++;
            }
        }

        return (current, cached, total);
    }

    private void DrawPhantomWeaponWorkspace()
    {
        DrawPhantomToolbar();
        ImGui.Separator();
        DrawStageTabs();
    }

    private void DrawPhantomToolbar()
    {
        if (ImGui.Button("重置当前阶段进度"))
        {
            ResetCurrentStage();
        }

        ImGui.SameLine();
        DrawWikiButton("打开幻境武器 Wiki", "phantom", "https://ff14.huijiwiki.com/wiki/%E5%B9%BB%E5%A2%83%E6%AD%A6%E5%99%A8");
        ImGui.SameLine();
        var monitorPhantom = configuration.ShowSecretTargetsInFloatingWindow || configuration.ShowSecretDutiesInFloatingWindow;
        if (ImGui.Checkbox("监控幻武##phantom-floating-window", ref monitorPhantom))
        {
            configuration.ShowSecretTargetsInFloatingWindow = monitorPhantom;
            configuration.ShowSecretDutiesInFloatingWindow = monitorPhantom;
            configuration.Save();
        }

        ImGui.SameLine();
        var autoMarkKills = configuration.AutoMarkSecretKills;
        if (ImGui.Checkbox("自动标记击杀##phantom-auto-mark-kills", ref autoMarkKills))
        {
            configuration.AutoMarkSecretKills = autoMarkKills;
            configuration.Save();
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

    private void DrawRelicSeriesWorkspace(RelicWeaponSeries series)
    {
        var stages = series.Stages;
        if (!configuration.SelectedRelicStageIndexes.TryGetValue(series.Key, out var selectedIndex) || selectedIndex < 0 || selectedIndex >= stages.Count)
        {
            selectedIndex = 0;
            configuration.SelectedRelicStageIndexes[series.Key] = selectedIndex;
        }

        ZodiacJobProgress? zodiacProgress = null;
        if (series.Key == "zodiac")
        {
            zodiacProgress = DrawZodiacJobSelector();
            ImGui.Spacing();
        }

        ImGui.TextWrapped(series.Summary);
        ImGui.TextDisabled(series.EnglishName);
        ImGui.SameLine();
        var sourceLabel = series.Key == "zodiac" ? "打开上古武器Wiki" : $"打开{series.Name} Wiki";
        DrawWikiButton(sourceLabel, series.Key, series.SourceUrl);
        if (!string.IsNullOrWhiteSpace(series.SecondarySourceUrl))
        {
            ImGui.SameLine();
            var secondaryLabel = series.Key == "zodiac" ? "打开黄道武器Wiki" : $"打开{series.Name} Wiki 2";
            DrawWikiButton(secondaryLabel, $"{series.Key}-secondary", series.SecondarySourceUrl);
        }
        if (series.Key == "zodiac")
        {
            ImGui.SameLine();
            var monitorZodiac = configuration.ShowZodiacMonitorInFloatingWindow;
            if (ImGui.Checkbox("监控古武##zodiac-floating-window", ref monitorZodiac))
            {
                configuration.ShowZodiacMonitorInFloatingWindow = monitorZodiac;
                configuration.Save();
            }

            ImGui.SameLine();
            DrawZodiacCurrentCoordinateButton();
        }
        ImGui.Separator();

        ImGui.BeginGroup();
        var showSeriesProgress = IsSeriesProgressActive(series.Key);
        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            var tabLabel = stage.Name
                .Replace("上古武器·", string.Empty, StringComparison.Ordinal)
                .Replace("黄道武器·", string.Empty, StringComparison.Ordinal)
                .Replace("元灵武器·", string.Empty, StringComparison.Ordinal)
                .Replace("义军武器·", string.Empty, StringComparison.Ordinal)
                .Replace("禁地兵装·", string.Empty, StringComparison.Ordinal)
                .Replace("工具", string.Empty, StringComparison.Ordinal);
            if (DrawContentTabButton($"{series.Key}-stage-{stage.Key}", tabLabel, !showSeriesProgress && selectedIndex == i))
            {
                stageSelectedSeries.Add(series.Key);
                progressSeriesKey = null;
                selectedIndex = i;
                configuration.SelectedRelicStageIndexes[series.Key] = selectedIndex;
                configuration.Save();
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

        var progressLabel = IsToolSeries(series.Key) ? "工具进度" : "武器进度";
        if (series.Key == "ultimate")
        {
            progressLabel = "总进度";
        }

        if (DrawContentTabButton($"{series.Key}-weapon-progress", progressLabel, showSeriesProgress))
        {
            stageSelectedSeries.Remove(series.Key);
            progressSeriesKey = series.Key;
        }

        ImGui.EndGroup();
        ImGui.Separator();
        if (series.Key == "ultimate")
        {
            if (showSeriesProgress)
            {
                DrawUltimateTotalProgressPanel();
            }
            else
            {
                DrawUltimateWeaponProgressPanel(stages[selectedIndex].Key);
            }
        }
        else if (showSeriesProgress)
        {
            if (series.Key == "cosmic")
            {
                DrawCosmicToolProgressPanel();
            }
            else if (series.Key == "skysteel")
            {
                DrawSkysteelToolProgressPanel();
            }
            else if (series.Key == "splendorous")
            {
                DrawSplendorousToolProgressPanel();
            }
            else if (series.Key == "zodiac")
            {
                DrawZodiacWeaponProgressPanel();
            }
            else if (series.Key == "anima")
            {
                DrawAnimaWeaponProgressPanel();
            }
            else if (series.Key == "eureka")
            {
                DrawEurekaWeaponProgressPanel();
            }
            else if (series.Key == "resistance")
            {
                DrawResistanceWeaponProgressPanel();
            }
            else
            {
                DrawPendingWeaponProgressPanel(series.Name);
            }
        }
        else
        {
            var zodiacRequirements = zodiacProgress?.RequirementProgress;
            var zodiacObjectives = zodiacProgress?.CompletedObjectives;
            if (series.Key == "zodiac" && zodiacProgress == null)
            {
                zodiacRequirements = new Dictionary<string, int>(StringComparer.Ordinal);
                zodiacObjectives = new HashSet<string>(StringComparer.Ordinal);
            }

            DrawStage(stages[selectedIndex], zodiacRequirements, zodiacObjectives);
            if (series.Key == "zodiac" && zodiacProgress != null)
            {
                DrawZodiacObjectives(stages[selectedIndex].Key, zodiacProgress);
                if (stages[selectedIndex].Key == "zodiac-zeta")
                {
                    DrawZodiacZetaProgress(zodiacProgress);
                }
            }
        }
    }

    private ZodiacJobProgress? DrawZodiacJobSelector()
    {
        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            ImGui.TextDisabled("登录角色后可记录按角色、按职业拆分的古武制作进度。");
            return null;
        }

        var selectedJob = RelicWeaponGuide.ZodiacWeaponJobs
            .FirstOrDefault(job => job.Key == configuration.SelectedZodiacJobKey)
            ?? RelicWeaponGuide.ZodiacWeaponJobs[0];
        if (selectedJob.Key != configuration.SelectedZodiacJobKey)
        {
            configuration.SelectedZodiacJobKey = selectedJob.Key;
            configuration.Save();
        }

        ImGui.TextUnformatted("制作职业");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180f);
        if (ImGui.BeginCombo("##zodiac-job", selectedJob.Name))
        {
            foreach (var job in RelicWeaponGuide.ZodiacWeaponJobs)
            {
                var selected = job.Key == selectedJob.Key;
                if (ImGui.Selectable($"{job.Name}##zodiac-job-{job.Key}", selected))
                {
                    configuration.SelectedZodiacJobKey = job.Key;
                    selectedJob = job;
                    configuration.Save();
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        if (!configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress))
        {
            characterProgress = new ZodiacCharacterProgress();
            configuration.ZodiacProgressByCharacter[characterKey] = characterProgress;
        }

        if (!characterProgress.Jobs.TryGetValue(selectedJob.Key, out var jobProgress))
        {
            jobProgress = new ZodiacJobProgress();
            characterProgress.Jobs[selectedJob.Key] = jobProgress;
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{GetCurrentCharacterLabel()} · 独立保存");
        return jobProgress;
    }

    private void DrawZodiacObjectives(string stageKey, ZodiacJobProgress progress)
    {
        if (stageKey == "zodiac-atma")
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("魂晶地区 FATE");
            ImGui.TextDisabled("装备天极武器后，在对应地区完成任意 FATE，有概率获得该地区魂晶。");
            if (ImGui.BeginTable("zodiac-atma-objectives", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56f);
                ImGui.TableSetupColumn("地区", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 112f);
                ImGui.TableHeadersRow();
                foreach (var objective in ZodiacGuide.AtmaTerritories)
                {
                    var done = progress.CompletedObjectives.Contains(objective.Key);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ImGui.Checkbox($"##zodiac-atma-{objective.Key}", ref done))
                    {
                        if (done)
                        {
                            progress.CompletedObjectives.Add(objective.Key);
                        }
                        else
                        {
                            progress.CompletedObjectives.Remove(objective.Key);
                        }

                        configuration.Save();
                    }

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{objective.Name} · {objective.Zone}");
                    ImGui.TableNextColumn();
                    if (ImGui.SmallButton($"传送到地图##zodiac-atma-teleport-{objective.Key}"))
                    {
                        vnav.TeleportToMap(objective.Zone);
                    }
                }

                ImGui.EndTable();
            }
        }
        else if (stageKey == "zodiac-animus")
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("黄道十二文书");
            ImGui.TextDisabled("每本书包含 10 个指定敌人、3 个副本、3 个 FATE 和 3 个理符目标。");
            ImGui.TextDisabled($"已完成文书：{progress.CompletedBooks.Count}/{ZodiacGuide.AnimusBooks.Count}");
            if (ImGui.BeginTable("zodiac-animus-books", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                for (var index = 0; index < ZodiacGuide.AnimusBooks.Count; index++)
                {
                    if (index % 3 == 0)
                    {
                        ImGui.TableNextRow();
                    }

                    ImGui.TableNextColumn();
                    var book = ZodiacGuide.AnimusBooks[index];
                    var done = progress.CompletedBooks.Contains(book.Key);
                    if (ImGui.Checkbox($"##zodiac-book-{book.Key}", ref done))
                    {
                        if (done)
                        {
                            progress.CompletedBooks.Add(book.Key);
                        }
                        else
                        {
                            progress.CompletedBooks.Remove(book.Key);
                        }

                        configuration.Save();
                    }

                    ImGui.SameLine(0f, 4f);
                    ImGui.TextUnformatted(book.Name);
                    ImGui.SameLine(0f, 8f);
                    var active = progress.SelectedBookKey == book.Key;
                    if (ImGui.RadioButton($"##zodiac-book-select-{book.Key}", active))
                    {
                        progress.SelectedBookKey = book.Key;
                        configuration.Save();
                    }

                    ImGui.SameLine(0f, 3f);
                    ImGui.TextDisabled("选择");
                }

                ImGui.EndTable();
            }

            var selectedBook = ZodiacGuide.AnimusBooks.FirstOrDefault(book => book.Key == progress.SelectedBookKey);
            if (selectedBook != null)
            {
                ImGui.Spacing();
                ImGui.TextUnformatted($"当前文书：{selectedBook.Name}");
                DrawZodiacBookProgressSummary(selectedBook, progress.CompletedObjectives, progress.CompletedBooks);
                DrawZodiacBookObjectives(selectedBook, progress.CompletedObjectives);
            }
        }
    }

    private void DrawZodiacBookProgressSummary(
        ZodiacBookGuide book,
        HashSet<string> completedObjectives,
        HashSet<string> completedBooks)
    {
        var monsters = book.Monsters.Count(objective => completedObjectives.Contains(objective.Key));
        var duties = book.Duties.Count(objective => completedObjectives.Contains(objective.Key));
        var fates = book.Fates.Count(objective => completedObjectives.Contains(objective.Key));
        var leves = book.Leves.Count(objective => completedObjectives.Contains(objective.Key));
        var total = book.Monsters.Count + book.Duties.Count + book.Fates.Count + book.Leves.Count;
        var completed = monsters + duties + fates + leves;
        ImGui.TextDisabled($"敌人 {monsters}/{book.Monsters.Count}  ·  副本 {duties}/{book.Duties.Count}  ·  FATE {fates}/{book.Fates.Count}  ·  理符 {leves}/{book.Leves.Count}  ·  总计 {completed}/{total}");
        ImGui.ProgressBar(total == 0 ? 0f : (float)completed / total, new Vector2(-1f, 0f), $"{completed}/{total}");
        if (completed == total && total > 0 && completedBooks.Add(book.Key))
        {
            configuration.Save();
        }
    }

    private void DrawZodiacBookObjectives(ZodiacBookGuide book, HashSet<string> completedObjectives)
    {
        var customCoordinates = GetZodiacUserCoordinates();
        var monsters = book.Monsters.Select(objective => new ZodiacObjectiveRow(objective.Key, $"{objective.Name} · {objective.Zone}", objective.Zone, FormatZodiacLocation(objective.LocationNotes, MergeZodiacCoordinates(objective.Key, objective.Coordinates, customCoordinates)), MergeZodiacCoordinates(objective.Key, objective.Coordinates, customCoordinates), WorldCoordinates: objective.WorldCoordinates)).ToArray();
        var duties = book.Duties.Select(objective => new ZodiacObjectiveRow(objective.Key, objective.Name, string.Empty, objective.LocationNotes, null, DutyName: objective.Name)).ToArray();
        var fates = book.Fates.Select(objective => new ZodiacObjectiveRow(objective.Key, $"{objective.Name} · {objective.Zone}", objective.Zone, objective.LocationNotes, objective.MapX > 0f ? new[] { new ZodiacCoordinate(objective.MapX, objective.MapY, "FATE") } : null, objective.PrerequisiteNpcName, objective.PrerequisiteNpcZone, objective.PrerequisiteNpcMapX, objective.PrerequisiteNpcMapY)).ToArray();
        var leves = book.Leves.Select(objective => new ZodiacObjectiveRow(objective.Key, $"[{FormatLeveType(objective)}] {objective.Name} · {objective.Zone}", objective.Zone, $"等级 {objective.Level}。{objective.LocationNotes}", objective.MapX > 0f ? new[] { new ZodiacCoordinate(objective.MapX, objective.MapY, "NPC") } : null)).ToArray();
        DrawZodiacObjectiveSection("指定敌人", monsters, completedObjectives);
        DrawZodiacObjectiveSection("指定副本", duties, completedObjectives);
        DrawZodiacObjectiveSection("指定 FATE", fates, completedObjectives);
        DrawZodiacObjectiveSection("指定理符", leves, completedObjectives);
    }

    private sealed record ZodiacObjectiveRow(string Key, string Label, string Zone, string Notes, IReadOnlyList<ZodiacCoordinate>? Coordinates, string? NpcName = null, string? NpcZone = null, float NpcX = 0f, float NpcY = 0f, string? DutyName = null, IReadOnlyList<ZodiacWorldCoordinate>? WorldCoordinates = null);

    private static string FormatLeveType(ZodiacLeveObjective objective)
        => objective.GrandCompany == null ? objective.Category : $"{objective.Category}·{objective.GrandCompany}";

    private void DrawZodiacCurrentCoordinateButton()
    {
        if (!ImGui.SmallButton("获取当前坐标##zodiac-current-coordinate"))
        {
            return;
        }

        if (!vnav.TryGetCurrentMapCoordinate(out var zoneName, out var mapX, out var mapY))
        {
            PrintChat("[古武] 无法读取当前地图坐标，请确认已进入野外地图。 ");
            return;
        }

        var coordinate = $"{zoneName} ({mapX:0.0}, {mapY:0.0})";
        ImGui.SetClipboardText(coordinate);
        PrintChat($"[古武] 当前坐标已复制：{coordinate}");
    }

    private Dictionary<string, List<ZodiacCoordinate>> GetZodiacUserCoordinates()
    {
        var characterKey = GetCurrentCharacterKey();
        return configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            && characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress)
            ? jobProgress.UserCoordinates
            : new Dictionary<string, List<ZodiacCoordinate>>(StringComparer.Ordinal);
    }

    private static IReadOnlyList<ZodiacCoordinate>? MergeZodiacCoordinates(
        string key,
        IReadOnlyList<ZodiacCoordinate>? wikiCoordinates,
        Dictionary<string, List<ZodiacCoordinate>> userCoordinates)
    {
        var merged = new List<ZodiacCoordinate>();
        if (wikiCoordinates != null)
        {
            merged.AddRange(wikiCoordinates);
        }

        if (userCoordinates.TryGetValue(key, out var custom))
        {
            merged.AddRange(custom);
        }

        return merged.Count == 0 ? null : merged;
    }

    private static string FormatZodiacLocation(string notes, IReadOnlyList<ZodiacCoordinate>? coordinates)
    {
        if (coordinates is { Count: > 0 })
        {
            return $"坐标：{string.Join("、", coordinates)}";
        }

        return notes;
    }

    private void DrawZodiacObjectiveSection(
        string title,
        IEnumerable<ZodiacObjectiveRow> objectives,
        HashSet<string> completedObjectives)
    {
        var rows = objectives.ToArray();
        ImGui.Spacing();
        var completed = rows.Count(row => completedObjectives.Contains(row.Key));
        if (!ImGui.CollapsingHeader($"{title} ({completed}/{rows.Length})##zodiac-objectives-header-{title}", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        if (ImGui.BeginTable($"zodiac-objectives-{title}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56f);
            ImGui.TableSetupColumn("目标", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 176f);
            ImGui.TableHeadersRow();
            foreach (var row in rows)
            {
                var done = completedObjectives.Contains(row.Key);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.Checkbox($"##zodiac-detail-{row.Key}", ref done))
                {
                    if (done)
                    {
                        completedObjectives.Add(row.Key);
                    }
                    else
                    {
                        completedObjectives.Remove(row.Key);
                    }

                    configuration.Save();
                }

                ImGui.TableNextColumn();
                var objectiveProgress = GetZodiacObjectiveProgress(row.Key);
                ImGui.TextWrapped(objectiveProgress > 0 ? $"{row.Label}  ({objectiveProgress}/3)" : row.Label);
                if (!string.IsNullOrWhiteSpace(row.Notes))
                {
                    ImGui.TextDisabled(row.Notes);
                }

                ImGui.TableNextColumn();
                if (row.DutyName != null)
                {
                    if (ImGui.SmallButton($"AD执行##zodiac-duty-ad-{row.Key}"))
                    {
                        autoDuty.Run(row.DutyName);
                    }
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row.Zone) && !row.Zone.StartsWith("未知", StringComparison.Ordinal))
                {
                    if (ImGui.SmallButton($"传送到地图##zodiac-teleport-{row.Key}"))
                    {
                        vnav.TeleportToMap(row.Zone);
                    }

                    if (row.Coordinates is { Count: > 0 } || row.WorldCoordinates is { Count: > 0 })
                    {
                        ImGui.SameLine(0f, 6f);
                        var isOuterLaNosceaMine = title == "指定敌人" && row.Zone == OuterLaNoscea;
                        var coordinateButtonLabel = title == "指定理符" ? "NPC导航" : isOuterLaNosceaMine ? "洞口/小怪" : "坐标导航";
                        if (ImGui.SmallButton($"{coordinateButtonLabel}##zodiac-coordinate-{row.Key}"))
                        {
                            ImGui.OpenPopup($"zodiac-coordinate-menu-{row.Key}");
                        }

                        if (ImGui.BeginPopup($"zodiac-coordinate-menu-{row.Key}"))
                        {
                            if (isOuterLaNosceaMine)
                            {
                                if (ImGui.Selectable($"飞到洞口 W:({OuterLaNosceaMineEntrance.X:0.00}, {OuterLaNosceaMineEntrance.Y:0.00}, {OuterLaNosceaMineEntrance.Z:0.00})##zodiac-cave-entrance-{row.Key}"))
                                {
                                    vnav.NavigateToWorldCoordinate(row.Zone, new Vector3(OuterLaNosceaMineEntrance.X, OuterLaNosceaMineEntrance.Y, OuterLaNosceaMineEntrance.Z));
                                }
                                ImGui.Separator();
                                ImGui.TextUnformatted("标记小怪刷新点");
                            }
                            else
                            {
                                ImGui.TextUnformatted("选择刷新点");
                            }
                            ImGui.Separator();
                            for (var coordinateIndex = 0; coordinateIndex < (row.WorldCoordinates?.Count ?? 0); coordinateIndex++)
                            {
                                var coordinate = row.WorldCoordinates![coordinateIndex];
                                var label = $"W:({coordinate.X:0.00}, {coordinate.Y:0.00}, {coordinate.Z:0.00})";
                                if (ImGui.Selectable($"前往 {label}##zodiac-world-coordinate-select-{row.Key}-{coordinateIndex}"))
                                {
                                    vnav.NavigateToWorldCoordinate(row.Zone, new Vector3(coordinate.X, coordinate.Y, coordinate.Z));
                                }
                            }

                            if (row.WorldCoordinates is { Count: > 0 } && row.Coordinates is { Count: > 0 })
                            {
                                ImGui.Separator();
                                ImGui.TextDisabled("地图刷新点");
                            }

                            for (var coordinateIndex = 0; coordinateIndex < (row.Coordinates?.Count ?? 0); coordinateIndex++)
                            {
                                var coordinate = row.Coordinates![coordinateIndex];
                                var label = coordinate.Note == null
                                    ? $"({coordinate.MapX:0.0}, {coordinate.MapY:0.0})"
                                    : $"({coordinate.MapX:0.0}, {coordinate.MapY:0.0}) {coordinate.Note}";
                                if (ImGui.Selectable($"前往 {label}##zodiac-coordinate-select-{row.Key}-{coordinateIndex}"))
                                {
                                    if (isOuterLaNosceaMine)
                                    {
                                        vnav.SetMapFlag(row.Zone, coordinate.MapX, coordinate.MapY);
                                    }
                                    else
                                    {
                                        vnav.NavigateToMapCoordinate(row.Zone, coordinate.MapX, coordinate.MapY);
                                    }
                                }
                            }

                            ImGui.EndPopup();
                        }
                    }

                }
            }

            ImGui.EndTable();
        }
    }

    private int GetZodiacObjectiveProgress(string key)
    {
        var characterKey = GetCurrentCharacterKey();
        return configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            && characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress)
            ? jobProgress.RequirementProgress.GetValueOrDefault(key)
            : 0;
    }

    private void SaveCurrentZodiacCoordinate(string objectiveKey, string expectedZone)
    {
        if (!vnav.TryGetCurrentMapCoordinate(out var zoneName, out var mapX, out var mapY))
        {
            PrintChat("[古武] 无法读取当前地图坐标，请确认已进入野外地图。 ");
            return;
        }

        if (!string.Equals(zoneName, expectedZone, StringComparison.Ordinal))
        {
            PrintChat($"[古武] 当前地图是“{zoneName}”，目标地图是“{expectedZone}”，未保存坐标。 ");
            return;
        }

        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey)
            || !configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            || !characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress))
        {
            PrintChat("[古武] 当前角色进度尚未初始化，未保存坐标。 ");
            return;
        }

        if (!jobProgress.UserCoordinates.TryGetValue(objectiveKey, out var coordinates))
        {
            coordinates = new List<ZodiacCoordinate>();
            jobProgress.UserCoordinates[objectiveKey] = coordinates;
        }

        var duplicate = coordinates.Any(coordinate =>
            Math.Abs(coordinate.MapX - mapX) < 0.05f && Math.Abs(coordinate.MapY - mapY) < 0.05f);
        if (!duplicate)
        {
            coordinates.Add(new ZodiacCoordinate(mapX, mapY, "用户提交"));
            configuration.Save();
        }

        PrintChat(duplicate
            ? $"[古武] 坐标 ({mapX:0.0}, {mapY:0.0}) 已存在。"
            : $"[古武] 已为当前目标保存坐标 ({mapX:0.0}, {mapY:0.0})。 ");
    }

    private void DrawZodiacZetaProgress(ZodiacJobProgress progress)
    {
        const int total = 12;
        var completed = 0;
        ImGui.Spacing();
        ImGui.TextUnformatted("本我光阶段");
        ImGui.TextDisabled("每个本我阶段单独记录；完成 12 个阶段后，本我进度完成。");
        if (ImGui.BeginTable("zodiac-zeta-mahatma", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56f);
            ImGui.TableSetupColumn("阶段", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableHeadersRow();
            for (var index = 1; index <= total; index++)
            {
                var key = $"zodiac-zeta-mahatma-{index}";
                var done = progress.CompletedObjectives.Contains(key);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.Checkbox($"##{key}", ref done))
                {
                    if (done)
                    {
                        progress.CompletedObjectives.Add(key);
                    }
                    else
                    {
                        progress.CompletedObjectives.Remove(key);
                    }

                    configuration.Save();
                }

                if (done)
                {
                    completed++;
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"本我光阶段 {index}");
                ImGui.TableNextColumn();
                ImGui.TextDisabled(done ? "完成" : "未完成");
            }

            ImGui.EndTable();
        }

        progress.RequirementProgress["zodiac-zeta-mahatma"] = completed;
        ImGui.ProgressBar((float)completed / total, new Vector2(-1f, 0f), $"{completed}/{total}");
    }

    private void DrawZodiacObjectiveRow(string key, string label, HashSet<string> completedObjectives)
    {
        var done = completedObjectives.Contains(key);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (ImGui.Checkbox($"##zodiac-objective-{key}", ref done))
        {
            if (done)
            {
                completedObjectives.Add(key);
            }
            else
            {
                completedObjectives.Remove(key);
            }

            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
    }

    private void DrawMandervilleWorkspace()
    {
        var stages = MandervilleWeaponGuide.Stages;
        if (configuration.SelectedMandervilleStageIndex < 0 || configuration.SelectedMandervilleStageIndex >= stages.Count)
        {
            configuration.SelectedMandervilleStageIndex = 0;
        }

        ImGui.BeginGroup();
        var showMandervilleProgress = IsSeriesProgressActive("manderville");
        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (DrawContentTabButton($"manderville-stage-{stage.Key}", stage.Name.Replace("曼德维尔武器·", string.Empty, StringComparison.Ordinal), !showMandervilleProgress && configuration.SelectedMandervilleStageIndex == i))
            {
                stageSelectedSeries.Add("manderville");
                progressSeriesKey = null;
                configuration.SelectedMandervilleStageIndex = i;
                configuration.Save();
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

        if (DrawContentTabButton("manderville-weapon-progress", "武器进度", showMandervilleProgress))
        {
            stageSelectedSeries.Remove("manderville");
            progressSeriesKey = "manderville";
        }

        ImGui.EndGroup();
        ImGui.SameLine();
        DrawWikiButton("打开曼德维尔武器 Wiki", "manderville", "https://ff14.huijiwiki.com/wiki/%E6%9B%BC%E5%BE%B7%E7%BB%B4%E5%B0%94%E6%AD%A6%E5%99%A8");
        ImGui.Separator();
        if (showMandervilleProgress)
        {
            DrawMandervilleWeaponProgressPanel();
        }
        else
        {
            DrawStage(stages[configuration.SelectedMandervilleStageIndex]);
        }
    }

    private void DrawElegantWeaponWorkspace()
    {
        ImGui.TextWrapped("6.x 雅武（Elegant Weapons）持有追踪。优雅武器会自动计入该职业的基础武器阶段。");
        ImGui.SameLine();
        DrawWikiButton("打开优雅武器兑换 Wiki", "elegant", "https://ff14.huijiwiki.com/wiki/%E7%89%A9%E5%93%81:%E5%85%A8%E5%A4%A9%E5%BC%BA%E5%8C%96%E8%8D%AF");
        ImGui.Separator();
        DrawElegantWeaponProgressPanel();
    }

    private void DrawDeepDungeonWeaponWorkspace()
    {
        selectedDeepDungeonIndex = Math.Clamp(selectedDeepDungeonIndex, 0, DeepDungeonWeaponGuide.Series.Count - 1);
        for (var i = 0; i < DeepDungeonWeaponGuide.Series.Count; i++)
        {
            var dungeon = DeepDungeonWeaponGuide.Series[i];
            if (DrawContentTabButton($"deep-dungeon-{dungeon.Key}", dungeon.Name, selectedDeepDungeonIndex == i))
            {
                selectedDeepDungeonIndex = i;
            }

            if (i < DeepDungeonWeaponGuide.Series.Count - 1)
            {
                ImGui.SameLine(0f, 4f);
            }
        }

        var series = DeepDungeonWeaponGuide.Series[selectedDeepDungeonIndex];
        ImGui.Separator();
        ImGui.TextWrapped(series.Summary);
        ImGui.TextDisabled($"{series.EnglishName} / 版本 {series.Version} / 进入等级 {series.EntryLevel}");
        ImGui.SameLine();
        DrawWikiButton($"打开{series.Name} Wiki", series.SeriesKey, series.SourceUrl);
        ImGui.SameLine();
        if (ImGui.Button("DEBUG 导出全部深武 ID##export-all-deep-dungeon"))
        {
            ExportAllDeepDungeonWeaponItemIds();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("导出四个深层迷宫全部 7 组武器的 Item ID，预期共 154 条（含骑士盾牌）。");
        }
        ImGui.Separator();
        DrawWeaponProgressPanel(
            series.SeriesKey,
            series.Name,
            series.Jobs,
            series.Stages,
            GetDeepDungeonWeaponItemLookup(series),
            _ => true,
            _ => $"当前实际持有 {GetOwnedStageCount(series.SeriesKey, series.Jobs, series.Stages, GetDeepDungeonWeaponItemLookup(series))}/{series.Jobs.Count * series.Stages.Count} 个职业阶段格；骑士必须同时持有剑盾才计入。",
            independentStages: true);
    }

    private bool IsSeriesProgressActive(string seriesKey)
        => progressSeriesKey == seriesKey || (seriesKey != "ultimate" && !stageSelectedSeries.Contains(seriesKey));

    private static void DrawPendingWeaponProgressPanel(string seriesName)
    {
        ImGui.TextDisabled($"{seriesName}武器进度按当前角色保存。逐职业武器名称与 Item RowId 尚未录入，暂不执行背包或 ItemFinder 扫描。 ");
        ImGui.Spacing();

        if (!ImGui.BeginTable($"{seriesName}-weapon-progress-placeholder", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var job in PhantomWeaponGuide.WeaponJobs)
        {
            ImGui.TableNextColumn();
            var cursor = ImGui.GetCursorScreenPos();
            var size = new Vector2(112f, 54f);
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.10f, 0.13f, 0.16f, 0.92f)), 6f);
            drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(new Vector4(0.22f, 0.31f, 0.36f, 1f)), 6f);
            ImGui.SetCursorScreenPos(cursor + new Vector2(10f, 8f));
            ImGui.TextUnformatted(job.Name);
            ImGui.SetCursorScreenPos(cursor + new Vector2(10f, 29f));
            ImGui.TextDisabled("待补充数据");
            ImGui.SetCursorScreenPos(cursor + new Vector2(0f, size.Y + 6f));
        }

        ImGui.EndTable();
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
        DrawWeaponProgressPanel(
            "phantom",
            "幻境武器",
            PhantomWeaponGuide.WeaponJobs,
            PhantomWeaponGuide.ProgressStages,
            GetPhantomWeaponItemLookup(),
            stage => stage.Key == "secret",
            completed => $"秘影完成职业 {completed}/{PhantomWeaponGuide.WeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");
    }

    private void DrawPhantomRewardWeapons()
    {
        var characterKey = GetCurrentCharacterKey();
        var syncedItems = characterKey.Length > 0 && configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? stored
            : new Dictionary<string, List<uint>>();
        var rewardLookup = GetPhantomRewardWeaponItemLookup();

        if (PhantomWeaponGuide.RewardWeapons.Count == 0)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "达成奖励");
        var tileWidth = configuration.ShowWeaponProgressIcons ? 120f : 94f;
        if (!ImGui.BeginTable("phantom-reward-weapons", PhantomWeaponGuide.RewardWeapons.Count, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var reward in PhantomWeaponGuide.RewardWeapons)
        {
            ImGui.TableNextColumn();
            DrawPhantomRewardWeaponTile("phantom", reward, rewardLookup, syncedItems, tileWidth);
        }

        ImGui.EndTable();
    }

    private void DrawMandervilleWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "manderville",
            "曼德维尔武器",
            MandervilleWeaponGuide.WeaponJobs,
            MandervilleWeaponGuide.ProgressStages,
            GetMandervilleWeaponItemLookup(),
            stage => stage.Key == "manderville-complete",
            completed => $"盈满完成职业 {completed}/{MandervilleWeaponGuide.WeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawElegantWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "elegant",
            "雅武",
            RelicWeaponGuide.ElegantWeaponJobs,
            RelicWeaponGuide.ElegantProgressStages,
            GetElegantWeaponItemLookup(),
            stage => stage.Key == "elegant",
            completed => $"优雅完成职业 {completed}/{RelicWeaponGuide.ElegantWeaponJobs.Count}。持有优雅武器时会自动点亮基础武器。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawCosmicToolProgressPanel()
        => DrawWeaponProgressPanel(
            "cosmic",
            "宇宙工具",
            RelicWeaponGuide.CosmicToolJobs,
            RelicWeaponGuide.CosmicProgressStages,
            GetCosmicToolItemLookup(),
            stage => stage.Key == "cosmic-stellar",
            completed => $"群星完成职业 {completed}/{RelicWeaponGuide.CosmicToolJobs.Count}。未显示的工具通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawZodiacWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "zodiac",
            "古武",
            RelicWeaponGuide.ZodiacWeaponJobs,
            RelicWeaponGuide.ZodiacProgressStages,
            GetZodiacWeaponItemLookup(),
            stage => stage.Key == "zodiac-zeta",
            completed => $"本我完成职业 {completed}/{RelicWeaponGuide.ZodiacWeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawAnimaWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "anima",
            "魂武",
            RelicWeaponGuide.AnimaWeaponJobs,
            RelicWeaponGuide.AnimaProgressStages,
            GetAnimaWeaponItemLookup(),
            stage => stage.Key == "anima-lux",
            completed => $"灵光完成职业 {completed}/{RelicWeaponGuide.AnimaWeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawEurekaWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "eureka",
            "优武",
            RelicWeaponGuide.EurekaWeaponJobs,
            RelicWeaponGuide.EurekaProgressStages,
            GetEurekaWeaponItemLookup(),
            stage => stage.Key == "eureka-physeos",
            completed => $"补正完成职业 {completed}/{RelicWeaponGuide.EurekaWeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawResistanceWeaponProgressPanel()
        => DrawWeaponProgressPanel(
            "resistance",
            "义武",
            RelicWeaponGuide.ResistanceWeaponJobs,
            RelicWeaponGuide.ResistanceProgressStages,
            GetResistanceWeaponItemLookup(),
            stage => stage.Key == "resistance-blades",
            completed => $"女王完成职业 {completed}/{RelicWeaponGuide.ResistanceWeaponJobs.Count}。未显示的武器通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawSkysteelToolProgressPanel()
        => DrawWeaponProgressPanel(
            "skysteel",
            "天钢工具",
            RelicWeaponGuide.SkysteelToolJobs,
            RelicWeaponGuide.SkysteelProgressStages,
            GetSkysteelToolItemLookup(),
            stage => stage.Key == "skysteel-skybuilders",
            completed => $"天工完成职业 {completed}/{RelicWeaponGuide.SkysteelToolJobs.Count}。未显示的工具通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawSplendorousToolProgressPanel()
        => DrawWeaponProgressPanel(
            "splendorous",
            "莫雯工具",
            RelicWeaponGuide.SplendorousToolJobs,
            RelicWeaponGuide.SplendorousProgressStages,
            GetSplendorousToolItemLookup(),
            stage => stage.Key == "splendorous-lodestar",
            completed => $"领航星完成职业 {completed}/{RelicWeaponGuide.SplendorousToolJobs.Count}。未显示的工具通常表示上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ");

    private void DrawUltimateWeaponProgressPanel(string stageKey)
    {
        var stageIndex = RelicWeaponGuide.UltimateProgressStages
            .Select((stage, index) => (stage, index))
            .FirstOrDefault(pair => pair.stage.Key == stageKey)
            .index;
        var selectedStage = RelicWeaponGuide.UltimateProgressStages[stageIndex];
        var displayJobs = RelicWeaponGuide.UltimateWeaponJobs
            .Where(job => job.StageItemNames.Count > stageIndex && job.StageItemNames[stageIndex].Any(name => !string.IsNullOrWhiteSpace(name)))
            .Select(job => new PhantomWeaponJob(job.Key, job.Name, new[] { job.StageItemNames[stageIndex] }))
            .ToArray();

        DrawWeaponProgressPanel(
            "ultimate",
            selectedStage.Name,
            displayJobs,
            new[] { selectedStage },
            GetUltimateWeaponItemLookup(),
            stage => stage.Key == selectedStage.Key,
            completed => $"{selectedStage.Name} 已持有职业 {completed}/{displayJobs.Length}。未显示的武器通常表示该职业当时未开放，或上次同步时不在背包、兵装库、装备栏或已加载的雇员库存。 ",
            RelicWeaponGuide.UltimateWeaponJobs,
            RelicWeaponGuide.UltimateProgressStages);
    }

    private void DrawUltimateTotalProgressPanel()
        => DrawWeaponProgressPanel(
            "ultimate",
            "绝武总进度",
            RelicWeaponGuide.UltimateWeaponJobs,
            RelicWeaponGuide.UltimateProgressStages,
            GetUltimateWeaponItemLookup(),
            stage => stage.Key == RelicWeaponGuide.UltimateProgressStages[^1].Key,
            completed => $"七个绝本分别作为一个阶段展示；绝妖星已持有职业 {completed}/{RelicWeaponGuide.UltimateWeaponJobs.Count}。未显示的武器通常表示上次同步时不在可读取的库存或雇员缓存中。 ");

    private void DrawWeaponProgressPanel(
        string seriesKey,
        string seriesName,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        Func<PhantomWeaponProgressStage, bool> isCompleteStage,
        Func<int, string> footerText,
        IReadOnlyList<PhantomWeaponJob>? syncJobs = null,
        IReadOnlyList<PhantomWeaponProgressStage>? syncStages = null,
        bool independentStages = false)
    {
        var characterKey = GetCurrentCharacterKey();
        var canSync = characterKey.Length > 0;
        var syncedItems = canSync && configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? stored
            : new Dictionary<string, List<uint>>();
        var completedJobs = 0;

        if (!canSync)
        {
            ImGui.TextColored(new Vector4(1f, 0.72f, 0.28f, 1f), "未登录角色，无法同步。");
        }

        if (ImGui.Button($"同步当前角色##sync-weapon-progress-{seriesKey}") && canSync)
        {
            syncedItems = SyncCurrentCharacterWeaponProgress(characterKey, seriesKey, syncJobs ?? jobs, syncStages ?? stages, itemLookup);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("点击同步时先扫描当前角色背包、兵装库和装备栏；若游戏 ItemFinder 已有当前道具检索结果，则读取雇员、鞍囊、投影台等缓存位置，不主动弹出持有情况窗口。");
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

        if (seriesKey == "phantom")
        {
            DrawPhantomRewardWeapons();
            ImGui.Spacing();
        }

        foreach (var job in jobs)
        {
            var highestStage = GetHighestSyncedStage(seriesKey, job, stages, itemLookup, syncedItems);
            if (independentStages
                ? stages.Any(stage => isCompleteStage(stage) && IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems))
                : highestStage != null && isCompleteStage(highestStage))
            {
                completedJobs++;
            }
        }

        if (configuration.GroupWeaponProgressByRole && IsToolSeries(seriesKey))
        {
            DrawWeaponCollectionRow(seriesKey, "制作职业", new[] { "crp", "bsm", "arm", "gsm", "ltw", "wvr", "alc", "cul" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "采集职业", new[] { "min", "btn", "fsh" }, jobs, stages, itemLookup, syncedItems);
        }
        else if (configuration.GroupWeaponProgressByRole)
        {
            DrawWeaponCollectionRow(seriesKey, "防护职能", new[] { "pld", "war", "drk", "gnb" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "治疗职能", new[] { "whm", "sch", "ast", "sge" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "近战职能 1", new[] { "mnk", "drg", "nin" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "近战职能 2", new[] { "sam", "rpr", "vpr" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "远程物理", new[] { "brd", "mch", "dnc" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "远程魔法", new[] { "blm", "smn", "rdm", "pct" }, jobs, stages, itemLookup, syncedItems);
            DrawWeaponCollectionRow(seriesKey, "特殊职业", new[] { "blu" }, jobs, stages, itemLookup, syncedItems);
        }
        else
        {
            DrawWeaponCollectionGrid(seriesKey, jobs, stages, itemLookup, syncedItems);
        }

        ImGui.TextDisabled(footerText(completedJobs));
    }

    private void DrawWeaponCollectionGrid(
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        ImGui.Spacing();
        var tileWidth = configuration.ShowWeaponProgressIcons ? 120f : 94f;
        const int columns = 5;
        if (!ImGui.BeginTable($"{seriesKey}-weapon-collection-grid", columns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var job in jobs)
        {
            ImGui.TableNextColumn();
            var highestStage = GetHighestSyncedStage(seriesKey, job, stages, itemLookup, syncedItems);
            DrawWeaponCollectionTile(seriesKey, job, highestStage, stages, itemLookup, syncedItems, tileWidth);
        }

        ImGui.EndTable();
    }

    private void DrawWeaponCollectionRow(
        string seriesKey,
        string label,
        IReadOnlyList<string> jobKeys,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), label);
        var tileWidth = configuration.ShowWeaponProgressIcons ? 120f : 94f;
        if (!ImGui.BeginTable($"{seriesKey}-weapon-row-{label}", jobKeys.Count, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        foreach (var key in jobKeys)
        {
            var job = jobs.FirstOrDefault(job => job.Key == key);
            if (job == null)
            {
                continue;
            }

            ImGui.TableNextColumn();
            var highestStage = GetHighestSyncedStage(seriesKey, job, stages, itemLookup, syncedItems);
            DrawWeaponCollectionTile(seriesKey, job, highestStage, stages, itemLookup, syncedItems, tileWidth);
        }

        ImGui.EndTable();
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetPhantomWeaponItemLookup()
    {
        weaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), PhantomWeaponGuide.ProgressItemIds);
        return weaponItemLookup;
    }

    private Dictionary<string, IReadOnlyList<Item>> GetPhantomRewardWeaponItemLookup()
    {
        phantomRewardWeaponItemLookup ??= BuildRewardWeaponItemLookup(DalamudApi.DataManager.GetExcelSheet<Item>(), PhantomWeaponGuide.RewardWeapons);
        return phantomRewardWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetMandervilleWeaponItemLookup()
    {
        mandervilleWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("manderville"));
        return mandervilleWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetElegantWeaponItemLookup()
    {
        elegantWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("elegant"));
        return elegantWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetCosmicToolItemLookup()
    {
        cosmicToolItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("cosmic"));
        return cosmicToolItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetZodiacWeaponItemLookup()
    {
        zodiacWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("zodiac"));
        return zodiacWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetAnimaWeaponItemLookup()
    {
        animaWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("anima"));
        return animaWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetEurekaWeaponItemLookup()
    {
        eurekaWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("eureka"));
        return eurekaWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetResistanceWeaponItemLookup()
    {
        resistanceWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("resistance"));
        return resistanceWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetSkysteelToolItemLookup()
    {
        skysteelToolItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("skysteel"));
        return skysteelToolItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetSplendorousToolItemLookup()
    {
        splendorousToolItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("splendorous"));
        return splendorousToolItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetUltimateWeaponItemLookup()
    {
        ultimateWeaponItemLookup ??= BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), WeaponItemIds.Get("ultimate"));
        return ultimateWeaponItemLookup;
    }

    private Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> GetDeepDungeonWeaponItemLookup(DeepDungeonWeaponSeries series)
    {
        if (!deepDungeonWeaponItemLookups.TryGetValue(series.SeriesKey, out var lookup))
        {
            var itemIds = WeaponItemIds.GetOrEmpty(series.SeriesKey).ToDictionary(entry => entry.Key, entry => entry.Value);
            var debugPath = Path.Combine(DalamudApi.PluginInterface.GetPluginConfigDirectory(), "deep-dungeon-weapon-item-ids.txt");
            if (File.Exists(debugPath))
            {
                foreach (var line in File.ReadAllLines(debugPath))
                {
                    var fields = line.Split('|', 5);
                    if (fields.Length != 5
                        || fields[0] != series.SeriesKey
                        || !uint.TryParse(fields[3], out var itemId))
                    {
                        continue;
                    }

                    var key = (fields[1], fields[2]);
                    itemIds[key] = itemIds.TryGetValue(key, out var existing)
                        ? existing.Concat(new[] { itemId }).Distinct().ToArray()
                        : new[] { itemId };
                }
            }

            lookup = BuildWeaponItemLookupById(DalamudApi.DataManager.GetExcelSheet<Item>(), itemIds);
            deepDungeonWeaponItemLookups[series.SeriesKey] = lookup;
        }

        return lookup;
    }

    private static bool IsToolSeries(string seriesKey)
        => seriesKey is "cosmic" or "skysteel" or "splendorous";

    private unsafe Dictionary<string, List<uint>> SyncCurrentCharacterWeaponProgress(
        string characterKey,
        string seriesKey,
        IReadOnlyList<PhantomWeaponJob> jobs,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup)
    {
        var synced = configuration.WeaponProgressItemsByCharacter.TryGetValue(characterKey, out var stored)
            ? new Dictionary<string, List<uint>>(stored)
            : new Dictionary<string, List<uint>>();
        if (itemLookup.Count == 0)
        {
            return synced;
        }

        var ownedItemIds = GetOwnedWeaponItemIds();
        foreach (var key in synced.Keys.Where(key => key.StartsWith($"{seriesKey}:", StringComparison.Ordinal)).ToArray())
        {
            synced.Remove(key);
        }

        if (seriesKey == "phantom")
        {
            foreach (var key in synced.Keys.Where(key => key.Count(ch => ch == ':') == 1).ToArray())
            {
                synced.Remove(key);
            }
        }

        if (seriesKey == "phantom")
        {
            var rewardLookup = GetPhantomRewardWeaponItemLookup();
            foreach (var reward in PhantomWeaponGuide.RewardWeapons)
            {
                if (!rewardLookup.TryGetValue(reward.Key, out var rewardItems))
                {
                    continue;
                }

                var ownedRewardItems = rewardItems
                    .Where(item => ownedItemIds.Contains(item.RowId) || ItemFinderHasItem(item))
                    .Select(item => item.RowId)
                    .ToList();
                if (ownedRewardItems.Count > 0)
                {
                    synced[GetRewardWeaponProgressKey(seriesKey, reward)] = ownedRewardItems;
                }
            }
        }

        foreach (var job in jobs)
        {
            foreach (var stage in stages)
            {
                if (!itemLookup.TryGetValue((job.Key, stage.Key), out var items))
                {
                    continue;
                }

                var ownedStageItems = items
                    .Where(item => ownedItemIds.Contains(item.RowId) || ItemFinderHasItem(item))
                    .Select(item => item.RowId)
                    .ToList();
                if (ownedStageItems.Count > 0)
                {
                    synced[GetWeaponProgressKey(seriesKey, job, stage)] = ownedStageItems;
                }
            }
        }

        if (configuration.DebugLogSyncedItemLocations)
        {
            LogSyncedWeaponItemLocations(seriesKey, itemLookup, synced, configuration.DebugLogMissingItemLocations);
            if (seriesKey == "phantom")
            {
                LogSyncedRewardWeaponItemLocations(seriesKey, GetPhantomRewardWeaponItemLookup(), synced, configuration.DebugLogMissingItemLocations);
            }
        }

        configuration.WeaponProgressItemsByCharacter[characterKey] = synced;
        configuration.WeaponProgressSyncTimes[characterKey] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        configuration.Save();
        return synced;
    }

    private static void LogSyncedWeaponItemLocations(
        string seriesKey,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> synced,
        bool logMissingItems)
    {
        var loggedItemIds = new HashSet<uint>();
        var syncedItemIds = synced
            .Where(entry => entry.Key.StartsWith($"{seriesKey}:", StringComparison.Ordinal))
            .SelectMany(entry => entry.Value)
            .ToHashSet();
        foreach (var items in itemLookup.Values)
        {
            foreach (var item in items)
            {
                if (!loggedItemIds.Add(item.RowId))
                {
                    continue;
                }

                var locations = GetItemStorageLocations(item);
                var status = locations.Count > 0
                    ? string.Join("、", locations)
                    : syncedItemIds.Contains(item.RowId)
                        ? "已命中，位置缓存不可用"
                        : "未找到";
                if (status == "未找到" && !logMissingItems)
                {
                    continue;
                }

                PrintChat($"DEBUG [{GetDebugSeriesDisplayName(seriesKey)}] {item.Name.ExtractText()}：{status}");
            }
        }
    }

    private static void LogSyncedRewardWeaponItemLocations(
        string seriesKey,
        IReadOnlyDictionary<string, IReadOnlyList<Item>> rewardLookup,
        IReadOnlyDictionary<string, List<uint>> synced,
        bool logMissingItems)
    {
        foreach (var reward in PhantomWeaponGuide.RewardWeapons)
        {
            if (!rewardLookup.TryGetValue(reward.Key, out var items))
            {
                if (logMissingItems)
                {
                    PrintChat($"DEBUG [{GetDebugSeriesDisplayName(seriesKey)}] {string.Join("/", reward.ItemNames)}：未匹配物品表");
                }

                continue;
            }

            var syncedItemIds = synced.TryGetValue(GetRewardWeaponProgressKey(seriesKey, reward), out var itemIds)
                ? itemIds.ToHashSet()
                : new HashSet<uint>();
            foreach (var item in items)
            {
                var locations = GetItemStorageLocations(item);
                var status = locations.Count > 0
                    ? string.Join("、", locations)
                    : syncedItemIds.Contains(item.RowId)
                        ? "已命中，位置缓存不可用"
                        : "未找到";
                if (status == "未找到" && !logMissingItems)
                {
                    continue;
                }

                PrintChat($"DEBUG [{GetDebugSeriesDisplayName(seriesKey)}] {item.Name.ExtractText()}：{status}");
            }
        }
    }

    private static string GetDebugSeriesDisplayName(string seriesKey)
        => seriesKey switch
        {
            "phantom" => "幻武",
            "manderville" => "曼武",
            "zodiac" => "古武",
            "anima" => "魂武",
            "eureka" => "优武",
            "resistance" => "义武",
            "skysteel" => "天钢",
            "splendorous" => "莫雯",
            "cosmic" => "宇宙",
            "ultimate" => "绝武",
            "deep-dungeon-palace" => "深武·死者宫殿",
            "deep-dungeon-heaven-on-high" => "深武·天之御柱",
            "deep-dungeon-eureka-orthos" => "深武·正统优雷卡",
            "deep-dungeon-pilgrims-traverse" => "深武·朝圣交错路",
            _ => seriesKey,
        };

    private static unsafe IReadOnlyList<string> GetItemStorageLocations(Item item)
    {
        var locations = new HashSet<string>(StringComparer.Ordinal);
        var itemId = NormalizeItemId(item.RowId);
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager != null)
        {
            AddInventoryLocation(inventoryManager, InventoryType.Inventory1, itemId, "背包", locations);
            AddInventoryLocation(inventoryManager, InventoryType.Inventory2, itemId, "背包", locations);
            AddInventoryLocation(inventoryManager, InventoryType.Inventory3, itemId, "背包", locations);
            AddInventoryLocation(inventoryManager, InventoryType.Inventory4, itemId, "背包", locations);
            AddInventoryLocation(inventoryManager, InventoryType.ArmoryMainHand, itemId, "兵装库", locations);
            AddInventoryLocation(inventoryManager, InventoryType.ArmoryOffHand, itemId, "兵装库", locations);
            AddInventoryLocation(inventoryManager, InventoryType.EquippedItems, itemId, "装备中", locations);
        }

        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return locations.ToArray();
        }

        if (ContainsItemId(finder->SaddleBagItemIds, itemId)
            || ContainsItemId(finder->PremiumSaddleBagItemIds, itemId))
        {
            locations.Add("鞍囊");
        }

        if (ContainsItemId(finder->GlamourDresserItemIds, itemId))
        {
            locations.Add("投影台");
        }

        var cabinetRow = DalamudApi.DataManager.GetExcelSheet<Cabinet>()
            .FirstOrDefault(row => row.Item.RowId == itemId);
        if (cabinetRow.RowId > 0 && IsCabinetItemOwned(cabinetRow.RowId, finder))
        {
            locations.Add("收藏柜");
        }

        foreach (var retainerPointer in finder->RetainerInventories.Values)
        {
            var retainer = retainerPointer.Value;
            if (retainer != null
                && (ContainsItemId(retainer->EquippedItemIds, itemId)
                    || ContainsItemId(retainer->ItemIds, itemId)))
            {
                locations.Add("雇员");
                break;
            }
        }

        AddCurrentItemFinderLocations(item, finder, locations);
        return locations.ToArray();
    }

    private static unsafe void AddInventoryLocation(
        InventoryManager* inventoryManager,
        InventoryType inventoryType,
        uint itemId,
        string location,
        HashSet<string> locations)
    {
        var container = inventoryManager->GetInventoryContainer(inventoryType);
        if (container == null)
        {
            return;
        }

        for (var index = 0; index < container->Size; index++)
        {
            if (NormalizeItemId(container->GetInventorySlot(index)->ItemId) == itemId)
            {
                locations.Add(location);
                return;
            }
        }
    }

    private static bool ContainsItemId(Span<uint> itemIds, uint itemId)
    {
        foreach (var candidateId in itemIds)
        {
            if (NormalizeItemId(candidateId) == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private static unsafe bool IsCabinetItemOwned(uint cabinetRowId, ItemFinderModule* finder)
    {
        var uiState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        if (uiState != null && uiState->Cabinet.IsCabinetLoaded() && uiState->Cabinet.IsItemInCabinet(cabinetRowId))
        {
            return true;
        }

        if (finder->CabinetState != (byte)FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet.CabinetState.Loaded)
        {
            return false;
        }

        var bits = finder->CabinetItemUnlockBits;
        var wordIndex = (int)(cabinetRowId >> 5);
        var bitIndex = (int)(cabinetRowId & 31);
        return (uint)wordIndex < (uint)bits.Length && (bits[wordIndex] & (1u << bitIndex)) != 0;
    }

    private static unsafe void AddCurrentItemFinderLocations(Item item, ItemFinderModule* finder, HashSet<string> locations)
    {
        if (finder->Result == null
            || !ContainsItemId(finder->RequestItemIds, NormalizeItemId(item.RowId)))
        {
            return;
        }

        var result = finder->Result;
        if (result->SaddleBagPage1Count + result->SaddleBagPage2Count + result->PremiumSaddleBagPage1Count + result->PremiumSaddleBagPage2Count > 0) locations.Add("鞍囊");
        if (result->ArmoireCount > 0) locations.Add("收藏柜");
        if (result->GlamourDresserCount > 0) locations.Add("投影台");
        for (var index = 0L; index < result->RetainerCount; index++)
        {
            var retainer = result->Retainer[index];
            if (retainer != null && (retainer->EquipmentSlot >= 0 || retainer->Page1Count + retainer->Page2Count + retainer->Page3Count + retainer->Page4Count + retainer->Page5Count > 0))
            {
                locations.Add("雇员");
                break;
            }
        }
    }

    private static unsafe bool ItemFinderHasItem(Item item)
    {
        var finder = ItemFinderModule.Instance();
        if (finder == null || finder->Result == null)
        {
            return false;
        }

        var itemId = item.RowId;
        var normalizedItemId = NormalizeItemId(itemId);
        var currentResultMatchesItem = false;
        foreach (var requestItemId in finder->RequestItemIds)
        {
            if (NormalizeItemId(requestItemId) == normalizedItemId)
            {
                currentResultMatchesItem = true;
                break;
            }
        }

        if (!currentResultMatchesItem)
        {
            var resultItemName = finder->Result->ItemName.ToString().Trim();
            currentResultMatchesItem = string.Equals(resultItemName, item.Name.ExtractText(), StringComparison.Ordinal);
        }

        if (!currentResultMatchesItem)
        {
            return false;
        }

        var result = finder->Result;

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
        if (inventoryManager != null)
        {
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
        }

        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return result;
        }

        foreach (var itemId in finder->SaddleBagItemIds)
        {
            var normalizedItemId = NormalizeItemId(itemId);
            if (normalizedItemId > 0)
            {
                result.Add(normalizedItemId);
            }
        }

        foreach (var itemId in finder->PremiumSaddleBagItemIds)
        {
            var normalizedItemId = NormalizeItemId(itemId);
            if (normalizedItemId > 0)
            {
                result.Add(normalizedItemId);
            }
        }

        foreach (var itemId in finder->GlamourDresserItemIds)
        {
            var normalizedItemId = NormalizeItemId(itemId);
            if (normalizedItemId > 0)
            {
                result.Add(normalizedItemId);
            }
        }

        AddCabinetItemIds(result, finder);

        foreach (var retainerPointer in finder->RetainerInventories.Values)
        {
            var retainer = retainerPointer.Value;
            if (retainer == null)
            {
                continue;
            }

            foreach (var itemId in retainer->EquippedItemIds)
            {
                var normalizedItemId = NormalizeItemId(itemId);
                if (normalizedItemId > 0)
                {
                    result.Add(normalizedItemId);
                }
            }

            foreach (var itemId in retainer->ItemIds)
            {
                var normalizedItemId = NormalizeItemId(itemId);
                if (normalizedItemId > 0)
                {
                    result.Add(normalizedItemId);
                }
            }
        }

        return result;
    }

    private static unsafe void AddCabinetItemIds(HashSet<uint> result, ItemFinderModule* finder)
    {
        var cabinet = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        var liveCabinetLoaded = cabinet != null && cabinet->Cabinet.IsCabinetLoaded();
        var cachedCabinetLoaded = finder->CabinetState == (byte)FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet.CabinetState.Loaded;
        if (!liveCabinetLoaded && !cachedCabinetLoaded)
        {
            return;
        }

        var cachedBits = finder->CabinetItemUnlockBits;
        foreach (var cabinetRow in DalamudApi.DataManager.GetExcelSheet<Cabinet>())
        {
            var owned = liveCabinetLoaded && cabinet->Cabinet.IsItemInCabinet(cabinetRow.RowId);
            if (!owned && cachedCabinetLoaded)
            {
                var wordIndex = (int)(cabinetRow.RowId >> 5);
                var bitIndex = (int)(cabinetRow.RowId & 31);
                owned = (uint)wordIndex < (uint)cachedBits.Length && (cachedBits[wordIndex] & (1u << bitIndex)) != 0;
            }

            if (owned && cabinetRow.Item.RowId > 0)
            {
                result.Add(cabinetRow.Item.RowId);
            }
        }
    }

    private static uint NormalizeItemId(uint itemId)
        => itemId >= 1_000_000 ? itemId % 1_000_000 : itemId;

    private static Dictionary<string, IReadOnlyList<Item>> BuildRewardWeaponItemLookup(
        Lumina.Excel.ExcelSheet<Item> itemSheet,
        IReadOnlyList<PhantomRewardWeapon> rewards)
    {
        var itemsByName = itemSheet
            .Where(item => item.RowId > 0)
            .GroupBy(item => item.Name.ExtractText(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var lookup = new Dictionary<string, IReadOnlyList<Item>>(StringComparer.Ordinal);
        foreach (var reward in rewards)
        {
            var matchedItems = reward.ItemNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(itemsByName.ContainsKey)
                .Select(name => itemsByName[name])
                .ToArray();
            if (matchedItems.Length > 0)
            {
                lookup[reward.Key] = matchedItems;
            }
        }

        return lookup;
    }

    private static Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> BuildWeaponItemLookupById(
        Lumina.Excel.ExcelSheet<Item> itemSheet,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<uint>> itemIds)
    {
        var lookup = new Dictionary<(string JobKey, string StageKey), IReadOnlyList<Item>>();
        foreach (var (key, ids) in itemIds)
        {
            var items = ids
                .Where(itemId => itemSheet.TryGetRow(itemId, out _))
                .Select(itemId => itemSheet.GetRow(itemId))
                .Where(item => item.RowId > 0)
                .ToArray();
            if (items.Length > 0)
            {
                lookup[key] = items;
            }
        }

        return lookup;
    }

    private static PhantomWeaponProgressStage? GetHighestSyncedStage(
        string seriesKey,
        PhantomWeaponJob job,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        return stages
            .Where(stage => IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems))
            .LastOrDefault();
    }

    private static bool IsStageOwned(
        string seriesKey,
        PhantomWeaponJob job,
        PhantomWeaponProgressStage stage,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        if (!itemLookup.TryGetValue((job.Key, stage.Key), out var items)
            || items.Count == 0
            || !TryGetSyncedItemIds(seriesKey, job, stage, syncedItems, out var itemIds))
        {
            return false;
        }

        return seriesKey.StartsWith("deep-dungeon-", StringComparison.Ordinal) && job.Key == "pld"
            ? items.All(item => itemIds.Contains(item.RowId))
            : items.Any(item => itemIds.Contains(item.RowId));
    }

    private void DrawWeaponProgressCell(
        string seriesKey,
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
        var owned = IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems);
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
        ImGui.InvisibleButton($"weapon-progress-{seriesKey}-{job.Key}-{stage.Key}", size);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(itemAvailable
                ? $"{job.Name} / {stage.Name}\n{string.Join("\n", items.Select(item => item.Name.ExtractText()))}\n{(owned ? "已持有" : "未持有")}" 
                : $"{job.Name} / {stage.Name}\n未能在物品表匹配到武器");
        }
    }

    private void DrawWeaponCollectionTile(
        string seriesKey,
        PhantomWeaponJob job,
        PhantomWeaponProgressStage? highestStage,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
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
            : GetSyncedStageItems(seriesKey, job, highestStage, itemLookup, syncedItems).ToArray();
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

        DrawWeaponTileProgress(
            cursor + new Vector2(10f, showIcon ? 88f : 52f),
            width - 20f,
            seriesKey,
            job,
            stageKey,
            stages,
            itemLookup,
            syncedItems);

        ImGui.SetCursorScreenPos(cursor + new Vector2(width - 50f, showIcon ? 8f : 7f));
        DrawStagePill(highestStage?.Name ?? "未持有", stageKey);

        ImGui.SetCursorScreenPos(cursor);
        if (ImGui.InvisibleButton($"weapon-tile-{seriesKey}-{job.Key}", size))
        {
            ImGui.OpenPopup($"weapon-detail-{seriesKey}-{job.Key}");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(hasWeapon
                ? $"{job.Name} / {highestStage!.Name}\n{string.Join("\n", ownedItems.Select(item => item.Name.ExtractText()))}\n点击查看全部阶段"
                : $"{job.Name}\n上次同步未找到该系列武器\n点击查看全部阶段");
        }

        DrawWeaponTilePopup(seriesKey, job, stages, itemLookup, syncedItems);
    }

    private void DrawPhantomRewardWeaponTile(
        string seriesKey,
        PhantomRewardWeapon reward,
        IReadOnlyDictionary<string, IReadOnlyList<Item>> rewardLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems,
        float width)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var showIcon = configuration.ShowWeaponProgressIcons;
        var size = new Vector2(width, showIcon ? 104f : 64f);
        var earned = rewardLookup.TryGetValue(reward.Key, out var lookupItems)
            && lookupItems.Any(item => syncedItems.TryGetValue(GetRewardWeaponProgressKey(seriesKey, reward), out var ids)
                && ids.Contains(item.RowId));
        var ownedItemsForDisplay = rewardLookup.TryGetValue(reward.Key, out var matchedItems)
            && syncedItems.TryGetValue(GetRewardWeaponProgressKey(seriesKey, reward), out var syncedIds)
            ? matchedItems.Where(item => syncedIds.Contains(item.RowId)).ToArray()
            : Array.Empty<Item>();
        Item? displayItem = null;
        if (ownedItemsForDisplay.Length > 0)
        {
            displayItem = ownedItemsForDisplay[0];
        }
        else if (rewardLookup.TryGetValue(reward.Key, out var firstMatch) && firstMatch.Count > 0)
        {
            displayItem = firstMatch[0];
        }
        var drawList = ImGui.GetWindowDrawList();
        var bgTop = earned ? new Vector4(0.10f, 0.22f, 0.26f, 0.96f) : new Vector4(0.10f, 0.11f, 0.15f, 0.72f);
        var bgBottom = earned ? new Vector4(0.07f, 0.12f, 0.16f, 0.96f) : new Vector4(0.07f, 0.08f, 0.11f, 0.72f);
        var border = earned ? new Vector4(0.30f, 0.84f, 0.78f, 0.92f) : new Vector4(0.25f, 0.27f, 0.34f, 0.9f);

        drawList.AddRectFilledMultiColor(cursor, cursor + size,
            ImGui.GetColorU32(bgTop), ImGui.GetColorU32(bgTop), ImGui.GetColorU32(bgBottom), ImGui.GetColorU32(bgBottom));
        drawList.AddRect(cursor, cursor + size, ImGui.GetColorU32(border), 12f, ImDrawFlags.None, earned ? 1.8f : 1f);

        if (showIcon && displayItem.HasValue)
        {
            ImGui.SetCursorScreenPos(cursor + new Vector2(10f, 10f));
            var texture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(displayItem.Value.Icon)).GetWrapOrEmpty();
            ImGui.Image(texture.Handle, new Vector2(40f, 40f));
        }

        ImGui.SetCursorScreenPos(cursor + new Vector2(10f, showIcon ? 57f : 31f));
        ImGui.TextUnformatted(displayItem.HasValue ? displayItem.Value.Name.ExtractText() : reward.ItemNames.FirstOrDefault() ?? reward.JobName);
        ImGui.SetCursorScreenPos(cursor + new Vector2(10f, showIcon ? 73f : 47f));
        ImGui.TextColored(new Vector4(0.58f, 0.62f, 0.68f, 1f), reward.JobName);
        ImGui.SetCursorScreenPos(cursor + new Vector2(width - 50f, showIcon ? 8f : 7f));
        DrawStagePill(earned ? reward.BonusLabel : "未持有", earned ? "manderville-complete" : string.Empty);

        ImGui.SetCursorScreenPos(cursor);
        ImGui.InvisibleButton($"reward-tile-{seriesKey}-{reward.Key}", size);

        if (ImGui.IsItemHovered())
        {
            var names = rewardLookup.TryGetValue(reward.Key, out var rewardItems)
                ? string.Join("\n", rewardItems.Select(item => item.Name.ExtractText()))
                : "未能在物品表匹配到武器";
            ImGui.SetTooltip($"{reward.JobName} / {reward.BonusLabel}\n{names}\n{(earned ? "已获得" : "未获得")}");
        }
    }

    private static void DrawWeaponTileProgress(
        Vector2 pos,
        float width,
        string seriesKey,
        PhantomWeaponJob job,
        string stageKey,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        var stageIndex = stages
            .Select((stage, index) => (stage.Key, index))
            .FirstOrDefault(pair => pair.Key == stageKey).index;
        var filled = string.IsNullOrEmpty(stageKey) ? 0 : stageIndex + 1;
        var drawList = ImGui.GetWindowDrawList();
        var gap = 3f;
        var segmentWidth = (width - gap * (stages.Count - 1)) / stages.Count;
        for (var i = 0; i < stages.Count; i++)
        {
            var start = pos + new Vector2(i * (segmentWidth + gap), 0f);
            var end = start + new Vector2(segmentWidth, 6f);
            var isFilled = seriesKey == "ultimate" || seriesKey.StartsWith("deep-dungeon-", StringComparison.Ordinal)
                ? IsStageOwned(seriesKey, job, stages[i], itemLookup, syncedItems)
                : i < filled;
            var color = isFilled
                ? GetStageColor(stages[i].Key, 0.92f)
                : GetStageColor(stages[i].Key, 0.24f);
            drawList.AddRectFilled(start, end, ImGui.GetColorU32(color), 2f);
        }
    }

    private void DrawWeaponTilePopup(
        string seriesKey,
        PhantomWeaponJob job,
        IReadOnlyList<PhantomWeaponProgressStage> stages,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        if (!ImGui.BeginPopup($"weapon-detail-{seriesKey}-{job.Key}"))
        {
            return;
        }

        ImGui.TextUnformatted(job.Name);
        ImGui.Separator();
        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            var ownedItems = GetSyncedStageItems(seriesKey, job, stage, itemLookup, syncedItems).ToArray();
            var stageNames = job.StageItemNames[i];
            var hasStage = IsStageOwned(seriesKey, job, stage, itemLookup, syncedItems);
            ImGui.TextColored(hasStage ? GetStageColor(stage.Key, 1f) : new Vector4(0.48f, 0.50f, 0.57f, 1f), stage.Name);
            ImGui.SameLine(64f);
            if (hasStage)
            {
                ImGui.TextUnformatted(string.Join(" + ", ownedItems.Select(item => item.Name.ExtractText())));
            }
            else
            {
                var partialNames = ownedItems.Select(item => $"{item.Name.ExtractText()}（已有）").ToArray();
                var expectedNames = itemLookup.TryGetValue((job.Key, stage.Key), out var expectedItems)
                    ? expectedItems.Select(item => item.Name.ExtractText()).ToArray()
                    : stageNames;
                ImGui.TextColored(
                    new Vector4(0.52f, 0.54f, 0.60f, 1f),
                    partialNames.Length > 0
                        ? $"{string.Join(" + ", partialNames)} + 缺少其余部件"
                        : string.Join(" + ", expectedNames));
            }
        }

        ImGui.EndPopup();
    }

    private static IEnumerable<Item> GetSyncedStageItems(
        string seriesKey,
        PhantomWeaponJob job,
        PhantomWeaponProgressStage stage,
        IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<Item>> itemLookup,
        IReadOnlyDictionary<string, List<uint>> syncedItems)
    {
        if (!itemLookup.TryGetValue((job.Key, stage.Key), out var items)
            || !TryGetSyncedItemIds(seriesKey, job, stage, syncedItems, out var itemIds))
        {
            return Array.Empty<Item>();
        }

        return items.Where(item => itemIds.Contains(item.RowId));
    }

    private static bool TryGetSyncedItemIds(
        string seriesKey,
        PhantomWeaponJob job,
        PhantomWeaponProgressStage stage,
        IReadOnlyDictionary<string, List<uint>> syncedItems,
        out List<uint> itemIds)
    {
        if (syncedItems.TryGetValue(GetWeaponProgressKey(seriesKey, job, stage), out itemIds!))
        {
            return true;
        }

        return seriesKey == "phantom" && syncedItems.TryGetValue($"{job.Key}:{stage.Key}", out itemIds!);
    }

    private static Vector4 GetStageColor(string stageKey, float alpha)
    {
        var color = stageKey switch
        {
            "secret" => new Vector4(0.30f, 0.84f, 0.78f, alpha),
            "manderville-complete" => new Vector4(0.30f, 0.84f, 0.78f, alpha),
            "manderville-majestic" => new Vector4(0.67f, 0.56f, 0.94f, alpha),
            "manderville-amazing" => new Vector4(0.45f, 0.58f, 0.88f, alpha),
            "manderville-base" => new Vector4(0.72f, 0.72f, 0.78f, alpha),
            "cosmic-stellar" => new Vector4(0.30f, 0.84f, 0.78f, alpha),
            "cosmic-hyperspatial" => new Vector4(0.67f, 0.56f, 0.94f, alpha),
            "cosmic-spacious" => new Vector4(0.45f, 0.58f, 0.88f, alpha),
            "cosmic-base" => new Vector4(0.72f, 0.72f, 0.78f, alpha),
            "eclipse" => new Vector4(0.67f, 0.56f, 0.94f, alpha),
            "darkness" => new Vector4(0.54f, 0.48f, 0.82f, alpha),
            "umbra" => new Vector4(0.45f, 0.58f, 0.88f, alpha),
            "penumbra" => new Vector4(0.72f, 0.72f, 0.78f, alpha),
            _ => GetFallbackStageColor(stageKey, alpha),
        };
        return color;
    }

    private static string GetWeaponProgressKey(string seriesKey, PhantomWeaponJob job, PhantomWeaponProgressStage stage)
        => $"{seriesKey}:{job.Key}:{stage.Key}";

    private static string GetRewardWeaponProgressKey(string seriesKey, PhantomRewardWeapon reward)
        => $"{seriesKey}:reward:{reward.Key}";

    private static void DrawStagePill(string text, string stageKey)
    {
        var color = stageKey switch
        {
            "secret" => new Vector4(0.30f, 0.84f, 0.78f, 1f),
            "manderville-complete" => new Vector4(0.30f, 0.84f, 0.78f, 1f),
            "manderville-majestic" => new Vector4(0.76f, 0.70f, 0.95f, 1f),
            "manderville-amazing" => new Vector4(0.56f, 0.66f, 0.90f, 1f),
            "manderville-base" => new Vector4(0.72f, 0.72f, 0.78f, 1f),
            "cosmic-stellar" => new Vector4(0.30f, 0.84f, 0.78f, 1f),
            "cosmic-hyperspatial" => new Vector4(0.76f, 0.70f, 0.95f, 1f),
            "cosmic-spacious" => new Vector4(0.56f, 0.66f, 0.90f, 1f),
            "cosmic-base" => new Vector4(0.72f, 0.72f, 0.78f, 1f),
            "eclipse" => new Vector4(0.76f, 0.70f, 0.95f, 1f),
            "darkness" => new Vector4(0.70f, 0.58f, 0.90f, 1f),
            "umbra" => new Vector4(0.56f, 0.66f, 0.90f, 1f),
            "penumbra" => new Vector4(0.72f, 0.72f, 0.78f, 1f),
            _ => GetFallbackStageColor(stageKey, 1f),
        };

        ImGui.TextColored(color, text);
    }

    private static Vector4 GetFallbackStageColor(string stageKey, float alpha)
    {
        if (string.IsNullOrWhiteSpace(stageKey))
        {
            return new Vector4(0.48f, 0.48f, 0.52f, alpha);
        }

        var hash = 0;
        foreach (var ch in stageKey)
        {
            hash = unchecked(hash * 31 + ch);
        }

        var palette = new[]
        {
            new Vector3(0.30f, 0.84f, 0.78f),
            new Vector3(0.67f, 0.56f, 0.94f),
            new Vector3(0.45f, 0.58f, 0.88f),
            new Vector3(0.90f, 0.66f, 0.35f),
            new Vector3(0.82f, 0.46f, 0.66f),
            new Vector3(0.48f, 0.74f, 0.44f),
        };
        var color = palette[Math.Abs(hash) % palette.Length];
        return new Vector4(color.X, color.Y, color.Z, alpha);
    }

    private void DrawYokaiWorkspace()
    {
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

            if (configuration.DebugLogSyncedItemLocations)
            {
                LogSyncedYokaiItemLocations(yokaiResults, configuration.DebugLogMissingItemLocations);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("扫描当前角色已加载的背包、关键道具、装备栏和完整兵装库，统计妖怪手表联动奖励。已同步结果按角色 ContentId 保存。");
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

    private static void LogSyncedYokaiItemLocations(IEnumerable<YokaiRewardProgress> results, bool logMissingItems)
    {
        var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
        var loggedItemIds = new HashSet<uint>();
        foreach (var reward in results)
        {
            if (reward.Category != YokaiWatchGuide.WeaponCategory)
            {
                var status = reward.Owned ? "已解锁" : "未解锁";
                PrintChat($"DEBUG [妖表] {reward.Name}：{reward.Category}，{status}");
                continue;
            }

            foreach (var itemId in reward.MatchedItemIds)
            {
                if (!loggedItemIds.Add(itemId) || !itemSheet.TryGetRow(itemId, out var item))
                {
                    continue;
                }

                var locations = GetItemStorageLocations(item);
                if (locations.Count == 0 && !logMissingItems)
                {
                    continue;
                }

                PrintChat($"DEBUG [妖表] {item.Name.ExtractText()}：{(locations.Count > 0 ? string.Join("、", locations) : "未找到")}");
            }
        }
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
        var minionName = reward.Category == YokaiWatchGuide.WeaponCategory
            ? YokaiWatchGuide.GetWeaponMinionName(reward.Key)
            : null;
        var tooltip = $"{reward.Name}\n{(reward.Owned ? "已获得" : "未获得")}";
        if (minionName != null)
        {
            tooltip += $"\n对应宠物：{minionName}";
        }

        var acquisition = reward.Category == YokaiWatchGuide.WeaponCategory
            ? YokaiWatchGuide.GetWeaponAcquisition(reward.Key)
            : null;
        if (acquisition != null)
        {
            tooltip += $"\n职业：{acquisition.JobName}";
            tooltip += $"\n所需徽章：{acquisition.BadgeName}";
            tooltip += $"\n获取地区：{string.Join("、", acquisition.Territories)}";
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
            if (reward.Category == YokaiWatchGuide.WeaponCategory && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(tooltip);
                PrintChat($"[右键] [妖表] 武器信息已复制。\n{tooltip}");
            }
        }
    }

    private void DrawSettingsWorkspace()
    {
        DrawDependencyStatus();
        DrawSettingsSectionHeader("常用设置", "即时生效");
        if (ImGui.BeginTable("settings-common-grid", 2, ImGuiTableFlags.SizingStretchSame))
        {
            DrawSettingCard("悬浮窗", "显示或隐藏悬浮目标窗", configuration.ShowFloatingObjectiveWindow, "floating-window", value => configuration.ShowFloatingObjectiveWindow = value);
            DrawSettingCard("自动隐藏已完成项目", "减少悬浮窗中的已完成条目", configuration.AutoHideCompletedFloatingItems, "auto-hide", value => configuration.AutoHideCompletedFloatingItems = value);
            DrawSettingCard("古武监控", "在悬浮窗显示当前角色古武阶段进度", configuration.ShowZodiacMonitorInFloatingWindow, "zodiac-monitor", value => configuration.ShowZodiacMonitorInFloatingWindow = value);
            ImGui.EndTable();
        }

        DrawSettingsSectionHeader("导航设置", "即时生效");
        if (ImGui.BeginTable("settings-navigation-grid", 2, ImGuiTableFlags.SizingStretchSame))
        {
            DrawSettingCard("飞行导航", "允许导航过程自动使用飞行", configuration.UseFlightNavigation, "flight", value => configuration.UseFlightNavigation = value);
            DrawSettingCard("导航日志", "在聊天栏显示导航过程与状态", configuration.ShowNavigationLogs, "navigation-logs", value => configuration.ShowNavigationLogs = value);
            DrawSettingCard("导航时设置 Flag", "所有具有明确坐标的导航会同步标记目的地", configuration.SetFlagOnNavigation, "navigation-flag", value => configuration.SetFlagOnNavigation = value);
            ImGui.EndTable();
        }

        DrawSettingsSectionHeader("前往 Flag", "二选一");
        if (ImGui.BeginChild("settings-flag-panel", new Vector2(0f, 92f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.TextDisabled("选择点击 Flag 时的前往方式。直接前往适合快速抵达目标，按导航前往会交给 vnavmesh 计算路线。");
            ImGui.Spacing();
            var navigateToFlagDirectly = configuration.NavigateToFlagDirectly;
            if (ImGui.RadioButton("直接前往##flag-direct", navigateToFlagDirectly))
            {
                configuration.NavigateToFlagDirectly = true;
                configuration.Save();
            }

            ImGui.SameLine(0f, 32f);
            if (ImGui.RadioButton("按导航前往##flag-navigation", !navigateToFlagDirectly))
            {
                configuration.NavigateToFlagDirectly = false;
                configuration.Save();
            }

        }
        ImGui.EndChild();

        ImGui.Spacing();
        DrawSettingsSectionHeader("同步", "按当前角色保存");
        if (ImGui.BeginChild("settings-sync-panel", new Vector2(0f, 188f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var debugLogSyncedItemLocations = configuration.DebugLogSyncedItemLocations;
            if (DrawSettingToggle("同步时输出物品位置", "输出背包、收藏柜、投影台和雇员缓存中的匹配位置", ref debugLogSyncedItemLocations, "debug-log-synced-item-locations"))
            {
                configuration.DebugLogSyncedItemLocations = debugLogSyncedItemLocations;
                configuration.Save();
            }

            if (configuration.DebugLogSyncedItemLocations)
            {
                ImGui.SameLine(0f, 20f);
                var debugLogMissingItemLocations = configuration.DebugLogMissingItemLocations;
                if (ImGui.Checkbox("输出未找到物品##debug-log-missing-item-locations", ref debugLogMissingItemLocations))
                {
                    configuration.DebugLogMissingItemLocations = debugLogMissingItemLocations;
                    configuration.Save();
                }
            }

            var itemFinderText = GetItemFinderDebugText();
            ImGui.TextDisabled(itemFinderText);
            DrawDebugButtons(itemFinderText);
        }
        ImGui.EndChild();

        DrawBackpackOrganizer();
    }

    private void DrawHuntAssistantWorkspace()
    {
        DrawSettingsSectionHeader("狩猎助手", "跟随车头 Flag");
        ImGui.TextDisabled("指定车头在当前地图发送 Flag 后，自动贴地并抬升至指定高度飞行导航。");
        ImGui.Spacing();

        var huntAssistantEnabled = configuration.HuntAssistantEnabled;
        if (ImGui.Checkbox("启用狩猎助手##hunt-assistant", ref huntAssistantEnabled))
        {
            configuration.HuntAssistantEnabled = huntAssistantEnabled;
            configuration.Save();
        }

        ImGui.SameLine();
        var showHuntAssistant = configuration.ShowHuntAssistantInFloatingWindow;
        if (ImGui.Checkbox("在悬浮窗显示##hunt-floating", ref showHuntAssistant))
        {
            configuration.ShowHuntAssistantInFloatingWindow = showHuntAssistant;
            configuration.Save();
        }

        ImGui.SameLine();
        var echoLeaderMessages = configuration.HuntAssistantEchoLeaderMessages;
        if (ImGui.Checkbox("测试##hunt-echo-leader", ref echoLeaderMessages))
        {
            configuration.HuntAssistantEchoLeaderMessages = echoLeaderMessages;
            configuration.Save();
        }
        ImGui.TextDisabled("以默语输出，用于确认车头名称匹配和聊天监听是否正常。\n测试开关不需要启用自动导航。");

        var configuredLeaderName = configuration.HuntLeaderName;
        ImGui.SetNextItemWidth(300f);
        if (ImGui.InputText("车头名称##hunt-leader-name", ref configuredLeaderName, 128))
        {
            configuration.HuntLeaderName = configuredLeaderName.Trim();
            configuration.Save();
        }
        ImGui.TextDisabled("可直接粘贴完整角色名。车头名称需与聊天消息发送者一致。");

        var leaderName = DalamudApi.TargetManager.Target?.Name.TextValue ?? string.Empty;
        if (ImGui.Button("使用当前目标##hunt-leader") && !string.IsNullOrWhiteSpace(leaderName))
        {
            configuration.HuntLeaderName = leaderName;
            configuration.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("当前目标：" + (string.IsNullOrWhiteSpace(leaderName) ? "无" : leaderName));

        ImGui.SameLine();
        var localPlayerName = DalamudApi.ObjectTable.LocalPlayer?.Name.TextValue ?? string.Empty;
        if (ImGui.Button("设为自己##hunt-leader") && !string.IsNullOrWhiteSpace(localPlayerName))
        {
            configuration.HuntLeaderName = localPlayerName;
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("清除##hunt-leader"))
        {
            configuration.HuntLeaderName = string.Empty;
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("停止导航##hunt-stop-navigation"))
        {
            vnav.Stop();
        }

        var height = configuration.HuntTargetHeight;
        ImGui.SetNextItemWidth(240f);
        if (ImGui.SliderFloat("接地距离##hunt-target-height", ref height, 0f, 200f, "%.0f yalms"))
        {
            configuration.HuntTargetHeight = height;
            configuration.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("目标贴地后自动上抬");
    }

    private void DrawFateAssistantWorkspace()
    {
        DrawSettingsSectionHeader("危命助手", "当前地图");
        var showAvailableFates = configuration.ShowAvailableFatesInFloatingWindow;
        if (ImGui.Checkbox("在悬浮窗显示可参与 FATE##fate-floating", ref showAvailableFates))
        {
            configuration.ShowAvailableFatesInFloatingWindow = showAvailableFates;
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("停止导航##fate-stop-navigation"))
        {
            vnav.Stop();
        }

        var fates = GetAvailableFates();
        if (fates.Length == 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("当前地图没有可参与的 FATE。 ");
            return;
        }

        ImGui.Spacing();
        ImGui.TextDisabled($"当前可参与 {fates.Length} 个 FATE");
        foreach (var fate in fates)
        {
            ImGui.TextUnformatted($"{GetFateDisplayName(fate)}  {FormatFateState(fate.State)} {fate.Progress}% {FormatFateTime(fate.State, fate.TimeRemaining)}");
            ImGui.SameLine();
            if (ImGui.SmallButton($"导航##fate-assistant-nav-{fate.FateId}"))
            {
                NavigateToFate(fate);
            }
        }
    }

    private static void DrawSettingsSectionHeader(string title, string note)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.45f, 0.86f, 0.82f, 1f), title);
        ImGui.SameLine();
        ImGui.TextDisabled(note);
        ImGui.Separator();
    }

    private void DrawSettingCard(string label, string description, bool value, string id, Action<bool> setValue)
    {
        ImGui.TableNextColumn();
        if (ImGui.BeginChild($"settings-card-{id}", new Vector2(0f, 72f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var changedValue = value;
            var changed = DrawSettingToggle(label, description, ref changedValue, id);
            if (changed)
            {
                setValue(changedValue);
                configuration.Save();
            }

        }
        ImGui.EndChild();
    }

    private static bool DrawSettingToggle(string label, string description, ref bool value, string id)
    {
        var changed = ImGui.Checkbox($"##settings-toggle-{id}", ref value);
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextUnformatted(label);
        ImGui.TextDisabled(description);
        ImGui.EndGroup();
        return changed;
    }

    private void DrawDebugButtons(string itemFinderText)
    {
        if (ImGui.Button("读取道具检索##debug-item-finder"))
        {
            PrintChat($"DEBUG: {itemFinderText}");
        }

        ImGui.SameLine();
        if (ImGui.Button("导出幻武 Item.RowId##debug-export-phantom-item-ids")) ExportPhantomWeaponItemIds();
        ImGui.SameLine();
        if (ImGui.Button("读取雅武 Item.RowId##debug-export-elegant-item-ids")) ExportElegantWeaponItemIds();

        if (ImGui.Button("读取当前坐标##debug-print-coords"))
        {
            var player = DalamudApi.ObjectTable[0];
            var terr = DalamudApi.ClientState.TerritoryType;
            PrintChat(player == null
                ? $"DEBUG: TerritoryType={terr}, (no local player)"
                : $"DEBUG: TerritoryType={terr}, Position=({player.Position.X:0.##}, {player.Position.Y:0.##}, {player.Position.Z:0.##})");
        }

        ImGui.SameLine();
        if (ImGui.Button("测试坐标换算##debug-test-convert"))
        {
            var terr = DalamudApi.ClientState.TerritoryType;
            var player = DalamudApi.ObjectTable[0];
            var territories = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>();
            if (player != null && territories.TryGetRow(terr, out var territory))
            {
                try
                {
                    var map = territory.Map.Value;
                    var s = map.SizeFactor;
                    var ox = map.OffsetX;
                    var oy = map.OffsetY;
                    var fwdX = 0.02f * ox + 2048f / s + 0.02f * player.Position.X + 1f;
                    var fwdZ = 0.02f * oy + 2048f / s + 0.02f * player.Position.Z + 1f;
                    PrintChat($"DEBUG: 当前位置→地图显示 ≈ ({fwdX:0.##}, {fwdZ:0.##})");
                    PrintChat($"DEBUG: 若地图坐标(20.7, 14.3)→世界 ≈ ({50f * 20.7f - ox - 102400f / s - 50f:0.##}, {50f * 14.3f - oy - 102400f / s - 50f:0.##})");
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
                catch (Exception ex) { PrintChat($"DEBUG: TerritoryType={terr}, Failed to resolve map: {ex.Message}"); }
            }
            else PrintChat($"DEBUG: TerritoryType={terr}, Territory not found in sheet.");
        }

        if (ImGui.Button("打开 Wiki##debug-open-wiki")) OpenUrl("https://ff14.huijiwiki.com/wiki/%E5%B9%BB%E5%A2%83%E6%AD%A6%E5%99%A8");
        ImGui.SameLine();
        if (ImGui.Button("读取战斗记忆（未完成）##debug-read-memory-ui")) PrintChat("未完成功能：后续可通过读取战斗记忆界面或任务状态同步进度。");
    }

    private static void ExportPhantomWeaponItemIds()
    {
        var itemsByName = DalamudApi.DataManager.GetExcelSheet<Item>()
            .Where(item => item.RowId > 0)
            .GroupBy(item => item.Name.ExtractText(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var lines = new List<string> { "# JobKey|StageKey|Item.RowId|ItemName" };
        var found = 0;
        var missing = 0;

        for (var stageIndex = 0; stageIndex < PhantomWeaponGuide.ProgressStages.Count; stageIndex++)
        {
            var stage = PhantomWeaponGuide.ProgressStages[stageIndex];
            foreach (var job in PhantomWeaponGuide.WeaponJobs)
            {
                foreach (var name in job.StageItemNames[stageIndex].Where(name => !string.IsNullOrWhiteSpace(name)))
                {
                    if (itemsByName.TryGetValue(name, out var item))
                    {
                        found++;
                        lines.Add($"{job.Key}|{stage.Key}|{item.RowId}|{name}");
                    }
                    else
                    {
                        missing++;
                        lines.Add($"{job.Key}|{stage.Key}|MISSING|{name}");
                    }
                }
            }
        }

        try
        {
            var path = Path.Combine(DalamudApi.PluginInterface.GetPluginConfigDirectory(), "phantom-item-ids.txt");
            File.WriteAllLines(path, lines);
            PrintChat($"已导出幻武 Item.RowId：匹配 {found}，未匹配 {missing}。文件：{path}");
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Error(ex, "Failed to export Phantom item IDs.");
            PrintChat($"导出幻武 Item.RowId 失败：{ex.Message}");
        }
    }

    private void ExportAllDeepDungeonWeaponItemIds()
    {
        var groups = new[]
        {
            (Series: "deep-dungeon-palace", Stage: "palace-padjali", Start: 15181u, End: 15194u, Extra: new[] { 20456u, 20457u, 27347u, 27348u, 35756u, 35774u, 43633u, 43654u }),
            (Series: "deep-dungeon-palace", Stage: "palace-kinna", Start: 16152u, End: 16165u, Extra: new[] { 20458u, 20459u, 27349u, 27350u, 35757u, 35775u, 43634u, 43655u }),
            (Series: "deep-dungeon-heaven-on-high", Stage: "heaven-on-high-empyrean", Start: 22977u, End: 22992u, Extra: new[] { 27379u, 27380u, 35759u, 35777u, 43635u, 43656u }),
            (Series: "deep-dungeon-eureka-orthos", Stage: "eureka-orthos-orthos", Start: 39184u, End: 39203u, Extra: new[] { 43636u, 43657u }),
            (Series: "deep-dungeon-eureka-orthos", Stage: "eureka-orthos-enaretos", Start: 39204u, End: 39223u, Extra: new[] { 43637u, 43658u }),
            (Series: "deep-dungeon-pilgrims-traverse", Stage: "pilgrims-traverse-illuminated", Start: 47028u, End: 47049u, Extra: Array.Empty<uint>()),
            (Series: "deep-dungeon-pilgrims-traverse", Stage: "pilgrims-traverse-ceremonial", Start: 47050u, End: 47071u, Extra: Array.Empty<uint>()),
        };

        try
        {
            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
            var lines = new List<string> { "# SeriesKey|JobKey|StageKey|Item.RowId|ItemName" };
            var unmapped = new List<string>();
            foreach (var group in groups)
            {
                var itemIds = Enumerable.Range((int)group.Start, (int)(group.End - group.Start + 1))
                    .Select(value => (uint)value)
                    .Concat(group.Extra);
                foreach (var itemId in itemIds)
                {
                    if (!itemSheet.TryGetRow(itemId, out var item))
                    {
                        unmapped.Add($"缺少物品表行 {itemId}");
                        continue;
                    }

                    var jobKey = GetDeepDungeonItemJobKey(item);
                    if (jobKey == null)
                    {
                        unmapped.Add($"无法识别职业 {itemId} {item.Name.ExtractText()}");
                        continue;
                    }

                    lines.Add($"{group.Series}|{jobKey}|{group.Stage}|{itemId}|{item.Name.ExtractText()}");
                }
            }

            var path = Path.Combine(DalamudApi.PluginInterface.GetPluginConfigDirectory(), "deep-dungeon-weapon-item-ids.txt");
            File.WriteAllLines(path, lines);
            deepDungeonWeaponItemLookups.Clear();
            PrintChat($"已导出全部深武 Item.RowId：映射 {lines.Count - 1} 条，待确认 {unmapped.Count} 条。文件：{path}");
            foreach (var message in unmapped)
            {
                PrintChat($"DEBUG [深武] {message}");
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Error(ex, "Failed to export deep dungeon weapon item IDs.");
            PrintChat($"导出全部深武 Item.RowId 失败：{ex.Message}");
        }
    }

    private static string? GetDeepDungeonItemJobKey(Item item)
    {
        var jobs = new[]
        {
            (Key: "pld", Property: "PLD"), (Key: "mnk", Property: "MNK"), (Key: "war", Property: "WAR"),
            (Key: "drg", Property: "DRG"), (Key: "brd", Property: "BRD"), (Key: "whm", Property: "WHM"),
            (Key: "blm", Property: "BLM"), (Key: "smn", Property: "SMN"), (Key: "sch", Property: "SCH"),
            (Key: "nin", Property: "NIN"), (Key: "mch", Property: "MCH"), (Key: "drk", Property: "DRK"),
            (Key: "ast", Property: "AST"), (Key: "sam", Property: "SAM"), (Key: "rdm", Property: "RDM"),
            (Key: "gnb", Property: "GNB"), (Key: "dnc", Property: "DNC"), (Key: "rpr", Property: "RPR"),
            (Key: "sge", Property: "SGE"), (Key: "vpr", Property: "VPR"), (Key: "pct", Property: "PCT"),
        };
        var category = item.ClassJobCategory.Value;
        var categoryType = category.GetType();
        return jobs.FirstOrDefault(job => categoryType.GetProperty(job.Property)?.GetValue(category) is true).Key;
    }

    private static void ExportElegantWeaponItemIds()
    {
        var itemsByName = DalamudApi.DataManager.GetExcelSheet<Item>()
            .Where(item => item.RowId > 0)
            .GroupBy(item => item.Name.ExtractText(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var lines = new List<string> { "# SeriesKey|JobKey|StageKey|Item.RowId|ItemName" };
        var found = 0;
        var missing = 0;

        for (var stageIndex = 0; stageIndex < RelicWeaponGuide.ElegantProgressStages.Count; stageIndex++)
        {
            var stage = RelicWeaponGuide.ElegantProgressStages[stageIndex];
            foreach (var job in RelicWeaponGuide.ElegantWeaponJobs)
            {
                foreach (var name in job.StageItemNames[stageIndex].Where(name => !string.IsNullOrWhiteSpace(name)))
                {
                    if (itemsByName.TryGetValue(name, out var item))
                    {
                        found++;
                        lines.Add($"elegant|{job.Key}|{stage.Key}|{item.RowId}|{name}");
                    }
                    else
                    {
                        missing++;
                        lines.Add($"elegant|{job.Key}|{stage.Key}|MISSING|{name}");
                    }
                }
            }
        }

        try
        {
            var path = Path.Combine(DalamudApi.PluginInterface.GetPluginConfigDirectory(), "elegant-weapon-item-ids.txt");
            File.WriteAllLines(path, lines);
            PrintChat($"已读取雅武 Item.RowId：匹配 {found}，未匹配 {missing}。文件：{path}");
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Error(ex, "Failed to export Elegant weapon item IDs.");
            PrintChat($"读取雅武 Item.RowId 失败：{ex.Message}");
        }
    }

    private unsafe void DrawBackpackOrganizer()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.58f, 0.86f, 0.90f, 1f), "整理背包");
        ImGui.TextDisabled("按 itemid 选择物品，优先合并到鞍囊已有的同物品、同品质未满堆。");

        if (ImGui.Button("选择物品##backpack-organizer-select"))
        {
            backpackOrganizeItems = ReadBackpackItemSummaries();
            backpackOrganizeSearch = string.Empty;
            ImGui.OpenPopup("backpack-organizer-items");
        }

        ImGui.SameLine();
        var organizerWasRunning = backpackOrganizerRunning;
        if (organizerWasRunning)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button(organizerWasRunning ? "整理中...##backpack-organizer-run" : "整理背包##backpack-organizer-run"))
        {
            OrganizeBackpack();
        }
        if (organizerWasRunning)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"已选择 {configuration.BackpackOrganizeItemIds.Count} 种");

        ImGui.SetNextWindowSize(new Vector2(480f, 420f), ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(new Vector2(360f, 260f), new Vector2(560f, 520f));
        if (!ImGui.BeginPopup("backpack-organizer-items"))
        {
            return;
        }

        ImGui.SetNextItemWidth(300f);
        ImGui.InputText("搜索名称##backpack-organizer-search", ref backpackOrganizeSearch, 128);
        ImGui.Separator();
        if (ImGui.BeginChild("backpack-organizer-item-list", new Vector2(0f, 0f), false))
        {
            foreach (var item in backpackOrganizeItems
                .OrderByDescending(item => configuration.BackpackOrganizeItemIds.Contains(item.ItemId))
                .ThenBy(item => item.Name, StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(backpackOrganizeSearch)
                    && !item.Name.Contains(backpackOrganizeSearch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var selected = configuration.BackpackOrganizeItemIds.Contains(item.ItemId);
                if (ImGui.Checkbox($"{item.Name} ({item.Quantity})##item-{item.ItemId}", ref selected))
                {
                    if (selected)
                    {
                        configuration.BackpackOrganizeItemIds.Add(item.ItemId);
                    }
                    else
                    {
                        configuration.BackpackOrganizeItemIds.Remove(item.ItemId);
                    }

                    configuration.Save();
                }

                ImGui.SameLine();
                ImGui.TextDisabled($"ID: {item.ItemId}");
            }

            if (backpackOrganizeItems.Count == 0)
            {
                ImGui.TextDisabled("当前背包没有物品。");
            }

        }
        ImGui.EndChild();

        ImGui.EndPopup();
    }

    private unsafe List<BackpackItemSummary> ReadBackpackItemSummaries()
    {
        var summaries = new Dictionary<uint, (string Name, int Quantity)>();
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return new List<BackpackItemSummary>();
        }

        var items = DalamudApi.DataManager.GetExcelSheet<Item>();
        foreach (var inventoryType in BackpackOrganizeSources)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null)
            {
                continue;
            }

            for (var index = 0; index < container->Size; index++)
            {
                var slot = container->GetInventorySlot(index);
                var itemId = NormalizeItemId(slot->ItemId);
                if (itemId == 0 || !items.TryGetRow(itemId, out var item))
                {
                    continue;
                }

                var name = item.Name.ExtractText();
                summaries.TryGetValue(itemId, out var summary);
                summaries[itemId] = (name, summary.Quantity + slot->Quantity);
            }
        }

        return summaries
            .OrderBy(entry => entry.Value.Name, StringComparer.Ordinal)
            .Select(entry => new BackpackItemSummary(entry.Key, entry.Value.Name, entry.Value.Quantity))
            .ToList();
    }

    private unsafe void OrganizeBackpack()
    {
        if (configuration.BackpackOrganizeItemIds.Count == 0)
        {
            PrintChat("请先选择要整理的物品。");
            return;
        }

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            PrintChat("无法访问当前背包。");
            return;
        }

        backpackMovedByItem.Clear();
        backpackSkippedItemIds.Clear();
        pendingBackpackMove = null;
        backpackOrganizerRunning = true;
        backpackOrganizerStartedUtc = DateTime.UtcNow;
        backpackOrganizerReadyUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
        backpackOrganizerWaitingForSaddlebagWindow = !IsSaddlebagWindowOpen();
        backpackOrganizerWaitingForSaddlebag = !BackpackOrganizeTargets.Any(targetType => IsInventoryContainerLoaded(inventoryManager, targetType));
        if (backpackOrganizerWaitingForSaddlebagWindow)
        {
            try
            {
                DalamudApi.Commands.ProcessCommand("/陆行鸟鞍囊");
                PrintChat("正在打开陆行鸟鞍囊；加载完成后会自动开始整理。");
            }
            catch (Exception ex)
            {
                FinishBackpackOrganizer($"无法打开陆行鸟鞍囊，整理已停止：{ex.Message}");
                return;
            }
        }
        else
        {
            PrintChat("开始整理背包。请保持陆行鸟鞍囊开启，插件会逐件确认服务器移动结果。");
        }

        ProcessBackpackOrganizer();
    }

    private unsafe void ProcessBackpackOrganizer()
    {
        if (!backpackOrganizerRunning)
        {
            return;
        }

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            FinishBackpackOrganizer("无法访问当前背包，整理已停止。");
            return;
        }

        var saddlebagWindowOpen = IsSaddlebagWindowOpen();
        var saddlebagLoaded = BackpackOrganizeTargets.Any(targetType => IsInventoryContainerLoaded(inventoryManager, targetType));
        if (!saddlebagWindowOpen || !saddlebagLoaded)
        {
            if ((backpackOrganizerWaitingForSaddlebagWindow || backpackOrganizerWaitingForSaddlebag)
                && DateTime.UtcNow - backpackOrganizerStartedUtc < TimeSpan.FromSeconds(10))
            {
                return;
            }

            FinishBackpackOrganizer(backpackOrganizerWaitingForSaddlebagWindow
                ? "等待陆行鸟鞍囊窗口打开超时，整理已停止。"
                : "陆行鸟鞍囊已关闭或未加载，整理已停止。");
            return;
        }

        if (backpackOrganizerWaitingForSaddlebagWindow || backpackOrganizerWaitingForSaddlebag)
        {
            backpackOrganizerWaitingForSaddlebagWindow = false;
            backpackOrganizerWaitingForSaddlebag = false;
            backpackOrganizerReadyUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
            PrintChat("陆行鸟鞍囊已打开并加载，开始逐件整理。请保持鞍囊开启。");
            return;
        }

        if (DateTime.UtcNow < backpackOrganizerReadyUtc)
        {
            return;
        }

        if (pendingBackpackMove is { } pending)
        {
            var source = inventoryManager->GetInventoryContainer(pending.SourceType);
            var target = inventoryManager->GetInventoryContainer(pending.TargetType);
            if (source == null || target == null || !source->IsLoaded || !target->IsLoaded)
            {
                FinishBackpackOrganizer("背包或鞍囊数据已卸载，整理已停止。");
                return;
            }

            var sourceItem = source->GetInventorySlot(pending.SourceSlot);
            var targetItem = target->GetInventorySlot(pending.TargetSlot);
            var sourceQuantity = sourceItem->ItemId == pending.RawItemId ? sourceItem->Quantity : 0;
            var targetQuantity = targetItem->ItemId == pending.RawItemId ? targetItem->Quantity : 0;
            var sourceReduced = Math.Max(0, pending.SourceQuantity - sourceQuantity);
            var targetIncreased = Math.Max(0, targetQuantity - pending.TargetQuantity);
            var confirmedMoved = Math.Min(sourceReduced, targetIncreased);

            if (confirmedMoved > 0)
            {
                var confirmedUtc = pending.ConfirmedUtc ?? DateTime.UtcNow;
                if (DateTime.UtcNow - confirmedUtc >= TimeSpan.FromMilliseconds(250))
                {
                    backpackMovedByItem[pending.ItemId] = backpackMovedByItem.GetValueOrDefault(pending.ItemId) + confirmedMoved;
                    pendingBackpackMove = null;
                }
                else if (!pending.ConfirmedUtc.HasValue)
                {
                    pendingBackpackMove = pending with { ConfirmedUtc = confirmedUtc };
                }

                return;
            }

            if (sourceReduced > 0)
            {
                var sourceChangedUtc = pending.SourceChangedUtc ?? DateTime.UtcNow;
                if (DateTime.UtcNow - sourceChangedUtc >= TimeSpan.FromMilliseconds(750))
                {
                    backpackMovedByItem[pending.ItemId] = backpackMovedByItem.GetValueOrDefault(pending.ItemId) + sourceReduced;
                    pendingBackpackMove = null;
                }
                else if (!pending.SourceChangedUtc.HasValue || pending.SourceReducedQuantity != sourceReduced)
                {
                    pendingBackpackMove = pending with
                    {
                        SourceChangedUtc = DateTime.UtcNow,
                        SourceReducedQuantity = sourceReduced,
                    };
                }

                return;
            }

            if (pending.SourceChangedUtc.HasValue)
            {
                pendingBackpackMove = pending with { SourceChangedUtc = null, SourceReducedQuantity = 0 };
            }

            if (DateTime.UtcNow - pending.StartedUtc >= TimeSpan.FromSeconds(8))
            {
                var name = GetBackpackOrganizerItemName(pending.ItemId);
                FinishBackpackOrganizer($"{name}（ID {pending.ItemId}）移动未获服务器确认，整理已停止。物品仍以服务器库存为准。");
            }

            return;
        }

        foreach (var sourceType in BackpackOrganizeSources)
        {
            var source = inventoryManager->GetInventoryContainer(sourceType);
            if (source == null || !source->IsLoaded)
            {
                continue;
            }

            for (var sourceSlot = 0; sourceSlot < source->Size; sourceSlot++)
            {
                var item = source->GetInventorySlot(sourceSlot);
                var itemId = NormalizeItemId(item->ItemId);
                if (itemId == 0 || !configuration.BackpackOrganizeItemIds.Contains(itemId))
                {
                    continue;
                }

                var stackSize = GetBackpackOrganizerStackSize(itemId);
                for (var targetPass = 0; targetPass < 2; targetPass++)
                {
                    foreach (var targetType in BackpackOrganizeTargets)
                    {
                        var target = inventoryManager->GetInventoryContainer(targetType);
                        if (target == null || !target->IsLoaded)
                        {
                            continue;
                        }

                        for (var targetSlot = 0; targetSlot < target->Size; targetSlot++)
                        {
                            var destination = target->GetInventorySlot(targetSlot);
                            var isPartialStack = destination->ItemId == item->ItemId && destination->Quantity < stackSize;
                            var isEmptySlot = destination->ItemId == 0;
                            if ((targetPass == 0 && !isPartialStack) || (targetPass == 1 && !isEmptySlot))
                            {
                                continue;
                            }

                            var sourceQuantity = item->Quantity;
                            var targetQuantity = destination->Quantity;
                            var result = inventoryManager->MoveItemSlot(
                                sourceType,
                                (ushort)sourceSlot,
                                targetType,
                                (ushort)targetSlot,
                                true);
                            if (result < 0)
                            {
                                var name = GetBackpackOrganizerItemName(itemId);
                                FinishBackpackOrganizer($"{name}（ID {itemId}）移动请求被客户端拒绝，整理已停止。");
                                return;
                            }

                            pendingBackpackMove = new PendingBackpackMove(
                                sourceType,
                                sourceSlot,
                                targetType,
                                targetSlot,
                                itemId,
                                item->ItemId,
                                sourceQuantity,
                                targetQuantity,
                                DateTime.UtcNow);
                            return;
                        }
                    }

                }

                backpackSkippedItemIds.Add(itemId);
            }
        }

        FinishBackpackOrganizer(null);
    }

    private unsafe void FinishBackpackOrganizer(string? error)
    {
        backpackOrganizerRunning = false;
        backpackOrganizerWaitingForSaddlebag = false;
        backpackOrganizerWaitingForSaddlebagWindow = false;
        pendingBackpackMove = null;
        var summaries = ReadBackpackItemSummaries().ToDictionary(item => item.ItemId);
        foreach (var itemId in backpackMovedByItem.Keys.OrderBy(GetBackpackOrganizerItemName, StringComparer.Ordinal))
        {
            backpackMovedByItem.TryGetValue(itemId, out var moved);
            summaries.TryGetValue(itemId, out var left);
            var name = GetBackpackOrganizerItemName(itemId);
            var remaining = string.IsNullOrWhiteSpace(error) ? (left?.Quantity ?? 0).ToString() : "未确认";
            PrintChat($"{name}（ID {itemId}）：已确认转移 {moved}，背包剩余 {remaining}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            PrintChat(error);
        }
        else
        {
            PrintChat($"整理背包完成：已移动 {backpackMovedByItem.Count} 种，跳过 {backpackSkippedItemIds.Count} 种。跳过项在鞍囊中没有可用空间，仍保留在普通背包。");
        }
    }

    private static unsafe bool IsSaddlebagWindowOpen()
    {
        var regular = (AtkUnitBase*)DalamudApi.GameGui.GetAddonByName("InventoryBuddy", 1).Address;
        var premium = (AtkUnitBase*)DalamudApi.GameGui.GetAddonByName("InventoryBuddy2", 1).Address;
        return (regular != null && regular->IsVisible)
            || (premium != null && premium->IsVisible);
    }

    private static unsafe bool IsInventoryContainerLoaded(InventoryManager* inventoryManager, InventoryType inventoryType)
    {
        var container = inventoryManager->GetInventoryContainer(inventoryType);
        return container != null && container->IsLoaded;
    }

    private static int GetBackpackOrganizerStackSize(uint itemId)
    {
        var items = DalamudApi.DataManager.GetExcelSheet<Item>();
        return items.TryGetRow(itemId, out var item) ? (int)Math.Max(1u, item.StackSize) : 1;
    }

    private static string GetBackpackOrganizerItemName(uint itemId)
    {
        var items = DalamudApi.DataManager.GetExcelSheet<Item>();
        return items.TryGetRow(itemId, out var item) && !item.Name.IsEmpty
            ? item.Name.ExtractText()
            : "未知物品";
    }

    private static unsafe string GetItemFinderDebugText()
    {
        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return "道具检索：不可用（ItemFinderModule.Instance() == null）";
        }

        var result = finder->Result;
        if (result == null)
        {
            return "道具检索：无结果（Result == null）";
        }

        var equipped = result->EquipmentSlot >= 0 ? 1 : 0;
        var inventory = result->InventoryPage1Count
            + result->InventoryPage2Count
            + result->InventoryPage3Count
            + result->InventoryPage4Count;
        var armoury = result->ArmouryChestCount;
        var saddle = result->SaddleBagPage1Count
            + result->SaddleBagPage2Count
            + result->PremiumSaddleBagPage1Count
            + result->PremiumSaddleBagPage2Count;
        var storage = result->ArmoireCount + result->GlamourDresserCount;
        var retainer = 0;
        for (var i = 0; i < result->RetainerCount; i++)
        {
            var retainerResult = result->Retainer[i];
            if (retainerResult == null)
            {
                continue;
            }

            retainer += retainerResult->EquipmentSlot >= 0 ? 1 : 0;
            retainer += retainerResult->Page1Count
                + retainerResult->Page2Count
                + retainerResult->Page3Count
                + retainerResult->Page4Count
                + retainerResult->Page5Count;
        }

        var total = equipped + inventory + armoury + saddle + storage + retainer;
        var state = total > 0 ? "有数据" : "无命中";
        return $"道具检索：{state}，总数 {total}（装备 {equipped}，背包 {inventory}，兵装库 {armoury}，雇员 {retainer}/{result->RetainerCount}，鞍囊 {saddle}，投影/收藏 {storage}）";
    }

    private static void DrawWikiButton(string label, string key, string url)
    {
        if (ImGui.Button($"{label}##open-wiki-{key}"))
        {
            OpenUrl(url);
        }
    }

    private static void OpenUrl(string url, string openedMessage = "已打开 Wiki。", string actionName = "打开 Wiki")
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            PrintChat(openedMessage);
        }
        catch (Exception ex)
        {
            PrintChat($"{actionName}失败: {ex.Message}");
        }
    }

    private static void PrintChat(string message)
    {
        try
        {
            if (message.StartsWith("DEBUG ", StringComparison.Ordinal))
            {
                message = $"[DEBUG] {message[6..]}";
            }

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

    private void PrintNavigationLog(string message)
    {
        if (!configuration.ShowNavigationLogs)
        {
            return;
        }

        PrintChat($"[导航日志] {message}");
    }

    private void DrawStage(
        PhantomWeaponStage stage,
        Dictionary<string, int>? progress = null,
        HashSet<string>? completedTasks = null)
    {
        progress ??= configuration.Progress;
        completedTasks ??= configuration.CompletedTasks;

        ImGui.TextUnformatted($"{stage.ItemLevel}  {stage.Quest}");
        ImGui.TextWrapped(stage.Summary);

        ImGui.Spacing();
        DrawTasks(stage, completedTasks);

        ImGui.Spacing();
        if (stage.Key != "secret")
        {
            DrawRequirements(stage, progress);
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

    private void DrawTasks(PhantomWeaponStage stage, HashSet<string> completedTasks)
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
            var done = completedTasks.Contains(task.Key);
            if (ImGui.Checkbox($"[仅一次] {task.Name}##{task.Key}", ref done))
            {
                if (done)
                {
                    completedTasks.Add(task.Key);
                }
                else
                {
                    completedTasks.Remove(task.Key);
                }

                configuration.Save();
            }

            ImGui.TextWrapped(task.Detail);
        }
    }

    private void DrawRequirements(PhantomWeaponStage stage, Dictionary<string, int> progress)
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
                DrawRequirementRow(requirement, progress);
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
            if (!ImGui.CollapsingHeader($"{header}##{group.Key}"))
            {
                continue;
            }

            ImGui.ProgressBar(total == 0 ? 1f : (float)completed / total, new Vector2(-1, 0), $"{completed}/{total}");
            if (ImGui.BeginTable($"secret-duty-table-{group.Key}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("完成", ImGuiTableColumnFlags.WidthFixed, 56);
                ImGui.TableSetupColumn("指定迷宫/讨伐", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 76f);
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
        ImGui.TableNextColumn();
        if (ImGui.SmallButton($"AD执行##secret-duty-ad-{duty.Key}"))
        {
            autoDuty.Run(duty.Name);
        }
    }

    private void DrawRequirementRow(PhantomWeaponRequirement requirement, Dictionary<string, int> progress)
    {
        var isZodiacBookProgress = requirement.Key == "zodiac-animus-books";
        var current = isZodiacBookProgress ? GetCompletedZodiacBookCount() : progress.GetValueOrDefault(requirement.Key);
        current = Math.Clamp(current, 0, requirement.Needed);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextWrapped(requirement.Name);

        ImGui.TableNextColumn();
        if (isZodiacBookProgress)
        {
            ImGui.TextDisabled("由下方文书完成状态自动计算");
        }
        else
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt($"##progress-{requirement.Key}", ref current, 1, Math.Max(10, requirement.Needed / 10)))
            {
                progress[requirement.Key] = Math.Clamp(current, 0, requirement.Needed);
                configuration.Save();
            }
        }

        var fraction = requirement.Needed == 0 ? 1f : Math.Clamp((float)current / requirement.Needed, 0f, 1f);
        ImGui.ProgressBar(fraction, new Vector2(-1, 0), $"{current}/{requirement.Needed}");

        ImGui.TableNextColumn();
        ImGui.TextWrapped($"剩余 {Math.Max(0, requirement.Needed - current)}。{requirement.Source}");
    }

    private int GetCompletedZodiacBookCount()
    {
        var characterKey = GetCurrentCharacterKey();
        return configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            && characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress)
            ? Math.Min(jobProgress.CompletedBooks.Count, ZodiacGuide.AnimusBooks.Count)
            : 0;
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
        ImGui.TextUnformatted("秘影目标");
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
            if (!ImGui.CollapsingHeader($"{doneStr}##secret-zone-{group.Key}"))
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

        ImGui.SetNextWindowSize(new Vector2(300, 360), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(240, 120), new Vector2(float.MaxValue, float.MaxValue));
        var floatingOpen = configuration.ShowFloatingObjectiveWindow;
        if (!ImGui.Begin("肝武助手##floating-secret-targets", ref floatingOpen))
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

        if (configuration.ShowHuntAssistantInFloatingWindow)
        {
            DrawFloatingHuntAssistant();
            ImGui.Separator();
        }

        if (configuration.ShowAvailableFatesInFloatingWindow)
        {
            DrawFloatingAvailableFates();
            ImGui.Separator();
        }

        if (configuration.ShowZodiacMonitorInFloatingWindow)
        {
            DrawFloatingZodiacMonitor();
            if (configuration.ShowSecretTargetsInFloatingWindow || configuration.ShowSecretDutiesInFloatingWindow)
            {
                ImGui.Separator();
            }
        }

        if (configuration.ShowSecretTargetsInFloatingWindow || configuration.ShowSecretDutiesInFloatingWindow)
        {
            DrawFloatingPhantomMonitor(targets, territory);
        }

        DrawFloatingContextMenu();
        ImGui.End();
    }

    private void DrawFloatingZodiacMonitor()
    {
        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            ImGui.TextDisabled("古武监控：未登录角色");
            return;
        }

        var selectedJob = RelicWeaponGuide.ZodiacWeaponJobs.FirstOrDefault(job => job.Key == configuration.FloatingZodiacJobKey)
            ?? RelicWeaponGuide.ZodiacWeaponJobs[0];
        var selectedStage = RelicWeaponGuide.ZodiacProgressStages.FirstOrDefault(stage => stage.Key == configuration.FloatingZodiacStageKey)
            ?? RelicWeaponGuide.ZodiacProgressStages[0];

        if (selectedJob.Key != configuration.FloatingZodiacJobKey)
        {
            configuration.FloatingZodiacJobKey = selectedJob.Key;
        }

        if (selectedStage.Key != configuration.FloatingZodiacStageKey)
        {
            configuration.FloatingZodiacStageKey = selectedStage.Key;
        }

        if (!configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            || !characterProgress.Jobs.TryGetValue(selectedJob.Key, out var progress))
        {
            progress = new ZodiacJobProgress();
        }

        var monitorHeight = floatingZodiacMonitorOpen
            ? selectedStage.Key switch
            {
                "zodiac-animus" => 224f,
                "zodiac-atma" => 190f,
                _ => 174f,
            }
            : 34f;
        if (ImGui.BeginChild("floating-zodiac-monitor", new Vector2(-1f, monitorHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (DrawFloatingMonitorHeaderButton("古武监控", "floating-zodiac-monitor-header", new Vector4(0.35f, 0.88f, 0.82f, 1f)))
            {
                floatingZodiacMonitorOpen = !floatingZodiacMonitorOpen;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("古武监控\n显示当前角色和所选职业的古武阶段进度、下一步目标及可用导航。点击标题可折叠或展开卡片。");
            }

            ImGui.SameLine();
            ImGui.TextDisabled(floatingZodiacMonitorOpen ? "收起" : "展开");
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("停止导航").X - ImGui.GetStyle().FramePadding.X * 2f);
            if (ImGui.SmallButton("停止导航##floating-zodiac-stop-navigation"))
            {
                vnav.Stop();
            }
            if (!floatingZodiacMonitorOpen)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"{selectedJob.Name} · {selectedStage.Name}");

            ImGui.SetNextItemWidth(125f);
            if (ImGui.BeginCombo("##floating-zodiac-job", selectedJob.Name))
            {
                foreach (var job in RelicWeaponGuide.ZodiacWeaponJobs)
                {
                    var active = job.Key == selectedJob.Key;
                    if (ImGui.Selectable($"{job.Name}##floating-zodiac-job-{job.Key}", active))
                    {
                        configuration.FloatingZodiacJobKey = job.Key;
                        configuration.Save();
                    }

                    if (active) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(125f);
            if (ImGui.BeginCombo("##floating-zodiac-stage", selectedStage.Name))
            {
                foreach (var stage in RelicWeaponGuide.ZodiacProgressStages)
                {
                    var active = stage.Key == selectedStage.Key;
                    if (ImGui.Selectable($"{stage.Name}##floating-zodiac-stage-{stage.Key}", active))
                    {
                        configuration.FloatingZodiacStageKey = stage.Key;
                        configuration.Save();
                    }

                    if (active) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            DrawFloatingZodiacStageSummary(selectedStage.Key, progress);
        }

        ImGui.EndChild();
    }

    private static bool DrawFloatingMonitorHeaderButton(string label, string id, Vector4 textColor)
    {
        var width = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2f;
        var hovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
        var active = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0f, 0f, 0f, 0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered with { W = 0.35f });
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active with { W = 0.5f });
        var clicked = ImGui.Button($"{label}##{id}", new Vector2(width, 22f));
        ImGui.PopStyleColor(4);
        return clicked;
    }

    private void DrawFloatingZodiacStageSummary(string stageKey, ZodiacJobProgress progress)
    {
        if (stageKey == "zodiac-atma")
        {
            var completed = ZodiacGuide.AtmaTerritories.Count(objective => progress.CompletedObjectives.Contains(objective.Key));
            ImGui.TextDisabled($"魂晶地区 FATE：{completed}/{ZodiacGuide.AtmaTerritories.Count}");
            ImGui.ProgressBar((float)completed / ZodiacGuide.AtmaTerritories.Count, new Vector2(-1f, 0f), $"{completed}/{ZodiacGuide.AtmaTerritories.Count}");
            var next = ZodiacGuide.AtmaTerritories.FirstOrDefault(objective => !progress.CompletedObjectives.Contains(objective.Key));
            DrawFloatingZodiacNextStep(
                next == null ? "魂晶阶段已完成" : $"前往 {next.Zone}，完成任意 FATE 获取{next.Name}",
                next == null ? null : () => vnav.TeleportToMap(next.Zone));
            return;
        }

        if (stageKey == "zodiac-animus")
        {
            var completed = Math.Min(progress.CompletedBooks.Count, ZodiacGuide.AnimusBooks.Count);
            ImGui.TextDisabled($"黄道十二文书：{completed}/{ZodiacGuide.AnimusBooks.Count}");
            ImGui.ProgressBar((float)completed / ZodiacGuide.AnimusBooks.Count, new Vector2(-1f, 0f), $"{completed}/{ZodiacGuide.AnimusBooks.Count}");
            var selectedBook = ZodiacGuide.AnimusBooks.FirstOrDefault(book => book.Key == progress.SelectedBookKey);
            if (selectedBook != null)
            {
                var targetCount = selectedBook.Monsters.Count + selectedBook.Duties.Count + selectedBook.Fates.Count + selectedBook.Leves.Count;
                var targetCompleted = selectedBook.Monsters.Count(objective => progress.CompletedObjectives.Contains(objective.Key))
                    + selectedBook.Duties.Count(objective => progress.CompletedObjectives.Contains(objective.Key))
                    + selectedBook.Fates.Count(objective => progress.CompletedObjectives.Contains(objective.Key))
                    + selectedBook.Leves.Count(objective => progress.CompletedObjectives.Contains(objective.Key));
                ImGui.TextDisabled($"当前文书：{selectedBook.Name} {targetCompleted}/{targetCount}");
                DrawFloatingZodiacNextBookStep(selectedBook, progress);
            }
            else
            {
                DrawFloatingZodiacNextStep("选择一本未完成的黄道十二文书", null);
            }

            return;
        }

        var stage = RelicWeaponGuide.Series["zodiac"].Stages.FirstOrDefault(candidate => candidate.Key == stageKey);
        if (stage == null)
        {
            return;
        }

        foreach (var requirement in stage.Requirements)
        {
            var current = progress.RequirementProgress.GetValueOrDefault(requirement.Key);
            ImGui.TextDisabled($"{requirement.Name}：{Math.Clamp(current, 0, requirement.Needed)}/{requirement.Needed}");
        }

        var nextTask = stage.Tasks.FirstOrDefault(task => !progress.CompletedObjectives.Contains(task.Key));
        if (nextTask != null)
        {
            DrawFloatingZodiacNextStep(nextTask.Name, null);
            return;
        }

        var nextRequirement = stage.Requirements.FirstOrDefault(requirement =>
            progress.RequirementProgress.GetValueOrDefault(requirement.Key) < requirement.Needed);
        DrawFloatingZodiacNextStep(nextRequirement == null
            ? "当前阶段已完成"
            : $"完成 {nextRequirement.Name}（{Math.Clamp(progress.RequirementProgress.GetValueOrDefault(nextRequirement.Key), 0, nextRequirement.Needed)}/{nextRequirement.Needed}）", null);
    }

    private void DrawFloatingZodiacNextBookStep(ZodiacBookGuide book, ZodiacJobProgress progress)
    {
        var nextMonster = book.Monsters.FirstOrDefault(objective => !progress.CompletedObjectives.Contains(objective.Key));
        if (nextMonster != null)
        {
            var count = progress.RequirementProgress.GetValueOrDefault(nextMonster.Key);
            DrawFloatingZodiacNextStep(
                $"讨伐 {nextMonster.Name}（{Math.Clamp(count, 0, nextMonster.Needed)}/{nextMonster.Needed}）",
                () => NavigateToZodiacMonster(nextMonster));
            return;
        }

        var nextDuty = book.Duties.FirstOrDefault(objective => !progress.CompletedObjectives.Contains(objective.Key));
        if (nextDuty != null)
        {
            DrawFloatingZodiacNextStep($"完成副本：{nextDuty.Name}", () => autoDuty.Run(nextDuty.Name), "AD执行");
            return;
        }

        var nextFate = book.Fates.FirstOrDefault(objective => !progress.CompletedObjectives.Contains(objective.Key));
        if (nextFate != null)
        {
            DrawFloatingZodiacNextStep($"完成 FATE：{nextFate.Name}", () => NavigateToZodiacCoordinate(nextFate.Zone, nextFate.MapX > 0f ? new ZodiacCoordinate(nextFate.MapX, nextFate.MapY) : null));
            return;
        }

        var nextLeve = book.Leves.FirstOrDefault(objective => !progress.CompletedObjectives.Contains(objective.Key));
        DrawFloatingZodiacNextStep(
            nextLeve == null ? "当前文书已完成" : $"完成理符：[{FormatLeveType(nextLeve)}] {nextLeve.Name}",
            nextLeve == null || nextLeve.MapX <= 0f ? null : () => NavigateToZodiacCoordinate(nextLeve.Zone, new ZodiacCoordinate(nextLeve.MapX, nextLeve.MapY)));
    }

    private void NavigateToZodiacCoordinate(string zone, ZodiacCoordinate? coordinate)
    {
        if (coordinate == null)
        {
            vnav.TeleportToMap(zone);
            return;
        }

        vnav.NavigateToMapCoordinate(zone, coordinate.MapX, coordinate.MapY);
    }

    private void NavigateToZodiacMonster(ZodiacMonsterObjective objective)
    {
        var worldCoordinate = objective.WorldCoordinates?.FirstOrDefault();
        if (worldCoordinate != null)
        {
            vnav.NavigateToWorldCoordinate(objective.Zone, new Vector3(worldCoordinate.X, worldCoordinate.Y, worldCoordinate.Z));
            return;
        }

        if (objective.Zone == OuterLaNoscea)
        {
            vnav.NavigateToWorldCoordinate(objective.Zone, new Vector3(OuterLaNosceaMineEntrance.X, OuterLaNosceaMineEntrance.Y, OuterLaNosceaMineEntrance.Z));
            return;
        }

        NavigateToZodiacCoordinate(objective.Zone, objective.Coordinates?.FirstOrDefault());
    }

    private static void DrawFloatingZodiacNextStep(string text, System.Action? navigate, string buttonLabel = "前往")
    {
        ImGui.TextColored(new Vector4(1f, 0.82f, 0.28f, 1f), "下一步");
        ImGui.SameLine();
        ImGui.TextWrapped(text);
        if (navigate != null)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton($"{buttonLabel}##floating-zodiac-next"))
            {
                navigate();
            }
        }
    }

    private void DrawFloatingPhantomMonitor(PhantomWeaponTarget[] targets, uint territory)
    {
        var targetTerritory = targets.Length > 0 ? targets[0].TerritoryType : 0;
        var completedTargets = targets.Count(target => configuration.CompletedTasks.Contains(target.Key));
        var fateCount = GetSecretFateCount(targetTerritory);
        var dutyCount = PhantomWeaponGuide.SecretDutyGroups.Sum(group => group.Duties.Count);
        var completedDuties = PhantomWeaponGuide.SecretDutyGroups.Sum(group => group.Duties.Count(duty => configuration.CompletedTasks.Contains(duty.Key)));
        var total = 9 + dutyCount;
        var completed = completedTargets + Math.Min(fateCount, 5) + completedDuties;

        var cardHeight = floatingPhantomMonitorOpen ? Math.Max(ImGui.GetContentRegionAvail().Y, 120f) : 34f;
        if (ImGui.BeginChild("floating-phantom-monitor-card", new Vector2(-1f, cardHeight), true))
        {
            if (DrawFloatingMonitorHeaderButton("幻武监控", "floating-phantom-monitor-header", new Vector4(0.45f, 0.70f, 0.98f, 1f)))
            {
                floatingPhantomMonitorOpen = !floatingPhantomMonitorOpen;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("幻武监控\n显示当前秘影阶段的指定目标、金牌 FATE 和迷宫/讨伐任务进度。点击标题可折叠或展开卡片。");
            }

            ImGui.SameLine();
            ImGui.TextDisabled(floatingPhantomMonitorOpen ? "收起" : "展开");
            if (!floatingPhantomMonitorOpen)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.62f, 0.95f, 0.72f, 1f), $"{completed} / {total}");
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("停止导航").X - ImGui.GetStyle().FramePadding.X * 2f);
            if (ImGui.SmallButton("停止导航##floating-phantom-stop-navigation"))
            {
                vnav.Stop();
            }
            ImGui.Separator();

            if (targets.Length > 0 && configuration.ShowSecretTargetsInFloatingWindow)
            {
                DrawFloatingPhantomTargetRow(targets, territory, targetTerritory, completedTargets, fateCount);
                ImGui.Separator();
            }

            if (configuration.ShowSecretDutiesInFloatingWindow)
            {
                DrawFloatingPhantomDutyRow(completedDuties, dutyCount);
            }
        }
        ImGui.EndChild();
    }

    private void DrawFloatingPhantomTargetRow(PhantomWeaponTarget[] targets, uint territory, uint targetTerritory, int completed, int fateCount)
    {
        ImGui.TextUnformatted("秘影指定目标");
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - 44f);
        if (ImGui.SmallButton(floatingPhantomTargetsOpen ? "收起##floating-phantom-targets-toggle" : "展开##floating-phantom-targets-toggle"))
        {
            floatingPhantomTargetsOpen = !floatingPhantomTargetsOpen;
        }

        ImGui.TextDisabled($"{targets[0].Zone} · {completed}/4 · FATE {Math.Min(fateCount, 5)}/5");
        ImGui.ProgressBar((completed + Math.Min(fateCount, 5)) / 9f, new Vector2(-1, 0), $"{completed + Math.Min(fateCount, 5)} / 9");

        if (!floatingPhantomTargetsOpen)
        {
            return;
        }

        if (ImGui.SmallButton("<##float-prev-zone"))
        {
            configuration.FloatingManualMode = true;
            SwitchFloatingSecretZone(-1);
            configuration.Save();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换上一张");

        ImGui.SameLine();
        if (ImGui.SmallButton(">##float-next-zone"))
        {
            configuration.FloatingManualMode = true;
            SwitchFloatingSecretZone(1);
            configuration.Save();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换下一张");

        ImGui.SameLine();
        if (ImGui.SmallButton("当##float-auto-zone"))
        {
            configuration.FloatingManualMode = false;
            configuration.Save();
            return;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("切换到当前地图");

        ImGui.SameLine();
        ImGui.TextDisabled(territory == targetTerritory && !configuration.FloatingManualMode ? "当前地图" : targets[0].Zone);

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
            if (IsChroniclerFateTerritory())
            {
                PrintChat("【新月岛地图】请使用【新月岛史官】插件。");
            }
            else if (IsUnsupportedFateTerritory())
            {
                PrintChat("【博兹雅/优雷卡】暂不支持该地图。");
            }
            else
            {
                var nearest = GetAvailableFates().FirstOrDefault();
                if (nearest != null)
                {
                    NavigateToFate(nearest);
                }
                else
                {
                    PrintChat("当前地图没有可参与的 FATE。");
                }
            }
        }

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
            ImGui.TextUnformatted(target.Name);
            ImGui.SameLine();
            if (ImGui.SmallButton($"导航##float-nav-{target.Key}"))
            {
                vnav.NavigateTo(target, configuration.UseFlightNavigation);
                PrintNavigationLog($"开始导航到 {target.Zone} {target.Name}");
            }
        }

        if (completed == targets.Length && fateCount >= 5)
        {
            ImGui.TextUnformatted("当前地图秘影目标已完成。");
        }
    }

    private void DrawFloatingPhantomDutyRow(int completed, int dutyCount)
    {
        ImGui.TextUnformatted("迷宫/讨伐任务");
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - 44f);
        if (ImGui.SmallButton(floatingPhantomDutiesOpen ? "收起##floating-phantom-duties-toggle" : "展开##floating-phantom-duties-toggle"))
        {
            floatingPhantomDutiesOpen = !floatingPhantomDutiesOpen;
        }

        ImGui.TextDisabled($"{completed} / {dutyCount} 完成");

        if (!floatingPhantomDutiesOpen)
        {
            return;
        }

        DrawFloatingSecretDuties();
    }

    private void DrawFloatingHuntAssistant()
    {
        var cardHeight = floatingHuntAssistantOpen ? 72f : 34f;
        if (ImGui.BeginChild("floating-hunt-card", new Vector2(-1f, cardHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var accent = configuration.HuntAssistantEnabled
                ? new Vector4(0.95f, 0.45f, 0.25f, 1f)
                : new Vector4(0.48f, 0.52f, 0.58f, 1f);
            if (DrawFloatingMonitorHeaderButton("狩猎助手", "floating-hunt-header", accent))
            {
                floatingHuntAssistantOpen = !floatingHuntAssistantOpen;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("狩猎助手\n显示车头、监听状态和狩猎 Flag 导航。点击标题可折叠或展开卡片。");
            }

            if (!floatingHuntAssistantOpen)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.SameLine();
            ImGui.TextColored(configuration.HuntAssistantEnabled ? new Vector4(0.42f, 0.88f, 0.58f, 1f) : new Vector4(0.65f, 0.67f, 0.70f, 1f),
                configuration.HuntAssistantEnabled ? "ONLINE" : "OFFLINE");
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("停止导航").X - ImGui.GetStyle().FramePadding.X * 2f);
            if (ImGui.SmallButton("停止导航##floating-hunt-stop-navigation"))
            {
                vnav.Stop();
            }

            ImGui.Separator();
            ImGui.TextUnformatted(string.IsNullOrWhiteSpace(configuration.HuntLeaderName)
                ? "车头  未指定"
                : $"车头  {configuration.HuntLeaderName}");
            ImGui.SameLine();
            ImGui.TextDisabled($"高度 +{configuration.HuntTargetHeight:0}y");
        }
        ImGui.EndChild();
    }

    private void DrawFloatingAvailableFates()
    {
        var fates = GetAvailableFates();
        var height = floatingFateAssistantOpen ? Math.Min(44f + Math.Max(fates.Length, 1) * 26f, 252f) : 34f;
        if (ImGui.BeginChild("floating-fate-card", new Vector2(-1f, height), true, ImGuiWindowFlags.NoScrollbar))
        {
            if (DrawFloatingMonitorHeaderButton("危命助手", "floating-fate-header", new Vector4(1f, 0.82f, 0.24f, 1f)))
            {
                floatingFateAssistantOpen = !floatingFateAssistantOpen;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("危命助手\n显示当前地图可参与的 FATE 和文书标记。点击标题可折叠或展开卡片。");
            }

            if (!floatingFateAssistantOpen)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.SameLine();
            ImGui.TextDisabled(fates.Length == 0 ? "当前地图" : $"{fates.Length} 个可参与");
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("停止导航").X - ImGui.GetStyle().FramePadding.X * 2f);
            if (ImGui.SmallButton("停止导航##floating-fates-stop-nav"))
            {
                vnav.Stop();
                PrintNavigationLog("已停止 FATE 导航。 ");
            }

            ImGui.Separator();
            if (fates.Length == 0)
            {
                ImGui.TextDisabled("当前地图没有可参与的 FATE。 ");
            }

            foreach (var fate in fates)
            {
                ImGui.TextUnformatted(GetFateDisplayName(fate));
                ImGui.SameLine();
                ImGui.TextDisabled($"{FormatFateState(fate.State)} {fate.Progress}% {FormatFateTime(fate.State, fate.TimeRemaining)}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"前往##floating-available-fate-{fate.FateId}"))
                {
                    NavigateToFate(fate);
                }
            }
        }
        ImGui.EndChild();
    }

    private IFate[] GetAvailableFates()
    {
        var territory = DalamudApi.ClientState.TerritoryType;
        var player = DalamudApi.ObjectTable.LocalPlayer;
        return DalamudApi.FateTable
            .Where(fate => fate != null && DalamudApi.FateTable.IsValid(fate))
            .Where(fate => fate!.TerritoryType.RowId == territory)
            .Where(fate => fate!.State is FateState.Preparing or FateState.Running)
            .Where(fate => fate!.Progress < 100)
            .Select(fate => fate!)
            .OrderBy(fate => player == null ? float.MaxValue : Vector3.Distance(player.Position, fate.Position))
            .ThenBy(fate => fate.TimeRemaining < 0 ? long.MaxValue : fate.TimeRemaining)
            .Take(8)
            .ToArray();
    }

    private string GetFateDisplayName(IFate fate)
    {
        var fateName = fate.Name.ToString();
        var book = GetSelectedZodiacBook();
        if (book != null && book.Fates.Any(objective =>
                fateName.Contains(objective.Name, StringComparison.OrdinalIgnoreCase)
                || objective.Name.Contains(fateName, StringComparison.OrdinalIgnoreCase)))
        {
            return $"{fateName}【{book.Name}】";
        }

        return fateName;
    }

    private ZodiacBookGuide? GetSelectedZodiacBook()
    {
        var characterKey = GetCurrentCharacterKey();
        if (string.IsNullOrWhiteSpace(characterKey)
            || !configuration.ZodiacProgressByCharacter.TryGetValue(characterKey, out var characterProgress)
            || !characterProgress.Jobs.TryGetValue(configuration.SelectedZodiacJobKey, out var jobProgress)
            || string.IsNullOrWhiteSpace(jobProgress.SelectedBookKey))
        {
            return null;
        }

        return ZodiacGuide.AnimusBooks.FirstOrDefault(book => book.Key == jobProgress.SelectedBookKey);
    }

    private void NavigateToFate(IFate fate)
    {
        if (IsChroniclerFateTerritory())
        {
            PrintChat("【新月岛地图】请使用【新月岛史官】插件。");
            return;
        }

        if (IsUnsupportedFateTerritory())
        {
            PrintChat("【博兹雅/优雷卡】暂不支持该地图。");
            return;
        }

        var player = DalamudApi.ObjectTable.LocalPlayer;
        if (player == null)
        {
            PrintNavigationLog("导航失败：当前没有本地角色。");
            return;
        }

        var target = fate.Position;
        var playerDistance = Vector2.Distance(new Vector2(player.Position.X, player.Position.Z), new Vector2(target.X, target.Z));
        var aetherytePosition = vnav.GetNearestCurrentTerritoryAetherytePosition(target);
        if (aetherytePosition.HasValue)
        {
            var aetheryteDistance = Vector2.Distance(new Vector2(aetherytePosition.Value.X, aetherytePosition.Value.Z), new Vector2(target.X, target.Z));
            if (playerDistance >= aetheryteDistance)
            {
                vnav.NavigateToFate(target, configuration.UseFlightNavigation);
                PrintNavigationLog($"导航到 FATE：{fate.Name}。");
                return;
            }
        }

        vnav.NavigateToFate(target, configuration.UseFlightNavigation);
        PrintNavigationLog($"导航到 FATE：{fate.Name}。");
    }

    private static bool IsChroniclerFateTerritory()
        => ChroniclerFateTerritories.Contains(DalamudApi.ClientState.TerritoryType);

    private static bool IsUnsupportedFateTerritory()
        => UnsupportedFateTerritories.Contains(DalamudApi.ClientState.TerritoryType);

    private static string FormatFateState(FateState state)
        => state switch
        {
            FateState.Preparing => "准备",
            FateState.Running => "进行中",
            FateState.Ending => "即将结束",
            _ => state.ToString(),
        };

    private static string FormatFateTime(FateState state, long seconds)
    {
        if (seconds < 0)
        {
            return state == FateState.Preparing ? "等待开始" : "时间未知";
        }

        return $"{seconds / 60}:{seconds % 60:00}";
    }

    private void DrawFloatingSecretDuties()
    {
        ImGui.Separator();
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

                ImGui.SameLine();
                if (ImGui.SmallButton($"AD执行##float-duty-ad-{duty.Key}"))
                {
                    autoDuty.Run(duty.Name);
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
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows)
            && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("floating-secret-targets-context");
        }

        if (!ImGui.BeginPopup("floating-secret-targets-context"))
        {
            return;
        }

        if (ImGui.BeginMenu("快捷入口"))
        {
            if (ImGui.MenuItem("幻武进度"))
            {
                selectedMainSection = GetMainSectionIndex("phantom");
                showWeaponProgressTab = true;
                OpenMainWindow();
            }

            if (ImGui.MenuItem("绝武总进度"))
            {
                selectedMainSection = GetMainSectionIndex("ultimate");
                stageSelectedSeries.Remove("ultimate");
                progressSeriesKey = "ultimate";
                OpenMainWindow();
            }

            if (ImGui.MenuItem("前往幻境村"))
            {
                vnav.GoToOccultVillage();
            }

            if (ImGui.MenuItem("妖表联动"))
            {
                selectedMainSection = GetMainSectionIndex("yokai");
                OpenMainWindow();
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();
        if (ImGui.MenuItem("打开主窗口"))
        {
            OpenMainWindow();
        }

        var monitorZodiac = configuration.ShowZodiacMonitorInFloatingWindow;
        if (ImGui.MenuItem("古武监控", string.Empty, monitorZodiac))
        {
            configuration.ShowZodiacMonitorInFloatingWindow = !monitorZodiac;
            configuration.Save();
        }

        var monitorPhantom = configuration.ShowSecretTargetsInFloatingWindow || configuration.ShowSecretDutiesInFloatingWindow;
        if (ImGui.MenuItem("幻武监控", string.Empty, monitorPhantom))
        {
            configuration.ShowSecretTargetsInFloatingWindow = !monitorPhantom;
            configuration.ShowSecretDutiesInFloatingWindow = !monitorPhantom;
            configuration.Save();
        }

        var showHuntAssistant = configuration.ShowHuntAssistantInFloatingWindow;
        if (ImGui.MenuItem("狩猎助手", string.Empty, showHuntAssistant))
        {
            configuration.ShowHuntAssistantInFloatingWindow = !showHuntAssistant;
            configuration.Save();
        }

        var showFateAssistant = configuration.ShowAvailableFatesInFloatingWindow;
        if (ImGui.MenuItem("危命助手", string.Empty, showFateAssistant))
        {
            configuration.ShowAvailableFatesInFloatingWindow = !showFateAssistant;
            configuration.Save();
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

        if (stage.Key == "secret")
        {
            foreach (var target in PhantomWeaponGuide.SecretTargets)
            {
                configuration.CompletedTasks.Remove(target.Key);
            }

            foreach (var duty in PhantomWeaponGuide.SecretDutyGroups.SelectMany(group => group.Duties))
            {
                configuration.CompletedTasks.Remove(duty.Key);
            }

            foreach (var territoryType in PhantomWeaponGuide.SecretTargets.Select(target => target.TerritoryType).Distinct())
            {
                configuration.Progress.Remove(GetSecretFateKey(territoryType));
            }
        }

        configuration.Save();
    }
}
