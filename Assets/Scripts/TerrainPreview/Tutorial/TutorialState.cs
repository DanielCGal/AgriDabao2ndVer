using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Everything the beginner guide needs to remember about a farm: whether the
    /// tour has been taken, how far it got, and which three seed kinds this farm
    /// was dealt.
    ///
    /// Static and scene-independent, like <see cref="SelectedAreaState"/>, because
    /// the district is chosen in one scene and used in another. It is written into
    /// the farm snapshot so a player who closes the app mid-tutorial does not lose
    /// their place, and so the seed draw can never change under them.
    /// </summary>
    public static class TutorialState
    {
        /// <summary>Player has finished the tour, or chose to skip it.</summary>
        public static bool Completed;

        /// <summary>Player has already been asked whether they want the tour.</summary>
        public static bool Offered;

        /// <summary>Index into the director's step table; -1 before it starts.</summary>
        public static int CurrentStep = -1;

        /// <summary>The three seed kinds this farm was dealt from its district.</summary>
        public static readonly List<InventoryItemType> StartingSeeds =
            new List<InventoryItemType>();

        /// <summary>
        /// Suppresses the crop info readout until Antonio has introduced it.
        ///
        /// The same panel reports "ground dug successfully" as reports a crop's
        /// health, so without this it pops up during the digging lesson - three
        /// steps before he explains what any of those numbers mean, and while the
        /// player is meant to be looking at the soil.
        /// </summary>
        public static bool CropInspectionLocked;

        /// <summary>
        /// True while the tour is running. Systems that must stay out of the
        /// player's way during it - Save Farm, Antonio's own tasks, Skip Day -
        /// check this rather than each holding their own flag.
        /// </summary>
        public static bool IsRunning => Offered && !Completed;

        /// <summary>
        /// Wipes back to "brand new farm". Called when a farm is created and by the
        /// Dev Tools replay button; NOT called on load, which restores instead.
        /// </summary>
        public static void ResetForNewFarm()
        {
            Completed = false;
            Offered = false;
            CurrentStep = -1;
            StartingSeeds.Clear();
            CropInspectionLocked = false;
        }

        /// <summary>
        /// Deals this farm's seed kinds if it has not been dealt yet. Idempotent on
        /// purpose: it is safe to call from more than one place during start-up,
        /// and a farm that already has seeds keeps them.
        /// </summary>
        public static void EnsureStartingSeedsRolled(string districtName)
        {
            if (StartingSeeds.Count > 0)
                return;

            StartingSeeds.AddRange(DistrictCropPools.RollStartingSeeds(districtName));

            Debug.Log("[Tutorial] " +
                      (string.IsNullOrWhiteSpace(districtName) ? "Unknown district" : districtName) +
                      " starting seeds: " + DistrictCropPools.Describe(StartingSeeds) +
                      " (" + DistrictCropPools.SeedsPerKind + " each).");
        }

        // ------------------------------------------------------------ persistence

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
                // An older save from before the tutorial existed. That farm is
                // already being played, so treat the tour as done rather than
                // ambushing a returning player with it.
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
                    StartingSeeds.Add(seed);
                }
            }
        }
    }
}
