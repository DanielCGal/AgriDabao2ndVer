using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public static class PlantingAvailability
    {
        public static int CountPlantableToday(Func<InventoryItemType, bool> allow = null)
        {
            GroundReadiness ground = GroundReadiness.Read();
            int count = 0;
            PlayerInventory inventory = PlayerInventory.Instance;

            if (inventory != null)
            {
                foreach (InventoryItemType item in PlantingMaterialCatalog.AllMaterials)
                {
                    if (!PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) ||
                        !info.FieldReady ||
                        (allow != null && !allow(item)) ||
                        !ground.CanPrepareFor(info.Crop))
                    {
                        continue;
                    }

                    count += inventory.GetCount(item);
                }
            }

            NurserySystem nursery = NurserySystem.Instance;
            if (nursery != null)
            {
                for (int slot = 0; slot < NurserySystem.BagCount; slot++)
                {
                    if (nursery.GetStatus(slot) != SeedlingBagStatus.Ready ||
                        !nursery.TryGetMaterial(slot, out PlantingMaterialInfo info) ||
                        (allow != null && !allow(info.Item)) ||
                        !ground.CanPrepareFor(info.Crop))
                    {
                        continue;
                    }

                    count++;
                }
            }

            return count;
        }

        public static int CountTentSeedlingsPlantableToday()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return 0;

            GroundReadiness ground = GroundReadiness.Read();
            int count = 0;
            for (int slot = 0; slot < NurserySystem.BagCount; slot++)
            {
                if (nursery.GetStatus(slot) == SeedlingBagStatus.Ready &&
                    nursery.TryGetMaterial(slot, out PlantingMaterialInfo info) &&
                    ground.CanPrepareFor(info.Crop))
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountPlantableToday(FarmCropType crop)
        {
            return CountPlantableToday(item =>
                PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) && info.Crop == crop);
        }

        public static bool CanPrepareGroundFor(FarmCropType crop)
        {
            return GroundReadiness.Read().CanPrepareFor(crop);
        }

        public static int CountNurseryMaterialsHeld(Func<InventoryItemType, bool> allow = null)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null)
                return 0;

            int count = 0;
            foreach (InventoryItemType item in PlantingMaterialCatalog.AllMaterials)
            {
                if (PlantingMaterialCatalog.IsNurseryMaterial(item) && (allow == null || allow(item)))
                    count += inventory.GetCount(item);
            }

            return count;
        }

        public static int CountFreeBags()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return 0;

            return nursery.CountBags(SeedlingBagStatus.Empty) + nursery.CountBags(SeedlingBagStatus.Filled);
        }

        private struct GroundReadiness
        {
            private bool hole;
            private bool furrow;
            private bool bed;
            private bool mulchedBed;
            private bool shovel;
            private bool mulchBag;

            public static GroundReadiness Read()
            {
                GroundReadiness ground = new GroundReadiness();

                foreach (DigSpot spot in Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
                {
                    if (spot == null || spot.occupied)
                        continue;

                    switch (spot.plotKind)
                    {
                        case PreparedPlotKind.Hole: ground.hole = true; break;
                        case PreparedPlotKind.Furrow: ground.furrow = true; break;
                        case PreparedPlotKind.RaisedBed:
                            ground.bed = true;
                            if (spot.mulched)
                                ground.mulchedBed = true;
                            break;
                    }
                }

                PlayerInventory inventory = PlayerInventory.Instance;
                ground.shovel = inventory != null && inventory.HasItem(InventoryItemType.Shovel, 1);
                ground.mulchBag = inventory != null && inventory.HasItem(InventoryItemType.MulchBag, 1);
                return ground;
            }

            public bool CanPrepareFor(FarmCropType crop)
            {
                bool needsMulch = PlantingMaterialCatalog.NeedsMulchedBed(crop);

                switch (PlantingMaterialCatalog.PlotFor(crop))
                {
                    case PreparedPlotKind.Hole:
                        if (hole) return true;
                        break;
                    case PreparedPlotKind.Furrow:
                        if (furrow) return true;
                        break;
                    case PreparedPlotKind.RaisedBed:
                        if (needsMulch ? mulchedBed : bed) return true;
                        if (needsMulch && bed && mulchBag) return true;
                        break;
                }

                return shovel && (!needsMulch || mulchBag);
            }
        }
    }
}
