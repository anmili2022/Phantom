namespace Phantom;

public sealed record CollabNpc(string Key, string Name, string Zone, float X, float Y, string Purpose);

public sealed record YokaiRewardDefinition(
    string Key,
    string Name,
    string Category,
    string ItemNameFragment,
    IReadOnlyList<string>? AdditionalItemNameFragments = null,
    uint? MountId = null)
{
    public IReadOnlyList<string> ItemNameFragments
        => [ItemNameFragment, .. AdditionalItemNameFragments ?? []];
}

public sealed record YokaiRewardProgress(
    string Key,
    string Name,
    string Category,
    IReadOnlyList<uint> MatchedItemIds,
    bool Owned);
