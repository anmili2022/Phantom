namespace Phantom;

public sealed record ZodiacCoordinate(float MapX, float MapY, string? Note = null)
{
    public override string ToString()
        => Note == null ? $"({MapX:0.0}, {MapY:0.0})" : $"({MapX:0.0}, {MapY:0.0}) {Note}";
}

public sealed class ZodiacCharacterProgress
{
    public Dictionary<string, ZodiacJobProgress> Jobs { get; set; } = new(StringComparer.Ordinal);
}

public sealed class ZodiacJobProgress
{
    public Dictionary<string, int> RequirementProgress { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CompletedObjectives { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CompletedBooks { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<ZodiacCoordinate>> UserCoordinates { get; set; } = new(StringComparer.Ordinal);
    public string SelectedBookKey { get; set; } = string.Empty;
}

public sealed record ZodiacMonsterObjective(
    string Key,
    string Name,
    string Zone,
    uint TerritoryType,
    float MapX,
    float MapY,
    int Needed = 3,
    string? JobKey = null,
    string LocationNotes = "",
    IReadOnlyList<ZodiacCoordinate>? Coordinates = null);

public sealed record ZodiacFateObjective(
    string Key,
    string Name,
    string Zone,
    uint TerritoryType,
    float MapX,
    float MapY,
    IReadOnlyList<string>? PrerequisiteKeys = null,
    string? BookKey = null,
    bool AnyFateInTerritory = false,
    string LocationNotes = "",
    string? PrerequisiteNpcName = null,
    string? PrerequisiteNpcZone = null,
    float PrerequisiteNpcMapX = 0f,
    float PrerequisiteNpcMapY = 0f);

public sealed record ZodiacDutyObjective(
    string Key,
    string Name,
    string Zone,
    uint TerritoryType,
    string? BookKey = null,
    string? GroupKey = null,
    bool Sequential = false,
    string LocationNotes = "");

public sealed record ZodiacLeveObjective(
    string Key,
    string Name,
    string Zone,
    float MapX,
    float MapY,
    string Category,
    int Level,
    string? BookKey = null,
    string LocationNotes = "");

public sealed record ZodiacBookGuide(
    string Key,
    string Name,
    IReadOnlyList<ZodiacMonsterObjective> Monsters,
    IReadOnlyList<ZodiacDutyObjective> Duties,
    IReadOnlyList<ZodiacFateObjective> Fates,
    IReadOnlyList<ZodiacLeveObjective> Leves);

public sealed record ZodiacFateAssistantContext(
    string StageKey,
    string? BookKey,
    string TargetKey,
    string TargetName,
    uint TerritoryType,
    bool AnyFateInTerritory);
