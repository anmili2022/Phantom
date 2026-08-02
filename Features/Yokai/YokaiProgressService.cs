using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;

namespace Phantom;

public sealed class YokaiProgressService
{
    private static readonly InventoryType[] InventoryTypes =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.ArmoryMainHand,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryWaist,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.ArmorySoulCrystal,
        InventoryType.EquippedItems,
        InventoryType.KeyItems,
    };

    public sealed record GlamourDresserStatus(
        bool IsCached,
        int CachedItemCount,
        int SearchGlamourDresserCount,
        string SearchItemName);

    public IReadOnlyList<YokaiRewardProgress> ScanCurrentCharacter()
    {
        var ownedIds = GetOwnedItemIds();
        ownedIds.UnionWith(GetCachedStorageItemIds());
        var glamourSearchNames = GetGlamourDresserSearchNames();
        var items = DalamudApi.DataManager.GetExcelSheet<Item>().ToArray();

        return YokaiWatchGuide.Rewards
            .Select(definition =>
            {
                var matchedIds = definition.ItemNameFragments
                    .SelectMany(fragment => FindItemIds(items, fragment))
                    .Distinct()
                    .ToArray();
                var owned = definition.Category == YokaiWatchGuide.MountCategory
                    ? IsMountRewardUnlocked(items.Where(item => matchedIds.Contains(item.RowId)))
                    : definition.Category == YokaiWatchGuide.PortraitCategory
                        ? IsItemUnlocked(items.Where(item => matchedIds.Contains(item.RowId)))
                    : definition.Category == YokaiWatchGuide.MinionCategory
                        ? IsMinionRewardUnlocked(items.Where(item => matchedIds.Contains(item.RowId)))
                    : definition.Category == YokaiWatchGuide.WeaponCategory
                        ? definition.ItemNameFragments.All(fragment => FindItemIds(items, fragment).Any(ownedIds.Contains))
                    : matchedIds.Any(ownedIds.Contains)
                        || items.Any(item => matchedIds.Contains(item.RowId)
                            && glamourSearchNames.Any(searchName =>
                                string.Equals(searchName, item.Name.ExtractText(), StringComparison.Ordinal)
                                || searchName.Contains(item.Name.ExtractText(), StringComparison.Ordinal)
                                || item.Name.ExtractText().Contains(searchName, StringComparison.Ordinal)));

                return new YokaiRewardProgress(
                    definition.Key,
                    definition.Name,
                    definition.Category,
                    matchedIds,
                    owned);
            })
            .ToArray();
    }

    public unsafe GlamourDresserStatus GetGlamourDresserStatus()
    {
        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return new GlamourDresserStatus(false, 0, 0, string.Empty);
        }

        var cachedItemCount = 0;
        foreach (var itemId in finder->GlamourDresserItemIds)
        {
            if (itemId > 0)
            {
                cachedItemCount++;
            }
        }

        var searchCount = finder->Result == null ? 0 : finder->Result->GlamourDresserCount;
        var searchName = finder->Result == null ? string.Empty : finder->Result->ItemName.ToString().Trim();
        return new GlamourDresserStatus(cachedItemCount > 0, cachedItemCount, searchCount, searchName);
    }

    private static uint[] FindItemIds(IReadOnlyList<Item> items, string nameFragment)
    {
        var exactIds = items
            .Where(item => item.RowId > 0 && string.Equals(item.Name.ExtractText(), nameFragment, StringComparison.Ordinal))
            .Select(item => item.RowId)
            .ToArray();
        return exactIds.Length > 0
            ? exactIds
            : items
                .Where(item => item.RowId > 0 && item.Name.ExtractText().Contains(nameFragment, StringComparison.Ordinal))
                .Select(item => item.RowId)
                .ToArray();
    }

    private static unsafe HashSet<uint> GetOwnedItemIds()
    {
        var result = new HashSet<uint>();
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return result;
        }

        foreach (var inventoryType in InventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null)
            {
                continue;
            }

            for (var index = 0; index < container->Size; index++)
            {
                var itemId = NormalizeItemId(container->GetInventorySlot(index)->ItemId);
                if (itemId > 0)
                {
                    result.Add(itemId);
                }
            }
        }

        return result;
    }

    private static unsafe HashSet<uint> GetCachedStorageItemIds()
    {
        var result = new HashSet<uint>();
        var finder = ItemFinderModule.Instance();
        if (finder == null)
        {
            return result;
        }

        foreach (var itemId in finder->GlamourDresserItemIds)
        {
            if (itemId > 0)
            {
                result.Add(NormalizeItemId(itemId));
            }
        }

        foreach (var itemId in finder->SaddleBagItemIds)
        {
            AddItemId(result, itemId);
        }

        foreach (var itemId in finder->PremiumSaddleBagItemIds)
        {
            AddItemId(result, itemId);
        }

        foreach (var retainerPointer in finder->RetainerInventories.Values)
        {
            var retainer = retainerPointer.Value;
            if (retainer == null)
            {
                continue;
            }

            foreach (var itemId in retainer->EquippedItemIds)
            {
                AddItemId(result, itemId);
            }

            foreach (var itemId in retainer->ItemIds)
            {
                AddItemId(result, itemId);
            }
        }

        AddCabinetItemIds(result, finder);

        if (finder->Result != null && (finder->Result->GlamourDresserCount > 0 || finder->Result->ArmoireCount > 0))
        {
            foreach (var itemId in finder->RequestItemIds)
            {
                AddItemId(result, itemId);
            }
        }

        return result;
    }

    private static void AddItemId(HashSet<uint> result, uint itemId)
    {
        var normalizedItemId = NormalizeItemId(itemId);
        if (normalizedItemId > 0)
        {
            result.Add(normalizedItemId);
        }
    }

    private static unsafe void AddCabinetItemIds(HashSet<uint> result, ItemFinderModule* finder)
    {
        var uiState = UIState.Instance();
        var liveCabinetLoaded = uiState != null && uiState->Cabinet.IsCabinetLoaded();
        var cachedCabinetLoaded = finder->CabinetState == (byte)FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet.CabinetState.Loaded;
        if (!liveCabinetLoaded && !cachedCabinetLoaded)
        {
            return;
        }

        var cachedBits = finder->CabinetItemUnlockBits;
        foreach (var cabinetRow in DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>())
        {
            var owned = liveCabinetLoaded && uiState->Cabinet.IsItemInCabinet(cabinetRow.RowId);
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

    private static unsafe HashSet<string> GetGlamourDresserSearchNames()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var finder = ItemFinderModule.Instance();
        if (finder == null || finder->Result == null || finder->Result->GlamourDresserCount <= 0)
        {
            return result;
        }

        var itemName = finder->Result->ItemName.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(itemName))
        {
            result.Add(itemName);
        }

        return result;
    }

    private static unsafe bool IsMountRewardUnlocked(IEnumerable<Item> items)
    {
        var playerState = PlayerState.Instance();
        if (playerState == null)
        {
            return false;
        }

        foreach (var item in items)
        {
            var action = item.ItemAction.Value;
            if (action.Action.Value.RowId != 1322)
            {
                continue;
            }

            var mountId = action.Data[0];
            if (mountId > 0 && playerState->IsMountUnlocked(mountId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsItemUnlocked(IEnumerable<Item> items)
        => items.Any(item => DalamudApi.UnlockState.IsItemUnlocked(item));

    private static unsafe bool IsMinionRewardUnlocked(IEnumerable<Item> items)
    {
        var uiState = UIState.Instance();
        if (uiState == null)
        {
            return false;
        }

        foreach (var item in items)
        {
            var action = item.ItemAction.Value;
            if (action.Action.Value.RowId != 853)
            {
                continue;
            }

            var companionId = action.Data[0];
            if (companionId > 0 && uiState->IsCompanionUnlocked(companionId))
            {
                return true;
            }
        }

        return false;
    }

    private static uint NormalizeItemId(uint itemId)
        => itemId >= 1_000_000 ? itemId % 1_000_000 : itemId;
}
