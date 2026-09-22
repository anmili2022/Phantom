using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Phantom;

public sealed class HuntAssistant : IDisposable
{
    private sealed record HuntFlagTarget(uint TerritoryType, uint MapId, float MapX, float MapY, Vector3 Position);

    private readonly PluginConfiguration configuration;
    private readonly VnavService vnav;
    private string lastFlagKey = string.Empty;
    private DateTime lastFlagUtc = DateTime.MinValue;
    private HuntFlagTarget? latestFlag;
    private bool pendingLatestFlagNavigation;

    public HuntAssistant(PluginConfiguration configuration, VnavService vnav)
    {
        this.configuration = configuration;
        this.vnav = vnav;
        DalamudApi.ChatGui.ChatMessage += OnChatMessage;
        DalamudApi.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudApi.ChatGui.ChatMessage -= OnChatMessage;
        DalamudApi.Framework.Update -= OnFrameworkUpdate;
    }

    public bool HasLatestFlag => latestFlag != null;

    public bool IsWaitingForCombat => pendingLatestFlagNavigation && DalamudApi.Condition[ConditionFlag.InCombat];

    public string LatestFlagLabel => latestFlag == null
        ? "尚未收到车头 Flag"
        : $"最新 Flag  X:{latestFlag.MapX:0.0} Y:{latestFlag.MapY:0.0}";

    public void NavigateToLatestFlag()
    {
        if (latestFlag == null)
        {
            PrintTestStatus("尚未收到可用的车头 Flag。", false);
            return;
        }

        pendingLatestFlagNavigation = true;
        vnav.Stop();
        TryNavigateLatestFlag();
    }

    private void OnChatMessage(object message)
    {
        if (!configuration.Enabled
            || string.IsNullOrWhiteSpace(configuration.HuntLeaderName)
            || (!configuration.HuntAssistantEnabled && !configuration.HuntAssistantEchoLeaderMessages))
        {
            return;
        }

        var sender = ExtractPlayerName(message)
            ?? ExtractText(message, "OriginalSender", "Author", "Sender", "AuthorName", "SenderName", "PlayerName", "Name");
        if (!MatchesLeader(sender, configuration.HuntLeaderName))
        {
            return;
        }

        if (configuration.HuntAssistantEchoLeaderMessages)
        {
            PrintLeaderMessage(sender, ExtractText(message, "OriginalMessage", "Message"));
        }

        if (!configuration.HuntAssistantEnabled)
        {
            return;
        }

        if (!TryExtractMapLink(message, out var mapLink))
        {
            DalamudApi.Log.Information("Ignored hunt chat from {Sender}: no MapLink payload found.", sender);
            PrintTestStatus("未读取到 Flag payload。", false);
            return;
        }

        var territoryType = mapLink.TerritoryType.RowId;
        var mapId = mapLink.Map.RowId;
        var mapX = mapLink.XCoord;
        var mapY = mapLink.YCoord;
        var flagKey = $"{sender}|{territoryType}|{mapId}|{mapLink.RawX}|{mapLink.RawY}";
        if (flagKey == lastFlagKey && DateTime.UtcNow - lastFlagUtc < TimeSpan.FromSeconds(5))
        {
            PrintTestStatus("忽略 5 秒内重复的车头 Flag。", false);
            return;
        }

        lastFlagKey = flagKey;
        lastFlagUtc = DateTime.UtcNow;

        if (!vnav.TryResolveMapLinkPosition(territoryType, mapId, mapX, mapY, out var position))
        {
            DalamudApi.Log.Information("Ignored hunt Flag from {Leader}: unable to resolve territory {TerritoryType}, map {MapId}.", configuration.HuntLeaderName, territoryType, mapId);
            PrintTestStatus($"无法解析 Flag 地图：Territory={territoryType}，Map={mapId}。", false);
            return;
        }

        TryMarkMapFlag(territoryType, mapId, position);
        latestFlag = new HuntFlagTarget(territoryType, mapId, mapX, mapY, position);
        pendingLatestFlagNavigation = true;
        vnav.Stop();
        if (DalamudApi.Condition[ConditionFlag.InCombat])
        {
            DalamudApi.Log.Information("Queued latest hunt Flag during combat: {X:0.0}, {Y:0.0}.", mapX, mapY);
            PrintTestStatus($"战斗中，已保存最新 Flag：X={mapX:0.0}，Y={mapY:0.0}；脱战后自动前往。", false);
            return;
        }

        TryNavigateLatestFlag();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _ = framework;
        if (pendingLatestFlagNavigation && !DalamudApi.Condition[ConditionFlag.InCombat])
        {
            TryNavigateLatestFlag();
        }
    }

