using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public enum CropDevelopmentStage
    {
        Any,
        Seedling,
        Vegetative,
        PreFruiting,
        Fruiting,
        Old
    }

    [Serializable]
    public class PestDiseaseRule
    {
        [Header("Identity")]
        public PestDiseaseType type;
        public string displayName;

        public PestDiseaseCategory category;
        public FarmCropType affectedCrop;

        [Header("Season")]
        [Tooltip("Use month numbers: 1 = January, 12 = December.")]
        public List<int> peakMonths = new List<int>();

        public bool canAppearOutsidePeakMonths;

        [Range(0f, 1f)]
        public float offSeasonRiskMultiplier = 0.15f;

        [Header("Crop Stage")]
        public CropDevelopmentStage minimumStage =
            CropDevelopmentStage.Any;

        public CropDevelopmentStage maximumStage =
            CropDevelopmentStage.Old;

        [Header("Climate Requirements")]
        public float minimumTemperatureC = 20f;
        public float maximumTemperatureC = 32f;

        [Range(0f, 100f)]
        public float minimumHumidity;

        public bool prefersWetWeather;
        public bool prefersDryWeather;
        public bool prefersWaterloggedSoil;
        public bool prefersStressedPlants;

        [Header("Appearance and Damage")]
        [Range(0f, 1f)]
        public float baseDailyAppearanceChance = 0.05f;

        public float severityRisePerGameDay = 8f;
        public float healthDamagePerGameDay = 3f;
        public float stressAddedPerGameDay = 4f;

        [Header("Spread")]
        public float spreadRadius = 8f;

        [Range(0f, 1f)]
        public float spreadChancePerDay = 0.05f;

        [Header("Valid Mitigations")]
        public List<PestDiseaseMitigation> mitigations =
            new List<PestDiseaseMitigation>();
    }
}