using System;
using UnityEngine;

namespace AgriDabao3D
{
    public enum TropicalCropKind
    {
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

    public enum TropicalCropStage
    {
        Seedling,
        Vegetative,
        PreFruiting,
        Fruiting,
        Old
    }

    public enum TropicalHarvestMode
    {
        IndividualFruit,
        BulkBundle
    }

    [Serializable]
    public class TropicalBundleHarvest
    {
        public int itemCount;
        public float producedGameDay;
    }

    public static class TropicalCropCatalog
    {
        public static string GetDisplayName(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Durian => "Durian",
                TropicalCropKind.Pomelo => "Pomelo",
                TropicalCropKind.Cacao => "Cacao",
                TropicalCropKind.Pineapple => "Pineapple",
                TropicalCropKind.Mangosteen => "Mangosteen",
                TropicalCropKind.Mango => "Mango",

                TropicalCropKind.Corn => "Corn",
                TropicalCropKind.Eggplant => "Eggplant",
                TropicalCropKind.Squash => "Squash",
                TropicalCropKind.Strawberry => "Strawberry",
                TropicalCropKind.Tomato => "Tomato",

                _ => "Crop"
            };
        }

        public static TropicalHarvestMode GetHarvestMode(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Mangosteen => TropicalHarvestMode.BulkBundle,
                TropicalCropKind.Mango => TropicalHarvestMode.BulkBundle,
                TropicalCropKind.Tomato => TropicalHarvestMode.BulkBundle,

                _ => TropicalHarvestMode.IndividualFruit
            };
        }

        public static InventoryItemType GetSeedItem(TropicalCropKind crop)
        {
            return PlantingMaterialCatalog.DefaultMaterialFor(CropClimateRules.GetFarmCropType(crop));
        }

