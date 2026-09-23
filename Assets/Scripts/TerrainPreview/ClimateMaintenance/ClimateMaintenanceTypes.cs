using UnityEngine;

namespace AgriDabao3D
{
    public enum CropMaintenanceActionType
    {
        None,
        Mulch,
        OrganicCompost,
        Prune,
        SupportStake,
        Trellis,
        RaisedBed
    }

    public enum ClimateWorldMitigationType
    {
        None,
        IrrigationSystem,
        WaterStorageTank,
        ShadeNet,
        Windbreak,
        Greenhouse,
        DrainageCanal
    }

    public static class ClimateMaintenanceItemCatalog
    {
        public static bool TryGetCropAction(InventoryItemType item, out CropMaintenanceActionType action)
        {
            switch (item)
            {
                case InventoryItemType.MulchBag:
                    action = CropMaintenanceActionType.Mulch;
                    return true;
                case InventoryItemType.OrganicCompostBag:
                    action = CropMaintenanceActionType.OrganicCompost;
                    return true;
                case InventoryItemType.PruningShears:
                    action = CropMaintenanceActionType.Prune;
                    return true;
                case InventoryItemType.SupportStakeKit:
                    action = CropMaintenanceActionType.SupportStake;
                    return true;
                case InventoryItemType.TrellisKit:
                    action = CropMaintenanceActionType.Trellis;
                    return true;
                case InventoryItemType.RaisedBedKit:
                    action = CropMaintenanceActionType.RaisedBed;
                    return true;
                default:
                    action = CropMaintenanceActionType.None;
                    return false;
            }
        }

        public static bool TryGetWorldMitigation(InventoryItemType item, out ClimateWorldMitigationType type)
        {
            switch (item)
            {
                case InventoryItemType.IrrigationSystemKit:
                    type = ClimateWorldMitigationType.IrrigationSystem;
                    return true;
                case InventoryItemType.WaterStorageTankKit:
                    type = ClimateWorldMitigationType.WaterStorageTank;
                    return true;
                case InventoryItemType.ShadeNetKit:
                    type = ClimateWorldMitigationType.ShadeNet;
                    return true;
                case InventoryItemType.WindbreakKit:
                    type = ClimateWorldMitigationType.Windbreak;
                    return true;
                case InventoryItemType.GreenhouseKit:
                    type = ClimateWorldMitigationType.Greenhouse;
                    return true;
                case InventoryItemType.DrainageCanalKit:
                    type = ClimateWorldMitigationType.DrainageCanal;
                    return true;
                default:
                    type = ClimateWorldMitigationType.None;
                    return false;
            }
        }

        public static bool IsClimateMaintenanceItem(InventoryItemType item)
        {
            return TryGetCropAction(item, out _) || TryGetWorldMitigation(item, out _);
        }

        public static bool IsReusableTool(InventoryItemType item)
        {
            return item == InventoryItemType.PruningShears;
        }

        public static string FriendlyName(InventoryItemType item)
        {
            switch (item)
            {
                case InventoryItemType.MulchBag: return "Mulch Bag";
                case InventoryItemType.OrganicCompostBag: return "Organic Compost Bag";
                case InventoryItemType.PruningShears: return "Pruning Shears";
                case InventoryItemType.SupportStakeKit: return "Support Stake Kit";
                case InventoryItemType.TrellisKit: return "Trellis Kit";
                case InventoryItemType.RaisedBedKit: return "Raised Bed Kit";
                case InventoryItemType.IrrigationSystemKit: return "Irrigation System Kit";
                case InventoryItemType.WaterStorageTankKit: return "Water Storage Tank Kit";
                case InventoryItemType.ShadeNetKit: return "Shade Net Kit";
                case InventoryItemType.WindbreakKit: return "Windbreak Kit";
                case InventoryItemType.GreenhouseKit: return "Greenhouse Kit";
                case InventoryItemType.DrainageCanalKit: return "Drainage Canal Kit";
                default: return item.ToString();
            }
        }

        public static string PrefabResourceName(ClimateWorldMitigationType type)
        {
            return "ClimateMaintenance/Prefabs/" + type;
        }

        public static string CropVisualResourceName(CropMaintenanceActionType type)
        {
            return "ClimateMaintenance/Prefabs/Crop_" + type;
        }
    }
}
