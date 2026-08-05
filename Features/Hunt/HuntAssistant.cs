using System.Numerics;

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
        if (!configuration.Enabled || !configuration.HuntAssistantEnabled || string.IsNullOrWhiteSpace(configuration.HuntLeaderName))
        {
            return;
        }

        if (!string.Equals(ExtractText(message, "Author", "Sender"), configuration.HuntLeaderName, StringComparison.OrdinalIgnoreCase)
            || !TryExtractMapLink(message, out var territoryType, out var mapId, out var mapX, out var mapY))
        {
            return;
        }

        if (!vnav.TryResolveMapLinkPosition(territoryType, mapId, mapX, mapY, out var position))
        {
            DalamudApi.Log.Information("Ignored hunt Flag from {Leader}: target territory {TerritoryType} is not current territory {CurrentTerritoryType}.", configuration.HuntLeaderName, territoryType, DalamudApi.ClientState.TerritoryType);
            return;
        }

        vnav.NavigateToHuntTarget(position, configuration.HuntTargetHeight);
        DalamudApi.Log.Information("Navigating to hunt Flag from {Leader}: {X:0.0}, {Y:0.0}.", configuration.HuntLeaderName, mapX, mapY);
    }

    private static bool TryExtractMapLink(object message, out uint territoryType, out uint mapId, out float mapX, out float mapY)
    {
        territoryType = mapId = 0;
        mapX = mapY = 0f;
        try
        {
            var content = message.GetType().GetProperty("Message")?.GetValue(message);
            var payloads = content?.GetType().GetProperty("Payloads")?.GetValue(content) as System.Collections.IEnumerable;
            if (payloads == null)
            {
                return false;
            }

            foreach (var payload in payloads)
            {
                if (payload?.GetType().Name != "MapLinkPayload")
                {
                    continue;
                }

                territoryType = GetUInt(payload, "TerritoryType");
                mapId = GetUInt(payload, "MapId");
                mapX = GetFloat(payload, "XCoord", "RawX");
                mapY = GetFloat(payload, "YCoord", "RawY");
                return territoryType != 0 && mapX != 0f && mapY != 0f;
            }
        }
        catch (Exception ex)
        {
            DalamudApi.Log.Warning(ex, "Failed to read MapLink payload from hunt chat message.");
        }

        return false;
    }

    private static uint GetUInt(object value, string propertyName)
        => Convert.ToUInt32(value.GetType().GetProperty(propertyName)?.GetValue(value) ?? 0u);

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
            var text = value?.GetType().GetProperty("TextValue")?.GetValue(value) as string ?? value?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }
}
