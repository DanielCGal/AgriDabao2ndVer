using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    /// <summary>
    /// What the player could plant in the field today, for the daily tasks and
    /// the adviser's task check.
    ///
    /// A daily task lasts one game day, so "plant a crop" can only be offered for
    /// something that can reach the ground today: a material that goes straight
    /// in, a bought ready seedling, or a seedling already ready in the Seedling
    /// Tent. A seed that still has days to spend in a bag cannot finish the task,
    /// and offering it would leave the day's reward out of reach.
    /// </summary>
    public static class PlantingAvailability
    {
        /// <summary>
        /// How many plantings could be made today among the materials the filter
        /// allows (every material when it is null).
        /// </summary>
        public static int CountPlantableToday(Func<InventoryItemType, bool> allow = null)
        {
            // The ground is read once per question, not once per material: the
            // daily tasks ask this for every template when a new day is drawn.
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

        /// <summary>Ready seedlings in the Seedling Tent that have ground they can go into today.</summary>
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

        /// <summary>Plantings of one crop possible today, from any of its materials.</summary>
        public static int CountPlantableToday(FarmCropType crop)
        {
            return CountPlantableToday(item =>
                PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) && info.Crop == crop);
        }

        /// <summary>
        /// Whether ground for this crop is ready or can be made: an open patch of
        /// the right kind already prepared, or the shovel to prepare one. A
        /// strawberry also needs its bed mulched, so without a mulched bed it needs
        /// a Mulch Bag too.
        /// </summary>
        public static bool CanPrepareGroundFor(FarmCropType crop)
        {
            return GroundReadiness.Read().CanPrepareFor(crop);
        }

        /// <summary>Held materials that can be sown in a seedling bag.</summary>
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

        /// <summary>Seedling bags that are empty or filled but not sown - room to sow.</summary>
        public static int CountFreeBags()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return 0;

            return nursery.CountBags(SeedlingBagStatus.Empty) + nursery.CountBags(SeedlingBagStatus.Filled);
        }

        /// <summary>The open prepared ground on the farm and the tools in hand, read once.</summary>
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
                        // An unmulched bed is ready for a strawberry once mulched.
                        if (needsMulch && bed && mulchBag) return true;
                        break;
                }

                return shovel && (!needsMulch || mulchBag);
            }
        }
    }
}
