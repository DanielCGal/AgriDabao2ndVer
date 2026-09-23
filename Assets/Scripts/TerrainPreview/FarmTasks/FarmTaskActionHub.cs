using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Routes successful farm actions to the Daily Task and AI Adviser Task systems.
    /// It also removes only cross-source duplicates (for example, one direct action
    /// record plus one state-observer record for the same mulch application).
    /// Repeated actions from the same source are never collapsed.
    /// </summary>
    public static class FarmTaskActionHub
    {
        private sealed class RecentAction
        {
            public ClimateActionRecord record;
            public string semanticFamily;
            public string semanticItem;
            public float time;
        }

        private static readonly List<RecentAction> Recent =
            new List<RecentAction>();

        // The state observer runs on a short interval after the direct interaction.
        // Two seconds is long enough to pair those records without suppressing two
        // legitimate direct clicks, because same-origin records are never deduped.
        private const float CrossSourceDuplicateWindowSeconds = 2f;

        public static void Record(ClimateActionRecord record)
        {
            if (record == null || !record.actionSucceeded)
                return;

            if (string.IsNullOrWhiteSpace(record.recordOrigin))
                record.recordOrigin = "Direct";

            float now = Time.unscaledTime;
            PruneExpired(now);

            string family = GetSemanticFamily(record);
            string item = GetSemanticItem(record, family);

            foreach (RecentAction recent in Recent)
            {
                if (recent == null || recent.record == null)
                    continue;
                if (!AreDifferentSources(record, recent.record))
                    continue;
                if (IsCrossSourceDuplicate(
                        record,
                        family,
                        item,
                        recent.record,
                        recent.semanticFamily,
                        recent.semanticItem))
                {
                    // Keep the direct record when it already exists. If the observer
                    // arrived first, the later direct record is still ignored here;
                    // both contain the same completed action and task progress remains
                    // exactly one. The climate tracker uses the same hub decision.
                    return;
                }
            }

            Recent.Add(new RecentAction
            {
                record = record,
                semanticFamily = family,
                semanticItem = item,
                time = now
            });

            if (DailyTaskSystem.Instance != null)
                DailyTaskSystem.Instance.RecordAction(record);
            if (AIAdvisorTaskSystem.Instance != null)
                AIAdvisorTaskSystem.Instance.RecordAction(record);

            ActionRecorded?.Invoke(record);
        }

        /// <summary>
        /// Raised once for every action that survives de-duplication above.
        ///
        /// The beginner guide listens here rather than hooking each interaction
        /// system separately: digging, planting, watering and mulching all already
        /// arrive at this one point, and they arrive already de-duplicated, so a
        /// single mulch application cannot satisfy a tutorial gate twice.
        /// </summary>
        public static event Action<ClimateActionRecord> ActionRecorded;

        public static void ClearRecent()
        {
            Recent.Clear();
        }

        private static void PruneExpired(float now)
        {
            for (int i = Recent.Count - 1; i >= 0; i--)
            {
                RecentAction value = Recent[i];
                if (value == null ||
                    now - value.time >
                    CrossSourceDuplicateWindowSeconds)
                {
                    Recent.RemoveAt(i);
                }
            }
        }

        private static bool AreDifferentSources(
            ClimateActionRecord first,
            ClimateActionRecord second)
        {
            string firstOrigin = Normalize(first.recordOrigin);
            string secondOrigin = Normalize(second.recordOrigin);
            if (string.IsNullOrEmpty(firstOrigin))
                firstOrigin = "direct";
            if (string.IsNullOrEmpty(secondOrigin))
                secondOrigin = "direct";
            return !string.Equals(
                firstOrigin,
                secondOrigin,
                StringComparison.Ordinal);
        }

        private static bool IsCrossSourceDuplicate(
            ClimateActionRecord first,
            string firstFamily,
            string firstItem,
            ClimateActionRecord second,
            string secondFamily,
            string secondItem)
        {
            if (!string.Equals(
                    firstFamily,
                    secondFamily,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!SameOrUnspecified(first.cropId, second.cropId))
                return false;
            if (!SameOrUnspecified(first.cropType, second.cropType))
                return false;

            // A treatment can have its exact condition only in the observer record.
            // Treat an empty condition as a wildcard, but never merge two different
            // named conditions.
            if (!SameOrUnspecified(
                    first.conditionType,
                    second.conditionType))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(firstItem) &&
                !string.IsNullOrEmpty(secondItem) &&
                !string.Equals(
                    firstItem,
                    secondItem,
                    StringComparison.Ordinal))
            {
                return false;
            }

            // Placement actions without a crop target are paired by item and nearby
            // position. This prevents two separately placed traps from being merged.
            if (string.IsNullOrWhiteSpace(first.cropId) &&
                string.IsNullOrWhiteSpace(second.cropId) &&
                IsPlacementFamily(firstFamily))
            {
                Vector3 a = first.worldPosition.ToVector3();
                Vector3 b = second.worldPosition.ToVector3();
                if (Vector3.Distance(a, b) > 1.25f)
                    return false;
            }

            return true;
        }

        private static bool SameOrUnspecified(
            string first,
            string second)
        {
            string a = Normalize(first);
            string b = Normalize(second);
            return string.IsNullOrEmpty(a) ||
                   string.IsNullOrEmpty(b) ||
                   string.Equals(a, b, StringComparison.Ordinal);
        }

        private static bool IsPlacementFamily(string family)
        {
            return family == "climateplacement" ||
                   family == "pesttrapplacement" ||
                   family == "digspot";
        }

        private static string GetSemanticFamily(
            ClimateActionRecord record)
        {
            string action = Normalize(record.actionType);
            string item = Normalize(
                (record.itemType ?? string.Empty) + " " +
                (record.taskSource ?? string.Empty));

            // Specific placement/protection families must be checked before
            // generic words such as "water" (WaterStorageTank) or "trap".
            if (action.Contains("cleantrap"))
                return "cleantrap";
            if (action.Contains("removeinfected") ||
                action.Contains("removecrop"))
                return "removecrop";

            if (ContainsMaintenance(action) ||
                ContainsMaintenance(item))
                return "maintenance";

            if (ContainsClimateMitigation(action) ||
                ContainsClimateMitigation(item))
                return "climateplacement";

            if (ContainsPestTrap(action) ||
                ContainsPestTrap(item))
                return "pesttrapplacement";

            if (action.Contains("fruitbag") ||
                item.Contains("fruitbag"))
                return "fruitbag";

            if (action.Contains("drainagekit") ||
                action.Contains("drainageimprovement") ||
                item.Contains("drainagekit") ||
                item.Contains("drainageimprovement"))
                return "drainagekit";

            if (action.Contains("spray") ||
                action.Contains("mitigate") ||
                action.Contains("sanitation") ||
                action.Contains("disinfect") ||
                !string.IsNullOrWhiteSpace(record.conditionType) ||
                record.beforeSeverity > record.afterSeverity + 0.01f)
            {
                return "pesttreatment";
            }

            if (action == "watercrop" ||
                action == "water" ||
                action.StartsWith("watercrop",
                    StringComparison.Ordinal))
                return "water";
            if (action.Contains("plantcrop") ||
                action == "plant")
                return "plant";
            if (action.Contains("collectharvest"))
                return "collectharvest";
            if (action.Contains("harvest"))
                return "harvest";
            if (action.Contains("sellcrop") ||
                action == "sell")
                return "sell";
            if (action.Contains("dig"))
                return "digspot";

            return action;
        }

        private static string GetSemanticItem(
            ClimateActionRecord record,
            string family)
        {
            string combined =
                (record.itemType ?? string.Empty) + " " +
                (record.taskSource ?? string.Empty) + " " +
                (record.actionType ?? string.Empty);
            string normalized = Normalize(combined);

            if (family == "maintenance")
            {
                if (normalized.Contains("organiccompost") ||
                    normalized.Contains("compost"))
                    return "organiccompost";
                if (normalized.Contains("supportstake") ||
                    normalized.Contains("stake"))
                    return "supportstake";
                if (normalized.Contains("raisedbed"))
                    return "raisedbed";
                if (normalized.Contains("trellis"))
                    return "trellis";
                if (normalized.Contains("prun"))
                    return "prune";
                if (normalized.Contains("mulch"))
                    return "mulch";
            }

            if (family == "climateplacement")
            {
                if (normalized.Contains("waterstoragetank"))
                    return "waterstoragetank";
                if (normalized.Contains("irrigation"))
                    return "irrigationsystem";
                if (normalized.Contains("shadenet"))
                    return "shadenet";
                if (normalized.Contains("windbreak"))
                    return "windbreak";
                if (normalized.Contains("greenhouse"))
                    return "greenhouse";
                if (normalized.Contains("drainagecanal"))
                    return "drainagecanal";
            }

            if (family == "pesttrapplacement" ||
                family == "cleantrap")
            {
                if (normalized.Contains("aphidtrap"))
                    return "aphidtrap";
                if (normalized.Contains("termitebait"))
                    return "termitebaitstation";
                if (normalized.Contains("pheromone") ||
                    normalized.Contains("baittrap"))
                    return "pheromonetrap";
            }

            if (family == "pesttreatment")
            {
                if (normalized.Contains("neemsoap"))
                    return "neemsoap";
                if (normalized.Contains("btbio") ||
                    normalized.Contains("btbiospray"))
                    return "btbioinsecticide";
                if (normalized.Contains("copperfungicide"))
                    return "copperfungicide";
                if (normalized.Contains("disinfectant"))
                    return "disinfectant";
                if (normalized.Contains("insecticide"))
                    return "insecticide";
                if (normalized.Contains("sanitation") ||
                    normalized.Contains("machete"))
                    return "sanitation";
                if (normalized.Contains("fruitbag"))
                    return "fruitbag";
                if (normalized.Contains("drainage"))
                    return "drainageimprovement";
            }

            if (family == "fruitbag")
                return "fruitbag";
            if (family == "drainagekit")
                return "drainageimprovement";

            return Normalize(record.itemType);
        }

        private static bool ContainsMaintenance(string value)
        {
            return value.Contains("applycropmaintenance") ||
                   value.Contains("usemulch") ||
                   value.Contains("useorganiccompost") ||
                   value.Contains("useprune") ||
                   value.Contains("usesupportstake") ||
                   value.Contains("usetrellis") ||
                   value.Contains("useraisedbed") ||
                   value.Contains("mulchbag") ||
                   value.Contains("organiccompostbag") ||
                   value.Contains("supportstakekit") ||
                   value.Contains("trelliskit") ||
                   value.Contains("raisedbedkit") ||
                   value.Contains("pruningshears");
        }

        private static bool ContainsClimateMitigation(string value)
        {
            return value.Contains("placeclimatemitigation") ||
                   value.Contains("irrigationsystem") ||
                   value.Contains("waterstoragetank") ||
                   value.Contains("shadenet") ||
                   value.Contains("windbreak") ||
                   value.Contains("greenhouse") ||
                   value.Contains("drainagecanal");
        }

        private static bool ContainsPestTrap(string value)
        {
            return value.Contains("placepestmitigationtrap") ||
                   value.Contains("placeaphidtrap") ||
                   value.Contains("aphidtrap") ||
                   value.Contains("pheromonetrap") ||
                   value.Contains("termitebaitstation");
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(char.ToLowerInvariant(character));
            }
            return builder.ToString();
        }
    }
}
