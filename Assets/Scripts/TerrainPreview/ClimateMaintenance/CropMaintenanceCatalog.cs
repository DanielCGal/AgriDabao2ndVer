using System.Collections.Generic;

namespace AgriDabao3D
{
    public static class CropMaintenanceCatalog
    {
        private static readonly Dictionary<FarmCropType, HashSet<CropMaintenanceActionType>> Allowed =
            new Dictionary<FarmCropType, HashSet<CropMaintenanceActionType>>
            {
                { FarmCropType.Banana, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake) },
                { FarmCropType.Coconut, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune) },
                { FarmCropType.Cacao, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune) },
                { FarmCropType.Durian, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake) },
                { FarmCropType.Pomelo, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake) },
                { FarmCropType.Mangosteen, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune) },
                { FarmCropType.Mango, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake) },
                { FarmCropType.Pineapple, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.RaisedBed) },
                { FarmCropType.Tomato, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake, CropMaintenanceActionType.RaisedBed) },
                { FarmCropType.Strawberry, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.RaisedBed) },
                { FarmCropType.Squash, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Trellis, CropMaintenanceActionType.RaisedBed) },
                { FarmCropType.Corn, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost) },
                { FarmCropType.Eggplant, Set(CropMaintenanceActionType.Mulch, CropMaintenanceActionType.OrganicCompost, CropMaintenanceActionType.Prune, CropMaintenanceActionType.SupportStake, CropMaintenanceActionType.RaisedBed) }
            };

        private static HashSet<CropMaintenanceActionType> Set(params CropMaintenanceActionType[] actions)
        {
            return new HashSet<CropMaintenanceActionType>(actions);
        }

        public static bool IsAllowed(FarmCropType crop, CropMaintenanceActionType action)
        {
            return Allowed.TryGetValue(crop, out HashSet<CropMaintenanceActionType> actions) && actions.Contains(action);
        }

        public static float ScoreCropAction(WeatherEventType weather, FarmCropType crop, CropMaintenanceActionType action)
        {
            if (!IsAllowed(crop, action)) return -2f;
            if (weather == WeatherEventType.ExtremeDrought)
            {
                switch (action)
                {
                    case CropMaintenanceActionType.Mulch: return 8f;
                    case CropMaintenanceActionType.OrganicCompost: return 3f;
                    case CropMaintenanceActionType.RaisedBed: return 0f;
                    case CropMaintenanceActionType.Prune: return -1f;
                    default: return 1f;
                }
            }
            if (weather == WeatherEventType.Typhoon)
            {
                switch (action)
                {
                    case CropMaintenanceActionType.SupportStake:
                    case CropMaintenanceActionType.Trellis: return 8f;
                    case CropMaintenanceActionType.RaisedBed: return 7f;
                    case CropMaintenanceActionType.Prune: return 5f;
                    case CropMaintenanceActionType.Mulch: return 1f;
                    case CropMaintenanceActionType.OrganicCompost: return 1f;
                    default: return 0f;
                }
            }
            return 0f;
        }

        public static float ScoreWorldAction(WeatherEventType weather, ClimateWorldMitigationType type)
        {
            if (weather == WeatherEventType.ExtremeDrought)
            {
                switch (type)
                {
                    case ClimateWorldMitigationType.IrrigationSystem: return 10f;
                    case ClimateWorldMitigationType.WaterStorageTank: return 8f;
                    case ClimateWorldMitigationType.ShadeNet: return 7f;
                    case ClimateWorldMitigationType.Greenhouse: return 3f;
                    case ClimateWorldMitigationType.Windbreak: return 0f;
                    case ClimateWorldMitigationType.DrainageCanal: return -2f;
                }
            }
            if (weather == WeatherEventType.Typhoon)
            {
                switch (type)
                {
                    case ClimateWorldMitigationType.Windbreak: return 10f;
                    case ClimateWorldMitigationType.DrainageCanal: return 10f;
                    case ClimateWorldMitigationType.Greenhouse: return 8f;
                    case ClimateWorldMitigationType.ShadeNet: return 2f;
                    case ClimateWorldMitigationType.WaterStorageTank: return 1f;
                    case ClimateWorldMitigationType.IrrigationSystem: return -5f;
                }
            }
            return 0f;
        }

        public static string MaintenanceGuide(FarmCropType crop)
        {
            if (!Allowed.TryGetValue(crop, out HashSet<CropMaintenanceActionType> actions)) return "No maintenance profile.";
            return string.Join(", ", actions);
        }
    }
}
