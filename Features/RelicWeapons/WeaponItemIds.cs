namespace Phantom;

/// <summary>Fixed Item.RowId mappings exported from the Chinese client item sheet.</summary>
public static class WeaponItemIds
{
    private const string ResourceName = "Phantom.WeaponItemIds";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<uint>>> Mappings = Load();

    public static IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<uint>> Get(string seriesKey)
        => Mappings.TryGetValue(seriesKey, out var mapping)
            ? mapping
            : throw new ArgumentOutOfRangeException(nameof(seriesKey), seriesKey, "No fixed Item.RowId mapping is available for this series.");

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<uint>>> Load()
    {
        using var stream = typeof(WeaponItemIds).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var entries = new List<(string SeriesKey, string JobKey, string StageKey, uint ItemId)>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split('|', 5);
            if (fields.Length != 5 || !uint.TryParse(fields[3], out var itemId))
            {
                throw new InvalidOperationException($"Invalid weapon Item.RowId mapping: '{line}'.");
            }

            entries.Add((fields[0], fields[1], fields[2], itemId));
        }

        return entries
            .GroupBy(entry => entry.SeriesKey, StringComparer.Ordinal)
            .ToDictionary(
                series => series.Key,
                series => (IReadOnlyDictionary<(string JobKey, string StageKey), IReadOnlyList<uint>>)series
                    .GroupBy(entry => (entry.JobKey, entry.StageKey))
                    .ToDictionary(
                        entry => entry.Key,
                        entry => (IReadOnlyList<uint>)entry.Select(value => value.ItemId).ToArray()),
                StringComparer.Ordinal);
    }
}