        public static InventoryItemType GetFruitItem(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Durian => InventoryItemType.Durian,
                TropicalCropKind.Pomelo => InventoryItemType.Pomelo,
                TropicalCropKind.Cacao => InventoryItemType.Cacao,
                TropicalCropKind.Pineapple => InventoryItemType.Pineapple,
                TropicalCropKind.Mangosteen => InventoryItemType.Mangosteen,
                TropicalCropKind.Mango => InventoryItemType.Mango,

                TropicalCropKind.Corn => InventoryItemType.Corn,
                TropicalCropKind.Eggplant => InventoryItemType.Eggplant,
                TropicalCropKind.Squash => InventoryItemType.Squash,
                TropicalCropKind.Strawberry => InventoryItemType.Strawberry,
                TropicalCropKind.Tomato => InventoryItemType.Tomato,

                _ => InventoryItemType.None
            };
        }

        public static float GetWaterAmount(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Cacao => 0.42f,
                TropicalCropKind.Mangosteen => 0.46f,
                TropicalCropKind.Pineapple => 0.28f,
                TropicalCropKind.Mango => 0.32f,

                TropicalCropKind.Corn => 0.36f,
                TropicalCropKind.Eggplant => 0.40f,
                TropicalCropKind.Squash => 0.42f,
                TropicalCropKind.Strawberry => 0.30f,
                TropicalCropKind.Tomato => 0.40f,

                _ => 0.38f
            };
        }

        public static float GetFirstHarvestDelayDays(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Durian => 170f,
                TropicalCropKind.Pomelo => 128f,
                TropicalCropKind.Cacao => 112f,
                TropicalCropKind.Pineapple => 96f,
                TropicalCropKind.Mangosteen => 182f,
                TropicalCropKind.Mango => 140f,

                TropicalCropKind.Corn => 42f,
                TropicalCropKind.Eggplant => 62f,
                TropicalCropKind.Squash => 55f,
                TropicalCropKind.Strawberry => 50f,
                TropicalCropKind.Tomato => 60f,

                _ => 120f
            };
        }

        public static float GetProductionIntervalDays(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Durian => 60f,
                TropicalCropKind.Pomelo => 46f,
                TropicalCropKind.Cacao => 32f,
                TropicalCropKind.Pineapple => 54f,
                TropicalCropKind.Mangosteen => 58f,
                TropicalCropKind.Mango => 56f,

                TropicalCropKind.Corn => 28f,
                TropicalCropKind.Eggplant => 18f,
                TropicalCropKind.Squash => 24f,
                TropicalCropKind.Strawberry => 16f,
                TropicalCropKind.Tomato => 20f,

                _ => 45f
            };
        }

        public static TropicalCropStage GetStageForAgeDays(TropicalCropKind crop, float livedDays)
        {
            float firstHarvest = GetFirstHarvestDelayDays(crop);

            float oldAge = crop switch
            {
                TropicalCropKind.Pineapple => 520f,
                TropicalCropKind.Cacao => 850f,
                TropicalCropKind.Pomelo => 980f,
                TropicalCropKind.Mango => 1150f,
                TropicalCropKind.Durian => 1250f,
                TropicalCropKind.Mangosteen => 1300f,

                TropicalCropKind.Corn => 220f,
                TropicalCropKind.Eggplant => 260f,
                TropicalCropKind.Squash => 240f,
                TropicalCropKind.Strawberry => 300f,
                TropicalCropKind.Tomato => 280f,

                _ => 900f
            };

            if (livedDays < firstHarvest * 0.22f) return TropicalCropStage.Seedling;
            if (livedDays < firstHarvest * 0.62f) return TropicalCropStage.Vegetative;
            if (livedDays < firstHarvest) return TropicalCropStage.PreFruiting;
            if (livedDays < oldAge) return TropicalCropStage.Fruiting;

            return TropicalCropStage.Old;
        }

        public static float GetIdealMoistureMin(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Cacao => 0.62f,
                TropicalCropKind.Mangosteen => 0.66f,
                TropicalCropKind.Durian => 0.54f,
                TropicalCropKind.Pineapple => 0.38f,
                TropicalCropKind.Mango => 0.42f,
                TropicalCropKind.Pomelo => 0.46f,

                TropicalCropKind.Corn => 0.46f,
                TropicalCropKind.Eggplant => 0.52f,
                TropicalCropKind.Squash => 0.48f,
                TropicalCropKind.Strawberry => 0.50f,
                TropicalCropKind.Tomato => 0.50f,

                _ => 0.50f
            };
        }

        public static float GetIdealMoistureMax(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Cacao => 0.88f,
                TropicalCropKind.Mangosteen => 0.90f,
                TropicalCropKind.Durian => 0.82f,
                TropicalCropKind.Pineapple => 0.72f,
                TropicalCropKind.Mango => 0.76f,
                TropicalCropKind.Pomelo => 0.78f,

                TropicalCropKind.Corn => 0.78f,
                TropicalCropKind.Eggplant => 0.82f,
                TropicalCropKind.Squash => 0.80f,
                TropicalCropKind.Strawberry => 0.74f,
                TropicalCropKind.Tomato => 0.78f,

                _ => 0.80f
            };
        }

        public static float GetIdealDrainage(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Pineapple => 0.78f,
                TropicalCropKind.Mango => 0.78f,
                TropicalCropKind.Pomelo => 0.72f,
                TropicalCropKind.Durian => 0.68f,
                TropicalCropKind.Cacao => 0.56f,
                TropicalCropKind.Mangosteen => 0.58f,

                TropicalCropKind.Corn => 0.68f,
                TropicalCropKind.Eggplant => 0.66f,
                TropicalCropKind.Squash => 0.72f,
                TropicalCropKind.Strawberry => 0.76f,
                TropicalCropKind.Tomato => 0.70f,

                _ => 0.68f
            };
        }

        public static float GetBaseDryRate(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Pineapple => 0.050f,
                TropicalCropKind.Mango => 0.058f,
                TropicalCropKind.Pomelo => 0.064f,
                TropicalCropKind.Durian => 0.070f,
                TropicalCropKind.Cacao => 0.075f,
                TropicalCropKind.Mangosteen => 0.078f,

                TropicalCropKind.Corn => 0.068f,
                TropicalCropKind.Eggplant => 0.071f,
                TropicalCropKind.Squash => 0.063f,
                TropicalCropKind.Strawberry => 0.080f,
                TropicalCropKind.Tomato => 0.070f,

                _ => 0.065f
            };
        }

        public static float GetTyphoonStress(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Mangosteen => 12f,
                TropicalCropKind.Durian => 11f,
                TropicalCropKind.Cacao => 9f,
                TropicalCropKind.Pineapple => 5f,
                TropicalCropKind.Mango => 7f,
                TropicalCropKind.Pomelo => 6f,

                TropicalCropKind.Corn => 10f,
                TropicalCropKind.Eggplant => 8f,
                TropicalCropKind.Squash => 7f,
                TropicalCropKind.Strawberry => 12f,
                TropicalCropKind.Tomato => 10f,

                _ => 7f
            };
        }

        public static float GetDroughtStress(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Cacao => 13f,
                TropicalCropKind.Mangosteen => 14f,
                TropicalCropKind.Durian => 11f,
                TropicalCropKind.Pomelo => 7f,
                TropicalCropKind.Mango => 6f,
                TropicalCropKind.Pineapple => 4f,

                TropicalCropKind.Corn => 12f,
                TropicalCropKind.Eggplant => 9f,
                TropicalCropKind.Squash => 8f,
                TropicalCropKind.Strawberry => 13f,
                TropicalCropKind.Tomato => 10f,

                _ => 8f
            };
        }

        public static float CalculateSoilSuitability(TropicalCropKind crop, SoilSample s)
        {
            float typeScore = GetSoilTypeScore(crop, s.GetVisualSoilType());
            float phScore = GetPhScore(crop, s.phh2o);

            float drainageScore =
                1f - Mathf.Clamp01(Mathf.Abs(SoilGameplayRules.GetDrainage(s) - GetIdealDrainage(crop)) / 0.65f);

            float fertilityScore = SoilGameplayRules.GetFertility(s);
            float compactionScore = 1f - SoilGameplayRules.GetCompactionPenalty(s);

            return Mathf.Clamp01(
                typeScore * 0.30f +
                phScore * 0.20f +
                drainageScore * 0.22f +
                fertilityScore * 0.18f +
                compactionScore * 0.10f
            );
        }

        public static float GetSoilTypeScore(TropicalCropKind crop, string type)
        {
            return crop switch
            {
                TropicalCropKind.Durian => type switch
                {
                    "Loamy" => 0.95f,
                    "Silty" => 0.78f,
                    "Organic-rich" => 0.82f,
                    "Sandy" => 0.58f,
                    "Clayey" => 0.52f,
                    "Rocky" => 0.20f,
                    _ => 0.60f
                },

                TropicalCropKind.Pomelo => type switch
                {
                    "Loamy" => 0.92f,
                    "Sandy" => 0.82f,
                    "Silty" => 0.74f,
                    "Clayey" => 0.48f,
                    "Organic-rich" => 0.70f,
                    "Rocky" => 0.22f,
                    _ => 0.60f
                },

                TropicalCropKind.Cacao => type switch
                {
                    "Organic-rich" => 0.96f,
                    "Loamy" => 0.92f,
                    "Silty" => 0.80f,
                    "Clayey" => 0.62f,
                    "Sandy" => 0.42f,
                    "Rocky" => 0.20f,
                    _ => 0.60f
                },

                TropicalCropKind.Pineapple => type switch
                {
                    "Sandy" => 0.94f,
                    "Loamy" => 0.88f,
                    "Silty" => 0.68f,
                    "Organic-rich" => 0.70f,
                    "Clayey" => 0.34f,
                    "Rocky" => 0.24f,
                    _ => 0.60f
                },

                TropicalCropKind.Mangosteen => type switch
                {
                    "Organic-rich" => 0.96f,
                    "Loamy" => 0.92f,
                    "Silty" => 0.78f,
                    "Clayey" => 0.58f,
                    "Sandy" => 0.36f,
                    "Rocky" => 0.18f,
                    _ => 0.60f
                },

                TropicalCropKind.Mango => type switch
                {
                    "Loamy" => 0.92f,
                    "Sandy" => 0.86f,
                    "Silty" => 0.70f,
                    "Organic-rich" => 0.72f,
                    "Clayey" => 0.45f,
                    "Rocky" => 0.24f,
                    _ => 0.60f
                },

                TropicalCropKind.Corn => type switch
                {
                    "Loamy" => 0.94f,
                    "Silty" => 0.86f,
                    "Sandy" => 0.72f,
                    "Organic-rich" => 0.78f,
                    "Clayey" => 0.50f,
                    "Rocky" => 0.22f,
                    _ => 0.60f
                },

                TropicalCropKind.Eggplant => type switch
                {
                    "Loamy" => 0.94f,
                    "Sandy" => 0.78f,
                    "Silty" => 0.76f,
                    "Organic-rich" => 0.82f,
                    "Clayey" => 0.46f,
                    "Rocky" => 0.20f,
                    _ => 0.60f
                },

                TropicalCropKind.Squash => type switch
                {
                    "Loamy" => 0.92f,
                    "Sandy" => 0.82f,
                    "Silty" => 0.74f,
                    "Organic-rich" => 0.80f,
                    "Clayey" => 0.42f,
                    "Rocky" => 0.22f,
                    _ => 0.60f
                },

                TropicalCropKind.Strawberry => type switch
                {
                    "Loamy" => 0.94f,
                    "Organic-rich" => 0.90f,
                    "Sandy" => 0.72f,
                    "Silty" => 0.70f,
                    "Clayey" => 0.30f,
                    "Rocky" => 0.18f,
                    _ => 0.55f
                },

                TropicalCropKind.Tomato => type switch
                {
                    "Loamy" => 0.95f,
                    "Silty" => 0.80f,
                    "Sandy" => 0.78f,
                    "Organic-rich" => 0.84f,
                    "Clayey" => 0.42f,
                    "Rocky" => 0.20f,
                    _ => 0.60f
                },

                _ => 0.60f
            };
        }

        public static float GetPhScore(TropicalCropKind crop, float ph)
        {
            float ideal = crop switch
            {
                TropicalCropKind.Pineapple => 5.2f,
                TropicalCropKind.Cacao => 6.3f,
                TropicalCropKind.Durian => 6.2f,
                TropicalCropKind.Mangosteen => 6.1f,
                TropicalCropKind.Pomelo => 6.4f,
                TropicalCropKind.Mango => 6.6f,

                TropicalCropKind.Corn => 6.4f,
                TropicalCropKind.Eggplant => 6.2f,
                TropicalCropKind.Squash => 6.3f,
                TropicalCropKind.Strawberry => 5.8f,
                TropicalCropKind.Tomato => 6.4f,

                _ => 6.3f
            };

            float tolerance = crop switch
            {
                TropicalCropKind.Pineapple => 1.4f,
                TropicalCropKind.Mango => 2.2f,
                TropicalCropKind.Pomelo => 2.0f,

                TropicalCropKind.Corn => 1.6f,
                TropicalCropKind.Eggplant => 1.5f,
                TropicalCropKind.Squash => 1.6f,
                TropicalCropKind.Strawberry => 1.0f,
                TropicalCropKind.Tomato => 1.4f,

                _ => 1.8f
            };

            return 1f - Mathf.Clamp01(Mathf.Abs(ph - ideal) / tolerance);
        }

        public static string GetPreferenceSummary(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Durian =>
                    "Prefers deep fertile loam, slightly acidic pH, steady moisture, and protection from strong wind. Sensitive to drought and waterlogging.",

                TropicalCropKind.Pomelo =>
                    "Prefers well-drained loam or sandy loam, moderate moisture, and stable sunny weather. Tolerates short dry spells better than durian/cacao.",

                TropicalCropKind.Cacao =>
                    "Prefers organic-rich loam, high fertility, consistent moisture, and humid conditions. Very drought-sensitive and can suffer under typhoon stress.",

                TropicalCropKind.Pineapple =>
                    "Prefers sandy or loamy acidic soil with strong drainage. More drought-tolerant, but poor under waterlogged clayey soil.",

                TropicalCropKind.Mangosteen =>
                    "Prefers deep organic-rich loam, high moisture, and low stress. Slow-growing and sensitive to drought, wind, and waterlogging.",

                TropicalCropKind.Mango =>
                    "Prefers well-drained loam or sandy loam and can tolerate drier periods. Excess rain or typhoon conditions reduce fruiting quality.",

                TropicalCropKind.Corn =>
                    "Prefers fertile loamy or silty soil, good nitrogen, steady moisture, and warm weather. Very sensitive to drought and fall armyworm damage.",

                TropicalCropKind.Eggplant =>
                    "Prefers fertile well-drained loam with consistent moisture. Sensitive to bacterial wilt, mites, aphids, drought stress, and waterlogging.",

                TropicalCropKind.Squash =>
                    "Prefers loose fertile well-drained soil and moderate moisture. Can handle warm weather but suffers from waterlogging, heavy rain, and pest pressure.",

                TropicalCropKind.Strawberry =>
                    "Prefers cool, well-drained, organic-rich soil with slightly acidic pH. Very sensitive to heat, drought, typhoon rain, mites, and poor drainage.",

                TropicalCropKind.Tomato =>
                    "Prefers fertile well-drained loam, steady moisture, and warm but stable weather. Sensitive to bacterial wilt, drought, waterlogging, and typhoon damage.",

                _ => "General tropical crop preferences."
            };
        }

        public static int CalculateIndividualYield(
        TropicalCropKind crop,
        TropicalCropStage stage,
        float averageHealth,
        float stress)
            {
                float maxYield = crop switch
                {
                    TropicalCropKind.Durian => 5f,
                    TropicalCropKind.Pomelo => 8f,
                    TropicalCropKind.Cacao => 14f,
                    TropicalCropKind.Pineapple => 3f,

                    TropicalCropKind.Corn => 4f,
                    TropicalCropKind.Eggplant => 6f,
                    TropicalCropKind.Squash => 3f,
                    TropicalCropKind.Strawberry => 10f,

                    _ => 6f
                };

                float minYield = crop switch
                {
                    TropicalCropKind.Pineapple => 1f,
                    TropicalCropKind.Durian => 1f,
                    TropicalCropKind.Corn => 1f,
                    TropicalCropKind.Squash => 1f,
                    TropicalCropKind.Strawberry => 3f,
                    TropicalCropKind.Eggplant => 2f,
                    _ => 2f
                };

                float maturityFactor = stage switch
                {
                    TropicalCropStage.PreFruiting => 0.45f,
                    TropicalCropStage.Fruiting => 1.00f,
                    TropicalCropStage.Old => 0.70f,
                    _ => 0f
                };

                float healthFactor = Mathf.Clamp01(averageHealth / 100f);
                float stressFactor = 1f - Mathf.Clamp01(stress / 100f);
                float randomFactor = UnityEngine.Random.Range(0.90f, 1.10f);

                float quality =
                    maturityFactor *
                    Mathf.Lerp(0.55f, 1f, healthFactor) *
                    Mathf.Lerp(0.50f, 1f, stressFactor) *
                    randomFactor;

                return Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(minYield, maxYield, Mathf.Clamp01(quality))),
                    1,
                    Mathf.RoundToInt(maxYield)
                );
            }

        public static int CalculateBulkYield(
        TropicalCropKind crop,
        TropicalCropStage stage,
        float averageHealth,
        float stress)
            {
                float minYield = crop switch
                {
                    TropicalCropKind.Mango => 8f,
                    TropicalCropKind.Tomato => 8f,
                    _ => 6f
                };

                float maxYield = crop switch
                {
                    TropicalCropKind.Mango => 30f,
                    TropicalCropKind.Tomato => 40f,
                    _ => 20f
                };

                float maturityFactor = stage switch
                {
                    TropicalCropStage.PreFruiting => 0.50f,
                    TropicalCropStage.Fruiting => 1.00f,
                    TropicalCropStage.Old => 0.72f,
                    _ => 0f
                };

                float healthFactor = Mathf.Clamp01(averageHealth / 100f);
                float stressFactor = 1f - Mathf.Clamp01(stress / 100f);
                float randomFactor = UnityEngine.Random.Range(0.90f, 1.10f);

                float quality =
                    maturityFactor *
                    Mathf.Lerp(0.60f, 1f, healthFactor) *
                    Mathf.Lerp(0.60f, 1f, stressFactor) *
                    randomFactor;

                return Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(minYield, maxYield, Mathf.Clamp01(quality))),
                    Mathf.RoundToInt(minYield),
                    Mathf.RoundToInt(maxYield)
                );
            }
    }
}
