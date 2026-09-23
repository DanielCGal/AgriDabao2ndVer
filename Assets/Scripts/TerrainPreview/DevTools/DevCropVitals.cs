using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Reads and writes one crop's live readings, for the developer tools only.
    ///
    /// Three unrelated classes grow crops - coconut, banana and everything else -
    /// and none of them share a base type, so every caller that wants to touch a
    /// crop generically has to branch across all three. This puts that branch in
    /// one place.
    ///
    /// Only five of the ten readings are editable, and the split is not arbitrary:
    ///
    ///   Drainage, Fertility and Soil Suitability are sampled from the soil once
    ///   in Initialize() and never resampled, so writing them holds for good.
    ///
    ///   Water and Stress are simulated every step, so writing them sets a
    ///   starting point rather than a permanent value - the simulation will drift
    ///   both back toward what the soil and weather dictate. Stress goes through
    ///   DevSetStressBaseline so that it at least does not spring straight back
    ///   from the external-effect channel.
    ///
    ///   Health, Avg Health, Tree Stage and Crop Name are left alone. Health is
    ///   computed from the other five, so forcing it would be overwritten within a
    ///   step or two and would only mislead.
    /// </summary>
    public static class DevCropVitals
    {
        public struct Vitals
        {
            public string cropName;
            /// <summary>0-100.</summary>
            public float stress;
            /// <summary>0-1, shown to the player as a percentage.</summary>
            public float water;
            public float drainage;
            public float fertility;
            public float suitability;
        }

        public static bool TryRead(GameObject cropObject, out Vitals vitals)
        {
            vitals = default;

            if (cropObject == null)
                return false;

            CoconutTreeInstance coconut =
                cropObject.GetComponentInParent<CoconutTreeInstance>();
            if (coconut != null)
            {
                vitals = new Vitals
                {
                    cropName = coconut.cropName,
                    stress = coconut.stress,
                    water = coconut.moisture,
                    drainage = coconut.drainage,
                    fertility = coconut.fertility,
                    suitability = coconut.soilSuitability
                };
                return true;
            }

            BananaPlantInstance banana =
                cropObject.GetComponentInParent<BananaPlantInstance>();
            if (banana != null)
            {
                vitals = new Vitals
                {
                    cropName = banana.cropName,
                    stress = banana.stress,
                    water = banana.moisture,
                    drainage = banana.drainage,
                    fertility = banana.fertility,
                    suitability = banana.soilSuitability
                };
                return true;
            }

            TropicalCropPlantInstance tropical =
                cropObject.GetComponentInParent<TropicalCropPlantInstance>();
            if (tropical != null)
            {
                vitals = new Vitals
                {
                    cropName = tropical.cropName,
                    stress = tropical.stress,
                    water = tropical.moisture,
                    drainage = tropical.drainage,
                    fertility = tropical.fertility,
                    suitability = tropical.soilSuitability
                };
                return true;
            }

            return false;
        }

        public static bool TryApply(GameObject cropObject, Vitals vitals)
        {
            if (cropObject == null)
                return false;

            float water = Mathf.Clamp01(vitals.water);
            float drainage = Mathf.Clamp01(vitals.drainage);
            float fertility = Mathf.Clamp01(vitals.fertility);
            float suitability = Mathf.Clamp01(vitals.suitability);

            CoconutTreeInstance coconut =
                cropObject.GetComponentInParent<CoconutTreeInstance>();
            if (coconut != null)
            {
                coconut.moisture = water;
                coconut.drainage = drainage;
                coconut.fertility = fertility;
                coconut.soilSuitability = suitability;
                coconut.DevSetStressBaseline(vitals.stress);
                return true;
            }

            BananaPlantInstance banana =
                cropObject.GetComponentInParent<BananaPlantInstance>();
            if (banana != null)
            {
                banana.moisture = water;
                banana.drainage = drainage;
                banana.fertility = fertility;
                banana.soilSuitability = suitability;
                banana.DevSetStressBaseline(vitals.stress);
                return true;
            }

            TropicalCropPlantInstance tropical =
                cropObject.GetComponentInParent<TropicalCropPlantInstance>();
            if (tropical != null)
            {
                tropical.moisture = water;
                tropical.drainage = drainage;
                tropical.fertility = fertility;
                tropical.soilSuitability = suitability;
                tropical.DevSetStressBaseline(vitals.stress);
                return true;
            }

            return false;
        }
    }
}
