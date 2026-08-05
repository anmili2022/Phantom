using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Phantom;

public sealed class HuntAssistant : IDisposable
{
    private readonly PluginConfiguration configuration;
    private readonly VnavService vnav;

    public HuntAssistant(PluginConfiguration configuration, VnavService vnav)
    {
        this.configuration = configuration;
        this.vnav = vnav;
        DalamudApi.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        DalamudApi.ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(object message)
    {
        if (!configuration.Enabled
            || string.IsNullOrWhiteSpace(configuration.HuntLeaderName)
            || (!configuration.HuntAssistantEnabled && !configuration.HuntAssistantEchoLeaderMessages))
        {
            return;
        }

        var sender = ExtractText(message, "OriginalSender", "Author", "Sender", "AuthorName", "SenderName", "PlayerName", "Name");
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

        if (!TryExtractMapLink(message, out var territoryType, out var mapId, out var mapX, out var mapY))
        {
            DalamudApi.Log.Information("Ignored hunt chat from {Sender}: no MapLink payload found.", sender);
            PrintTestStatus("未读取到 Flag payload。", false);
            return;
        }

        if (!vnav.TryResolveMapLinkPosition(territoryType, mapId, mapX, mapY, out var position))
        {
            DalamudApi.Log.Information("Ignored hunt Flag from {Leader}: unable to resolve territory {TerritoryType}, map {MapId}.", configuration.HuntLeaderName, territoryType, mapId);
            PrintTestStatus($"无法解析 Flag 地图：Territory={territoryType}，Map={mapId}。", false);
            return;
        }

        vnav.NavigateToHuntTarget(territoryType, position, configuration.HuntTargetHeight);
        DalamudApi.Log.Information("Navigating to hunt Flag from {Leader}: {X:0.0}, {Y:0.0}.", configuration.HuntLeaderName, mapX, mapY);
        PrintTestStatus($"已解析 Flag：Territory={territoryType}，Map={mapId}，X={mapX:0.0}，Y={mapY:0.0}。", true);
    }

    private static bool TryExtractMapLink(object message, out uint territoryType, out uint mapId, out float mapX, out float mapY)
    {
        territoryType = mapId = 0;
        mapX = mapY = 0f;
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
                    if (payload is MapLinkPayload mapLink)
                    {
                        territoryType = mapLink.TerritoryType.RowId;
                        mapId = mapLink.Map.RowId;
                        mapX = mapLink.XCoord;
                        mapY = mapLink.YCoord;
                        return territoryType != 0 && mapId != 0;
                    }

                    if (payload == null || !payload.GetType().Name.Contains("MapLink", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    territoryType = GetUInt(payload, "TerritoryType", "Territory");
                    mapId = GetUInt(payload, "Map", "MapId");
                    mapX = GetFloat(payload, "XCoord", "MapX", "RawX");
                    mapY = GetFloat(payload, "YCoord", "MapY", "RawY");
                    return territoryType != 0 && mapX != 0f && mapY != 0f;
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
