using UnityEngine;

namespace AgriDabao3D
{
    public enum FarmCropType
    {
        Coconut,
        Banana,
        Durian,
        Pomelo,
        Cacao,
        Pineapple,
        Mangosteen,
        Mango,
        Corn,
        Eggplant,
        Squash,
        Strawberry,
        Tomato
    }

    public struct CropTemperatureProfile
    {
        public float criticalMin;
        public float idealMin;
        public float idealMax;
        public float criticalMax;

        public CropTemperatureProfile(
            float criticalMin,
            float idealMin,
            float idealMax,
            float criticalMax)
        {
            this.criticalMin = criticalMin;
            this.idealMin = idealMin;
            this.idealMax = idealMax;
            this.criticalMax = criticalMax;
        }
    }

    public static class CropClimateRules
    {
        public static CropTemperatureProfile GetProfile(
            FarmCropType crop)
        {
            return crop switch
            {
                FarmCropType.Coconut =>
                    new CropTemperatureProfile(
                        18f,
                        24f,
                        32f,
                        38f
                    ),

                FarmCropType.Banana =>
                    new CropTemperatureProfile(
                        18f,
                        24f,
                        32f,
                        38f
                    ),

                FarmCropType.Durian =>
                    new CropTemperatureProfile(
                        18f,
                        24f,
                        30f,
                        36f
                    ),

                FarmCropType.Pomelo =>
                    new CropTemperatureProfile(
                        15f,
                        23f,
                        32f,
                        38f
                    ),

                FarmCropType.Cacao =>
                    new CropTemperatureProfile(
                        18f,
                        22f,
                        30f,
                        35f
                    ),

                FarmCropType.Pineapple =>
                    new CropTemperatureProfile(
                        15f,
                        22f,
                        32f,
                        38f
                    ),

                FarmCropType.Mangosteen =>
                    new CropTemperatureProfile(
                        18f,
                        24f,
                        30f,
                        35f
                    ),

                FarmCropType.Mango =>
                    new CropTemperatureProfile(
                        15f,
                        24f,
                        32f,
                        40f
                    ),

                FarmCropType.Corn =>
                    new CropTemperatureProfile(
                        12f,
                        21f,
                        30f,
                        38f
                    ),

                FarmCropType.Eggplant =>
                    new CropTemperatureProfile(
                        15f,
                        22f,
                        30f,
                        38f
                    ),

                FarmCropType.Squash =>
                    new CropTemperatureProfile(
                        15f,
                        21f,
                        30f,
                        38f
                    ),

                FarmCropType.Strawberry =>
                    new CropTemperatureProfile(
                        8f,
                        15f,
                        24f,
                        32f
                    ),

                FarmCropType.Tomato =>
                    new CropTemperatureProfile(
                        10f,
                        18f,
                        28f,
                        35f
                    ),

                _ =>
                    new CropTemperatureProfile(
                        15f,
                        22f,
                        30f,
                        38f
                    )
            };
        }

        public static float GetTemperatureSuitability(
            FarmCropType crop,
            float temperatureC)
        {
            CropTemperatureProfile profile =
                GetProfile(crop);

            // Temperature is inside the ideal range.
            if (temperatureC >= profile.idealMin &&
                temperatureC <= profile.idealMax)
            {
                return 1f;
            }

            // Temperature is too cold.
            if (temperatureC < profile.idealMin)
            {
                return Mathf.Clamp01(
                    Mathf.InverseLerp(
                        profile.criticalMin,
                        profile.idealMin,
                        temperatureC
                    )
                );
            }

            // Temperature is too hot.
            return Mathf.Clamp01(
                1f -
                Mathf.InverseLerp(
                    profile.idealMax,
                    profile.criticalMax,
                    temperatureC
                )
            );
        }

        public static FarmCropType GetFarmCropType(
            TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian =>
                    FarmCropType.Durian,

                TropicalCropKind.Pomelo =>
                    FarmCropType.Pomelo,

                TropicalCropKind.Cacao =>
                    FarmCropType.Cacao,

                TropicalCropKind.Pineapple =>
                    FarmCropType.Pineapple,

                TropicalCropKind.Mangosteen =>
                    FarmCropType.Mangosteen,

                TropicalCropKind.Mango =>
                    FarmCropType.Mango,

                TropicalCropKind.Corn =>
                    FarmCropType.Corn,

                TropicalCropKind.Eggplant =>
                    FarmCropType.Eggplant,

                TropicalCropKind.Squash =>
                    FarmCropType.Squash,

                TropicalCropKind.Strawberry =>
                    FarmCropType.Strawberry,

                TropicalCropKind.Tomato =>
                    FarmCropType.Tomato,

                _ => FarmCropType.Tomato
            };
        }

        public static string GetTemperatureStatus(
            float suitability)
        {
            if (suitability >= 0.90f)
                return "Ideal";

            if (suitability >= 0.65f)
                return "Slight Temperature Stress";

            if (suitability >= 0.35f)
                return "High Temperature Stress";

            return "Critical Temperature Stress";
        }
    }
}