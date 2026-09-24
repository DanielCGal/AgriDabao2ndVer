using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class TutorialState
    {
        public static bool Completed;

        public static bool Offered;

        public static int CurrentStep = -1;

        public static readonly List<InventoryItemType> StartingSeeds =
            new List<InventoryItemType>();

        public static bool CropInspectionLocked;

        public static bool IsRunning => Offered && !Completed;

        public static void ResetForNewFarm()
        {
            Completed = false;
            Offered = false;
            CurrentStep = -1;
            StartingSeeds.Clear();
            CropInspectionLocked = false;
        }

        public static void EnsureStartingSeedsRolled(string districtName)
        {
            if (StartingSeeds.Count > 0)
                return;

            StartingSeeds.AddRange(DistrictCropPools.RollStartingSeeds(districtName));

            Debug.Log("[Tutorial] " +
                      (string.IsNullOrWhiteSpace(districtName) ? "Unknown district" : districtName) +
                      " starting materials: " + DistrictCropPools.Describe(StartingSeeds) +
                      " (" + DistrictCropPools.SeedsPerKind + " each).");
        }

        public static TutorialSaveDto Capture()
        {
            TutorialSaveDto dto = new TutorialSaveDto
            {
                completed = Completed,
                offered = Offered,
                currentStep = CurrentStep
            };

            foreach (InventoryItemType seed in StartingSeeds)
                dto.startingSeeds.Add(seed.ToString());

            return dto;
        }

        public static void Restore(TutorialSaveDto dto)
        {
            if (dto == null)
            {
                Completed = true;
                Offered = true;
                CurrentStep = -1;
                StartingSeeds.Clear();
                CropInspectionLocked = false;
                return;
            }

            Completed = dto.completed;
            Offered = dto.offered;
            CurrentStep = dto.currentStep;
            CropInspectionLocked = false;

            StartingSeeds.Clear();
            if (dto.startingSeeds == null)
                return;

            foreach (string name in dto.startingSeeds)
            {
                if (System.Enum.TryParse(name, out InventoryItemType seed) &&
                    seed != InventoryItemType.None)
                {
                    StartingSeeds.Add(PlantingMaterialCatalog.UpgradeLegacy(seed));
                }
            }
        }
    }
}
