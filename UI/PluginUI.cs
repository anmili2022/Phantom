using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Fates;
using System.Numerics;

namespace Phantom;

public sealed class PluginUI
{
    private readonly PluginConfiguration configuration;
    private readonly VnavService vnav;
    private bool isMainWindowOpen;

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

        ImGui.SetNextWindowSize(new Vector2(760, 620), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("幻境武器助手", ref isMainWindowOpen))
        {
            ImGui.End();
            return;
        }

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

        ImGui.Separator();
        DrawStageTabs();

        ImGui.End();
    }

    private void DrawStageTabs()
    {
        var stages = PhantomWeaponGuide.Stages;
        if (configuration.SelectedStageIndex < 0 || configuration.SelectedStageIndex >= stages.Count)
        {
            configuration.SelectedStageIndex = 0;
        }

        if (ImGui.BeginTabBar("phantom-stage-tabs"))
        {
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (!ImGui.BeginTabItem($"{stage.Name}##{stage.Key}"))
                {
                    continue;
                }

                configuration.SelectedStageIndex = i;
                DrawStage(stage);
                ImGui.EndTabItem();
            }

            DrawDebugTab();

            ImGui.EndTabBar();
        }
    }

    private void DrawDebugTab()
    {
        if (!ImGui.BeginTabItem("DEBUG##phantom-debug"))
        {
            return;
        }

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

        ImGui.EndTabItem();
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
        ImGui.TextDisabled("导航会先尝试 Lifestream 传送到目标地图，再用 vnavmesh 前往坐标。坐标来自灰机 Wiki / xivdaily。 ");

        foreach (var group in PhantomWeaponGuide.SecretTargets.GroupBy(target => target.Zone))
        {
            if (!ImGui.CollapsingHeader($"{group.Key}##secret-zone-{group.Key}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                continue;
            }

            var targets = group.ToArray();
            var completed = targets.Count(target => configuration.CompletedTasks.Contains(target.Key));
            var fateCount = GetSecretFateCount(targets[0].TerritoryType);
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
                ImGui.TableSetupColumn("地图坐标", ImGuiTableColumnFlags.WidthFixed, 120);
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
        ImGui.TextUnformatted($"X:{target.MapX:F2} Y:{target.MapY:F2}");

        ImGui.TableNextColumn();
        if (ImGui.SmallButton($"导航##nav-{target.Key}"))
        {
            vnav.NavigateTo(target, configuration.UseFlightNavigation);
        }
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
            configuration.FloatingSecretTerritoryType = territory;
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
            configuration.FloatingSecretTerritoryType = territory;
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
                    vnav.NavigateTo(new Vector3(fatePos.X, fatePos.Y, fatePos.Z), configuration.UseFlightNavigation);
                    PrintChat($"导航到最近FATE: {nearest.Name}");
                }
                else
                {
                    PrintChat("当前地图没有活跃的FATE。");
                }
            }
        }

        ImGui.Separator();
        foreach (var target in targets.Where(target => !configuration.CompletedTasks.Contains(target.Key)))
        {
            ImGui.TextUnformatted($"{target.Name}  X:{target.MapX:F1} Y:{target.MapY:F1}");
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

        ImGui.End();
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