    private void TryNavigateLatestFlag()
    {
        if (!pendingLatestFlagNavigation || latestFlag == null || DalamudApi.Condition[ConditionFlag.InCombat])
        {
            return;
        }

        pendingLatestFlagNavigation = false;
        TryMarkMapFlag(latestFlag.TerritoryType, latestFlag.MapId, latestFlag.Position);
        vnav.NavigateToHuntTarget(latestFlag.TerritoryType, latestFlag.Position, configuration.HuntTargetHeight);
        DalamudApi.Log.Information("Navigating to latest hunt Flag from {Leader}: {X:0.0}, {Y:0.0}.", configuration.HuntLeaderName, latestFlag.MapX, latestFlag.MapY);
        PrintTestStatus($"已前往最新 Flag：Territory={latestFlag.TerritoryType}，Map={latestFlag.MapId}，X={latestFlag.MapX:0.0}，Y={latestFlag.MapY:0.0}。", true);
    }

    private static bool TryExtractMapLink(object message, out MapLinkPayload mapLink)
    {
        mapLink = null!;
        try
        {
            foreach (var propertyName in new[] { "OriginalMessage", "Message", "OriginalSender", "Sender" })
            {
                var content = message.GetType().GetProperty(propertyName)?.GetValue(message);
                var payloads = content?.GetType().GetProperty("Payloads")?.GetValue(content) as System.Collections.IEnumerable;
                if (payloads == null)
                {
                    continue;
                }

                foreach (var payload in payloads)
                {
                    if (payload is MapLinkPayload payloadMapLink)
                    {
                        if (payloadMapLink.TerritoryType.RowId != 0 && payloadMapLink.Map.RowId != 0)
                        {
                            mapLink = payloadMapLink;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to read MapLink payload from hunt chat message.");
        }

        return false;
    }

    private static bool MatchesLeader(string sender, string leader)
    {
        var normalizedSender = NormalizeName(sender);
        var normalizedLeader = NormalizeName(leader);
        return normalizedSender.Length > 0
            && normalizedLeader.Length > 0
            && string.Equals(normalizedSender, normalizedLeader, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string name)
    {
        var separator = name.IndexOf('@');
        if (separator >= 0)
        {
            name = name[..separator];
        }

        return name.Trim().TrimEnd('：', ':');
    }

    private static uint GetUInt(object value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var propertyValue = value.GetType().GetProperty(propertyName)?.GetValue(value);
            if (propertyValue == null)
            {
                continue;
            }

            try
            {
                return Convert.ToUInt32(propertyValue);
            }
            catch
            {
                var rowId = propertyValue.GetType().GetProperty("RowId")?.GetValue(propertyValue);
                if (rowId != null)
                {
                    return Convert.ToUInt32(rowId);
                }
            }
        }

        return 0;
    }

    private static float GetFloat(object value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var propertyValue = value.GetType().GetProperty(propertyName)?.GetValue(value);
            if (propertyValue != null)
            {
                return Convert.ToSingle(propertyValue);
            }
        }

        return 0f;
    }

    private static string ExtractText(object message, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = message.GetType().GetProperty(propertyName)?.GetValue(message);
            var text = GetText(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static string GetText(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var text = value.GetType().GetProperty("TextValue")?.GetValue(value) as string;
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        foreach (var propertyName in new[] { "Name", "Text", "Value" })
        {
            var nested = value.GetType().GetProperty(propertyName)?.GetValue(value);
            text = nested?.GetType().GetProperty("TextValue")?.GetValue(nested) as string ?? nested as string;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return value is string stringValue ? stringValue : string.Empty;
    }

    private static string? ExtractPlayerName(object message)
    {
        foreach (var propertyName in new[] { "OriginalSender", "Sender" })
        {
            var content = message.GetType().GetProperty(propertyName)?.GetValue(message);
            var payloads = content?.GetType().GetProperty("Payloads")?.GetValue(content) as System.Collections.IEnumerable;
            if (payloads == null)
            {
                continue;
            }

            foreach (var payload in payloads)
            {
                if (payload is PlayerPayload player && !string.IsNullOrWhiteSpace(player.PlayerName))
                {
                    return player.PlayerName;
                }
            }
        }

        return null;
    }

    private static unsafe void TryMarkMapFlag(uint territoryType, uint mapId, Vector3 worldPosition)
    {
        try
        {
            var agentMap = AgentMap.Instance();
            if (agentMap == null)
            {
                return;
            }

            agentMap->SetFlagMapMarker(territoryType, mapId, worldPosition);
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to set the map Flag for hunt navigation.");
        }
    }

    private static void PrintLeaderMessage(string sender, string text)
    {
        try
        {
            DalamudApi.ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = new SeStringBuilder()
                    .AddUiForeground("[Phantom] [狩猎测试] ", 37)
                    .AddText($"{NormalizeName(sender)}：{text}")
                    .Build(),
            });
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to echo hunt leader chat message.");
        }
    }

    private void PrintTestStatus(string text, bool navigationStarted)
    {
        if (!configuration.HuntAssistantEchoLeaderMessages)
        {
            return;
        }

        try
        {
            DalamudApi.ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = new SeStringBuilder()
                    .AddUiForeground("[Phantom] [狩猎测试] ", 37)
                    .AddText(navigationStarted ? $"开始导航。{text}" : text)
                    .Build(),
            });
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to print hunt test status.");
        }
    }
}
