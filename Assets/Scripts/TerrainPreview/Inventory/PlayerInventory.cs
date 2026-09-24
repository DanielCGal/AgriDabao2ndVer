using System;
using UnityEngine;
namespace AgriDabao3D
{
    [Serializable]
    public class InventorySlotData
    {
        public InventoryItemType itemType = InventoryItemType.None;
        public int amount = 0;
        public int liquidMl = 0;
        public SprayerLiquidType sprayerLiquid = SprayerLiquidType.None;
        public bool IsEmpty => itemType == InventoryItemType.None || amount <= 0;
        public void Clear()
        {
            itemType = InventoryItemType.None;
            amount = 0;
            liquidMl = 0;
            sprayerLiquid = SprayerLiquidType.None;
        }
        public void Set(InventoryItemType type, int value)
        {
            itemType = type;
            amount = Mathf.Max(0, value);
            liquidMl = PlayerInventory.GetDefaultLiquidMl(type);
            sprayerLiquid = SprayerLiquidType.None;
            if (itemType == InventoryItemType.None || amount <= 0)
                Clear();
        }
        public void CopyFrom(
    InventorySlotData source)
        {
            if (source == null || source.IsEmpty)
            {
                Clear();
                return;
            }
            itemType = source.itemType;
            amount = source.amount;
            liquidMl = source.liquidMl;
            sprayerLiquid = source.sprayerLiquid;
        }
    }
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }
        public const int HotbarSlotCount = 6;
        public const int InventorySlotCount = 36;
        [Header("Selection")]
        public int selectedSlotIndex = 0;
        public InventoryItemType selectedItem = InventoryItemType.Machete;
        [Header("Slots")]
        public InventorySlotData[] slots = new InventorySlotData[InventorySlotCount];
        public int money { get; private set; }
        public event Action OnInventoryChanged;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureSlots();
            if (FarmLoadContext.IsRestoring)
            {
                ClearAllSlots();
                selectedSlotIndex = 0;
                money = 0;
            }
            else
            {
                TutorialState.ResetForNewFarm();
                BuildStartingInventory();
                money = 0;
            }
            RefreshSelectedItem(false);
        }
        private void EnsureSlots()
        {
            if (slots == null || slots.Length != InventorySlotCount)
                slots = new InventorySlotData[InventorySlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    slots[i] = new InventorySlotData();
            }
        }
        private void ClearAllSlots()
        {
            EnsureSlots();
            for (int i = 0; i < slots.Length; i++)
                slots[i].Clear();
        }
        private void BuildStartingInventory()
        {
            ClearAllSlots();

            TutorialState.EnsureStartingSeedsRolled(
                SelectedAreaState.SelectedDistrictName);

            if (!TutorialState.Completed)
                return;

            slots[0].Set(InventoryItemType.Machete, 1);

            int slot = 1;
            foreach (InventoryItemType seed in TutorialState.StartingSeeds)
            {
                if (slot >= HotbarSlotCount - 2)
                    break;

                slots[slot].Set(seed, DistrictCropPools.SeedsPerKind);
                slot++;
            }

            slots[HotbarSlotCount - 2].Set(InventoryItemType.WateringCan, 1);
            slots[HotbarSlotCount - 1].Set(InventoryItemType.Shovel, 1);

            slots[HotbarSlotCount].Set(InventoryItemType.FruitBag, 10);
            slots[HotbarSlotCount + 1].Set(InventoryItemType.MulchBag, 3);
        }

        public InventorySlotData GetSelectedSlot()
        {
            EnsureSlots();
            return IsValidSlot(selectedSlotIndex) ? slots[selectedSlotIndex] : null;
        }
        public static bool IsLiquidItem(
            InventoryItemType item)
        {
            return item ==
                       InventoryItemType.InsecticideLiter ||
                   item ==
                       InventoryItemType.DisinfectantLiter ||
                   item ==
                       InventoryItemType.NeemSoapLiter ||
                   item ==
                       InventoryItemType.BtBioInsecticideLiter ||
                   item ==
                       InventoryItemType.CopperFungicideLiter;
        }
        public static int GetDefaultLiquidMl(InventoryItemType item)
        {
            return IsLiquidItem(item) ? 5000 : 0;
        }
        public static SprayerLiquidType GetLiquidTypeFromItem(
    InventoryItemType item)
        {
            switch (item)
            {
                case InventoryItemType.InsecticideLiter:
                    return SprayerLiquidType.Insecticide;
                case InventoryItemType.DisinfectantLiter:
                    return SprayerLiquidType.Disinfectant;
                case InventoryItemType.NeemSoapLiter:
                    return SprayerLiquidType.NeemSoap;
                case InventoryItemType.BtBioInsecticideLiter:
                    return SprayerLiquidType.BtBioInsecticide;
                case InventoryItemType.CopperFungicideLiter:
                    return SprayerLiquidType.CopperFungicide;
                default:
                    return SprayerLiquidType.None;
            }
        }
        public static InventoryItemType GetItemFromLiquidType(
    SprayerLiquidType liquidType)
        {
            switch (liquidType)
            {
                case SprayerLiquidType.Insecticide:
                    return InventoryItemType.InsecticideLiter;
                case SprayerLiquidType.Disinfectant:
                    return InventoryItemType.DisinfectantLiter;
                case SprayerLiquidType.NeemSoap:
                    return InventoryItemType.NeemSoapLiter;
                case SprayerLiquidType.BtBioInsecticide:
                    return InventoryItemType.BtBioInsecticideLiter;
                case SprayerLiquidType.CopperFungicide:
                    return InventoryItemType.CopperFungicideLiter;
                default:
                    return InventoryItemType.None;
            }
        }
        private static bool TryGetSprayerTreatment(
    SprayerLiquidType liquidType,
    out PestDiseaseMitigation mitigation,
    out float treatmentStrength,
    out string treatmentName)
        {
            mitigation = PestDiseaseMitigation.None;
            treatmentStrength = 0f;
            treatmentName = "";
            switch (liquidType)
            {
                case SprayerLiquidType.NeemSoap:
                    mitigation =
                        PestDiseaseMitigation.NeemSoap;
                    treatmentStrength = 20f;
                    treatmentName = "Neem Soap";
                    return true;
                case SprayerLiquidType.BtBioInsecticide:
                    mitigation =
                        PestDiseaseMitigation.BtBioSpray;
                    treatmentStrength = 25f;
                    treatmentName = "Bt Bio-Insecticide";
                    return true;
                case SprayerLiquidType.CopperFungicide:
                    mitigation =
                        PestDiseaseMitigation.CopperFungicide;
                    treatmentStrength = 18f;
                    treatmentName = "Copper Fungicide";
                    return true;
                case SprayerLiquidType.Disinfectant:
                    mitigation =
                        PestDiseaseMitigation.Disinfectant;
                    treatmentStrength = 15f;
                    treatmentName = "Disinfectant";
                    return true;
                case SprayerLiquidType.Insecticide:
                    mitigation =
                        PestDiseaseMitigation.BtBioSpray;
                    treatmentStrength = 20f;
                    treatmentName = "Insecticide";
                    return true;
                default:
                    return false;
            }
        }
        public bool TryLoadSprayerFromSlot(int liquidSlotIndex, int sprayerSlotIndex)
        {
            EnsureSlots();
            if (!IsValidSlot(liquidSlotIndex) || !IsValidSlot(sprayerSlotIndex))
                return false;
            InventorySlotData liquidSlot = slots[liquidSlotIndex];
            InventorySlotData sprayerSlot = slots[sprayerSlotIndex];
            if (liquidSlot == null || sprayerSlot == null)
                return false;
            if (!IsLiquidItem(liquidSlot.itemType))
                return false;
            if (sprayerSlot.itemType != InventoryItemType.SprayerPump)
                return false;
            SprayerLiquidType newLiquid = GetLiquidTypeFromItem(liquidSlot.itemType);
            int newLiquidMl = liquidSlot.liquidMl > 0 ? liquidSlot.liquidMl : GetDefaultLiquidMl(liquidSlot.itemType);
            SprayerLiquidType oldLiquid = sprayerSlot.sprayerLiquid;
            int oldLiquidMl = sprayerSlot.liquidMl;
            sprayerSlot.sprayerLiquid = newLiquid;
            sprayerSlot.liquidMl = newLiquidMl;
            if (oldLiquid != SprayerLiquidType.None && oldLiquidMl > 0)
            {
                InventoryItemType oldLiquidItem = GetItemFromLiquidType(oldLiquid);
                liquidSlot.Set(oldLiquidItem, 1);
                liquidSlot.liquidMl = oldLiquidMl;
            }
            else
            {
                liquidSlot.Clear();
            }
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return true;
        }
        public bool TryUseSelectedSprayerOnCrop(
    PestDiseaseAffectedCrop disease,
    out string message)
        {
            message = "";
            if (disease == null)
            {
                message =
                    "No crop pest or disease component was found.";
                return false;
            }
            InventorySlotData pumpSlot =
                GetSelectedSlot();
            if (pumpSlot == null ||
                pumpSlot.itemType !=
                    InventoryItemType.SprayerPump)
            {
                message =
                    "Sprayer Pump is not selected.";
                return false;
            }
            const int liquidUseMl = 500;
            if (pumpSlot.sprayerLiquid ==
                    SprayerLiquidType.None ||
                pumpSlot.liquidMl < liquidUseMl)
            {
                message =
                    "The Sprayer Pump does not contain enough liquid.";
                return false;
            }
            if (!TryGetSprayerTreatment(
                    pumpSlot.sprayerLiquid,
                    out PestDiseaseMitigation mitigation,
                    out float treatmentStrength,
                    out string treatmentName))
            {
                message =
                    "The loaded liquid cannot be used as a treatment.";
                return false;
            }
            float removed =
                disease.ApplyMitigation(
                    mitigation,
                    treatmentStrength
                );
            pumpSlot.liquidMl = Mathf.Max(
                0,
                pumpSlot.liquidMl - liquidUseMl
            );
            if (pumpSlot.liquidMl <= 0)
            {
                pumpSlot.sprayerLiquid =
                    SprayerLiquidType.None;
                pumpSlot.liquidMl = 0;
            }
            if (removed > 0.01f)
            {
                message =
                    $"{treatmentName} applied to " +
                    $"{disease.CropDisplayName}. " +
                    $"Total pest or disease severity reduced " +
                    $"by {removed:F1}%.";
            }
            else
            {
                message =
                    $"{treatmentName} was sprayed, but it does " +
                    "not control any active condition on this crop.";
            }
            if (ClimateEventTracker.Instance != null && ClimateEventTracker.Instance.IsTrackingEvent)
            {
                ClimateEventTracker.Instance.RecordAction(new ClimateActionRecord
                {
                    actionType = "SprayCrop",
                    itemType = treatmentName,
                    cropType = disease.CropDisplayName,
                    cropId = disease.CropId,
                    gameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f,
                    worldPosition = new SerializableVector3(disease.transform.position),
                    actionSucceeded = true,
                    recommendedForEvent = false,
                    effectivenessScore = -1f,
                    details = message
                });
            }
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return true;
        }
        public InventorySlotData GetSlot(int slotIndex)
        {
            EnsureSlots();
            if (!IsValidSlot(slotIndex))
                return null;
            return slots[slotIndex];
        }
        public bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < InventorySlotCount;
        }
        public bool IsHotbarSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < HotbarSlotCount;
        }
        public void SelectSlot(int slotIndex)
        {
            if (!IsHotbarSlot(slotIndex))
                return;
            selectedSlotIndex = slotIndex;
            RefreshSelectedItem(true);
        }
        public void SelectItem(InventoryItemType item)
        {
            if (item == InventoryItemType.None)
                return;
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                if (!slots[i].IsEmpty && slots[i].itemType == item)
                {
                    SelectSlot(i);
                    return;
                }
            }
        }
        private void RefreshSelectedItem(bool notify)
        {
            EnsureSlots();
            if (!IsHotbarSlot(selectedSlotIndex))
                selectedSlotIndex = 0;
            InventorySlotData selectedSlot = slots[selectedSlotIndex];
            selectedItem = selectedSlot != null && !selectedSlot.IsEmpty
                ? selectedSlot.itemType
                : InventoryItemType.None;
            if (notify)
                OnInventoryChanged?.Invoke();
        }
        public int GetCount(InventoryItemType item)
        {
            if (item == InventoryItemType.None)
                return 0;
            EnsureSlots();
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].itemType == item)
                    total += slots[i].amount;
            }
            return total;
        }
        public bool HasItem(InventoryItemType item, int amount = 1)
        {
            return GetCount(item) >= amount;
        }
        public bool AddItem(InventoryItemType item, int amount)
        {
            item = PlantingMaterialCatalog.UpgradeLegacy(item);

            if (item == InventoryItemType.None || amount <= 0)
                return false;
            EnsureSlots();
            if (IsTool(item))
            {
                if (HasItem(item, 1))
                    return true;
                int emptySlot = FindEmptySlot();
                if (emptySlot < 0)
                    return false;
                slots[emptySlot].Set(item, 1);
                RefreshSelectedItem(false);
                OnInventoryChanged?.Invoke();
                return true;
            }
            int remaining = amount;
            int maxStack = GetMaxStack(item);
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].itemType != item)
                    continue;
                int space = maxStack - slots[i].amount;
                if (space <= 0)
                    continue;
                int add = Mathf.Min(space, remaining);
                slots[i].amount += add;
                remaining -= add;
            }
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;
                int add = Mathf.Min(maxStack, remaining);
                slots[i].Set(item, add);
                remaining -= add;
            }
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return remaining <= 0;
        }
        public bool ConsumeItem(InventoryItemType item, int amount)
        {
            if (item == InventoryItemType.None || amount <= 0)
                return false;
            if (!HasItem(item, amount))
                return false;
            EnsureSlots();
            int remaining = amount;
            if (selectedItem == item && IsHotbarSlot(selectedSlotIndex))
            {
                InventorySlotData selectedSlot = slots[selectedSlotIndex];
                if (!selectedSlot.IsEmpty && selectedSlot.itemType == item)
                {
                    int take = Mathf.Min(selectedSlot.amount, remaining);
                    selectedSlot.amount -= take;
                    remaining -= take;
                    if (selectedSlot.amount <= 0)
                        selectedSlot.Clear();
                }
            }
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (i == selectedSlotIndex)
                    continue;
                if (slots[i].IsEmpty || slots[i].itemType != item)
                    continue;
                int take = Mathf.Min(slots[i].amount, remaining);
                slots[i].amount -= take;
                remaining -= take;
                if (slots[i].amount <= 0)
                    slots[i].Clear();
            }
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return true;
        }
        public bool DiscardFromSlot(int slotIndex, int amount)
        {
            if (!IsValidSlot(slotIndex) || amount <= 0)
                return false;

            EnsureSlots();
            InventorySlotData slot = slots[slotIndex];
            if (slot == null || slot.IsEmpty)
                return false;

            if (IsTool(slot.itemType))
                return false;

            int take = Mathf.Min(amount, slot.amount);
            slot.amount -= take;
            if (slot.amount <= 0)
                slot.Clear();

            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void MoveOrSwapSlots(int fromIndex, int toIndex)
        {
            if (!IsValidSlot(fromIndex) || !IsValidSlot(toIndex) || fromIndex == toIndex)
                return;
            EnsureSlots();
            InventorySlotData from = slots[fromIndex];
            InventorySlotData to = slots[toIndex];
            if (from == null || from.IsEmpty)
                return;
            if (!to.IsEmpty && to.itemType == from.itemType && !IsTool(from.itemType))
            {
                int maxStack = GetMaxStack(from.itemType);
                int space = maxStack - to.amount;
                if (space > 0)
                {
                    int moved = Mathf.Min(space, from.amount);
                    to.amount += moved;
                    from.amount -= moved;
                    if (from.amount <= 0)
                        from.Clear();
                    RefreshSelectedItem(false);
                    OnInventoryChanged?.Invoke();
                }
                return;
            }
            InventorySlotData temporary =
                new InventorySlotData();
            temporary.CopyFrom(to);
            to.CopyFrom(from);
            from.CopyFrom(temporary);
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
        }
        public void AddMoney(int amount)
        {
            money += Mathf.Max(0, amount);
            OnInventoryChanged?.Invoke();
        }
        public bool SpendMoney(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return true;
            if (money < amount)
                return false;
            money -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }
        public bool CanAddItem(InventoryItemType item, int amount)
        {
            if (item == InventoryItemType.None || amount <= 0)
                return false;
            EnsureSlots();
            if (IsTool(item))
            {
                return HasItem(item, 1) || FindEmptySlot() >= 0;
            }
            int remaining = amount;
            int maxStack = GetMaxStack(item);
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].itemType != item)
                    continue;
                int space = maxStack - slots[i].amount;
                remaining -= Mathf.Max(0, space);
            }
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;
                remaining -= maxStack;
            }
            return remaining <= 0;
        }


        public int SellAll(InventoryItemType item, int centavosEach)
        {
            if (item == InventoryItemType.None)
                return 0;
            EnsureSlots();
            int soldCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty || slots[i].itemType != item)
                    continue;
                soldCount += slots[i].amount;
                slots[i].Clear();
            }
            if (soldCount <= 0)
                return 0;
            money += PesoPrice.TotalPesos(centavosEach, soldCount);
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
            return soldCount;
        }
        public InventorySaveDto CaptureSaveData()
        {
            EnsureSlots();
            InventorySaveDto save = new InventorySaveDto
            {
                selectedSlotIndex = selectedSlotIndex,
                money = money
            };
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotData slot = slots[i];
                save.slots.Add(new InventorySlotSaveDto
                {
                    itemType = slot.itemType.ToString(),
                    amount = slot.amount,
                    liquidMl = slot.liquidMl,
                    sprayerLiquid = slot.sprayerLiquid.ToString()
                });
            }
            return save;
        }
        public void RestoreSaveData(InventorySaveDto save)
        {
            EnsureSlots();
            ClearAllSlots();
            if (save == null)
            {
                BuildStartingInventory();
                selectedSlotIndex = 0;
                money = 0;
                RefreshSelectedItem(false);
                OnInventoryChanged?.Invoke();
                return;
            }
            if (save.slots != null)
            {
                int count = Mathf.Min(slots.Length, save.slots.Count);
                for (int i = 0; i < count; i++)
                {
                    InventorySlotSaveDto source = save.slots[i];
                    if (source == null || source.amount <= 0 ||
                        !Enum.TryParse(source.itemType, out InventoryItemType itemType) ||
                        itemType == InventoryItemType.None)
                    {
                        slots[i].Clear();
                        continue;
                    }
                    slots[i].itemType = PlantingMaterialCatalog.UpgradeLegacy(itemType);
                    slots[i].amount = Mathf.Max(0, source.amount);
                    slots[i].liquidMl = Mathf.Max(0, source.liquidMl);
                    if (!Enum.TryParse(source.sprayerLiquid, out SprayerLiquidType liquid))
                        liquid = SprayerLiquidType.None;
                    slots[i].sprayerLiquid = liquid;
                }
            }
            selectedSlotIndex = Mathf.Clamp(
                save.selectedSlotIndex,
                0,
                Mathf.Max(0, slots.Length - 1)
            );
            money = Mathf.Max(0, save.money);
            RefreshSelectedItem(false);
            OnInventoryChanged?.Invoke();
        }
        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }
        public static bool IsTool(InventoryItemType item)
        {
            return item == InventoryItemType.Machete ||
                   item == InventoryItemType.WateringCan ||
                   item == InventoryItemType.SprayerPump ||
                   item == InventoryItemType.Shovel ||
                   item == InventoryItemType.PruningShears;
        }
        public static int GetMaxStack(InventoryItemType item)
        {
            if (IsTool(item))
                return 1;
            if (IsLiquidItem(item))
                return 1;
            if (item == InventoryItemType.AphidTrap)
                return 99;
            return 999;
        }
    }
}
