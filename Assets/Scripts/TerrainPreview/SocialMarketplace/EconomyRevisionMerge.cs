using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class EconomyRevisionMerge
    {
        public static InventorySaveDto Clone(InventorySaveDto source)
        {
            InventorySaveDto result = new InventorySaveDto();
            if (source == null)
                return result;

            result.selectedSlotIndex = source.selectedSlotIndex;
            result.money = source.money;
            if (source.slots != null)
            {
                foreach (InventorySlotSaveDto slot in source.slots)
                {
                    result.slots.Add(slot == null
                        ? EmptySlot()
                        : new InventorySlotSaveDto
                        {
                            itemType = slot.itemType,
                            amount = slot.amount,
                            liquidMl = slot.liquidMl,
                            sprayerLiquid = slot.sprayerLiquid
                        });
                }
            }
            EnsureSlotCount(result);
            return result;
        }

        public static bool TryMerge(
            InventorySaveDto knownBase,
            InventorySaveDto localCurrent,
            InventorySaveDto refreshedServer,
            out InventorySaveDto merged,
            out string error)
        {
            merged = Clone(localCurrent);
            error = null;
            knownBase = Clone(knownBase);
            refreshedServer = Clone(refreshedServer);

            long remoteMoneyDelta = (long)refreshedServer.money - knownBase.money;
            long mergedMoney = (long)merged.money + remoteMoneyDelta;
            if (mergedMoney < 0 || mergedMoney > int.MaxValue)
            {
                error = "The remote money change could not be merged safely.";
                return false;
            }
            merged.money = (int)mergedMoney;

            foreach (InventoryItemType item in SocialMarketplaceCatalog.MergeItems)
            {
                int baseCount = Count(knownBase, item);
                int serverCount = Count(refreshedServer, item);
                int localCount = Count(merged, item);
                long target = (long)localCount + (serverCount - baseCount);
                if (target < 0 || target > int.MaxValue)
                {
                    error = "The remote inventory change for " + item + " could not be merged safely.";
                    return false;
                }
                if (!SetCount(merged, item, (int)target))
                {
                    error = "Inventory space is insufficient to merge the remote " + item + " change.";
                    return false;
                }
            }

            return true;
        }

        private static int Count(InventorySaveDto inventory, InventoryItemType item)
        {
            if (inventory == null || inventory.slots == null)
                return 0;
            int total = 0;
            string name = item.ToString();
            foreach (InventorySlotSaveDto slot in inventory.slots)
            {
                if (slot != null && slot.amount > 0 && slot.itemType == name)
                    total += slot.amount;
            }
            return total;
        }

        private static bool SetCount(InventorySaveDto inventory, InventoryItemType item, int target)
        {
            EnsureSlotCount(inventory);
            string name = item.ToString();

            foreach (InventorySlotSaveDto slot in inventory.slots)
            {
                if (slot != null && slot.itemType == name)
                    Clear(slot);
            }

            int remaining = target;
            int maxStack = PlayerInventory.GetMaxStack(item);
            foreach (InventorySlotSaveDto slot in inventory.slots)
            {
                if (remaining <= 0)
                    break;
                if (!IsEmpty(slot))
                    continue;

                int amount = Mathf.Min(maxStack, remaining);
                slot.itemType = name;
                slot.amount = amount;
                slot.liquidMl = PlayerInventory.IsLiquidItem(item)
                    ? PlayerInventory.GetDefaultLiquidMl(item)
                    : 0;
                slot.sprayerLiquid = SprayerLiquidType.None.ToString();
                remaining -= amount;
            }
            return remaining <= 0;
        }

        private static void EnsureSlotCount(InventorySaveDto inventory)
        {
            if (inventory.slots == null)
                inventory.slots = new List<InventorySlotSaveDto>();
            while (inventory.slots.Count < PlayerInventory.InventorySlotCount)
                inventory.slots.Add(EmptySlot());
            while (inventory.slots.Count > PlayerInventory.InventorySlotCount)
                inventory.slots.RemoveAt(inventory.slots.Count - 1);
        }

        private static InventorySlotSaveDto EmptySlot()
        {
            return new InventorySlotSaveDto
            {
                itemType = InventoryItemType.None.ToString(),
                amount = 0,
                liquidMl = 0,
                sprayerLiquid = SprayerLiquidType.None.ToString()
            };
        }

        private static bool IsEmpty(InventorySlotSaveDto slot)
        {
            return slot == null || slot.amount <= 0 ||
                   string.IsNullOrWhiteSpace(slot.itemType) ||
                   slot.itemType == InventoryItemType.None.ToString();
        }

        private static void Clear(InventorySlotSaveDto slot)
        {
            slot.itemType = InventoryItemType.None.ToString();
            slot.amount = 0;
            slot.liquidMl = 0;
            slot.sprayerLiquid = SprayerLiquidType.None.ToString();
        }
    }
}
