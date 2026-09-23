using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Asks the adviser to invent a farm task, and later to grade it.
    ///
    /// Both calls used to go to Gemini directly with a key held on this component,
    /// which shipped inside the APK. They now go through the game's backend, which
    /// holds the key and pins the JSON response format. The prompts stay here so
    /// they remain tunable in the Inspector.
    /// </summary>
    public class AIAdvisorTaskGeminiClient : MonoBehaviour
    {
        [TextArea(10, 24)]
        public string generationGuidance =
            "You are the AI farm adviser in a semi-realistic farming game set in Davao City. " +
            "The player requested one optional multi-day farm task with no deadline. " +
            "Use only the farm data supplied in JSON. The task must be possible from the current farm, " +
            "must take meaningful effort over roughly two or more game days when appropriate, and must not " +
            "require unavailable crops or impossible conditions. Prioritize active drought, typhoon, pest, " +
            "disease, low-moisture, low-health, high-stress, ready-harvest, or excess-inventory situations. " +
            "When you name a specific crop in the dialogue or taskText, always refer to it by its friendly " +
            "cropName (for example corn_1 or cacao_2), never by its cropId. " +
            "Return only strict JSON. Do not use Markdown.";

        [TextArea(10, 24)]
        public string checkingGuidance =
            "You are checking whether a player completed an assigned farm task. " +
            "Use the original task, baseline crop states, current farm state, and all recorded actions. " +
            "Do not approve partial or unrelated work. If incomplete, explain the missing requirement and give " +
            "two practical tips. If complete, clearly confirm it. Return only strict JSON. Do not use Markdown. " +
            // The grader was quoting both, which put a 32-character hex string in
            // the middle of a sentence on a wooden sign. The name is the half the
            // player can act on - it is what they see when they click the crop.
            "Refer to a crop only by its cropName, such as mangosteen_1. Never write its cropId " +
            "GUID and never add an \"(ID: ...)\" note after the name.";

        /// <summary>
        /// How planting works now, added to every task request. Kept in code rather
        /// than in the Inspector text above, because the Inspector copy in the scene
        /// would otherwise keep the old wording.
        /// </summary>
        private const string PlantingRules =
            "\n\nPlanting rules. The planting material decides the route. Seeds of cacao, durian, " +
            "mangosteen, pomelo, tomato and eggplant, banana plantlets and grafted mango seedlings " +
            "are first raised for a few game days in a seedling bag in the Seedling Tent " +
            "(seedlingTent lists the bags), then transplanted. Banana suckers, mango liso, coconut " +
            "seednuts, pineapple suckers, strawberry runners and corn seed go straight into " +
            "prepared ground; squash seed can go either way, and a squash seedling is bought " +
            "ready. Ground is tilled with the shovel, then made into a planting hole (tree crops, " +
            "banana, coconut), a raised bed (tomato, eggplant, squash, pineapple, strawberry - a " +
            "strawberry bed must be mulched first) or a furrow (corn); preparedGround lists " +
            "unplanted patches. For PlantQuantity, name the crop in cropType and any of its " +
            "materials in itemType. Young cacao needs a Shade Net Kit over it.";

        public IEnumerator GenerateTask(
            string farmContextJson,
            Action<AIAdvisorTaskGenerationEnvelope> onSuccess,
            Action<string> onError)
        {
            string schema =
                "{\n" +
                "  \"dialogue\": \"2-4 short adviser sentences\",\n" +
                "  \"taskText\": \"one clear measurable task\",\n" +
                "  \"rewardMoney\": 800,\n" +
                "  \"objective\": {\n" +
                "    \"type\": \"SellCropValue|HarvestQuantity|WaterCrops|ImproveMoisture|ReduceCondition|ReduceFarmSeverity|InstallMitigation|ApplyMaintenance|PlantQuantity|ImproveHealth|ReduceStress\",\n" +
                "    \"cropType\": \"Coconut or empty\",\n" +
                "    \"cropId\": \"the target crop's machine cropId or empty (used only for matching; speak its cropName to the player)\",\n" +
                "    \"conditionType\": \"active condition or empty\",\n" +
                "    \"itemType\": \"required item/action or empty\",\n" +
                "    \"targetAmount\": 0,\n" +
                "    \"targetValue\": 0,\n" +
                "    \"targetThreshold\": 0,\n" +
                "    \"requiredReduction\": 0\n" +
                "  }\n" +
                "}";

            string prompt =
                generationGuidance +
                "\n\nReward rules: choose P800-P2500 based on difficulty and put it in " +
                "rewardMoney only. " +
                // The two fields are described by what they must contain rather
                // than by quoting the sentences the board wraps around them. The
                // wording used to be quoted, to explain what the game would add -
                // and quoting it is what taught the model to write it: the task
                // came back already carrying the opening and the reward line, the
                // board added its own, and the player read both twice.
                "\n\nField rules. dialogue: two to four short sentences of the adviser " +
                "describing what he found on the farm. taskText: the instruction by " +
                "itself, one sentence, beginning with a verb - no greeting, no " +
                "sign-off, and do not begin it with the words 'Now' or 'Do'. " +
                "Never state a peso amount or promise a payment in either field; the " +
                "game prints the reward on its own, and repeating it there makes the " +
                "player read it twice. A peso figure belongs in these fields only " +
                "when it is part of the goal itself, such as selling produce worth " +
                "a given value. " +
                // The friendly name is the point, not a leak: mangosteen_1 is what
                // the player reads when they click that crop, so naming it is how
                // they know which of several mangosteens the task means. The GUID
                // is the thing to keep out of the text.
                "Name a crop by its cropName exactly as given, such as mangosteen_1, " +
                "so the player can tell which crop is meant. Never write the raw " +
                "cropId GUID in either field. " +
                "The objective fields must match the wording and be numerically measurable. " +
                "For SellCropValue, targetValue is the required peso value. " +
                "For HarvestQuantity/PlantQuantity/WaterCrops, use targetAmount. " +
                "For ReduceCondition/ReduceFarmSeverity, use requiredReduction or targetThreshold. " +
                "For InstallMitigation/ApplyMaintenance, set itemType and targetAmount. " +
                PlantingRules +
                "\n\nRequired JSON schema:\n" + schema +
                "\n\nCurrent farm JSON:\n" + farmContextJson;

            yield return Send(
                AiBackendClient.FeatureTaskGenerate,
                prompt,
                raw =>
                {
                    try
                    {
                        JObject value = ParseObject(raw);
                        AIAdvisorTaskGenerationEnvelope result =
                            value.ToObject<AIAdvisorTaskGenerationEnvelope>();
                        if (result == null ||
                            string.IsNullOrWhiteSpace(result.taskText) ||
                            result.objective == null)
                        {
                            onError?.Invoke("The adviser returned an incomplete task.");
                            return;
                        }
                        result.rewardMoney = Mathf.Clamp(result.rewardMoney, 800, 2500);
                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke("Could not parse the AI task: " + ex.Message);
                    }
                },
                onError);
        }

        public IEnumerator CheckTask(
            string checkPayloadJson,
            Action<AIAdvisorTaskCheckEnvelope> onSuccess,
            Action<string> onError)
        {
            string prompt =
                checkingGuidance +
                "\n\nReturn exactly this JSON shape:\n" +
                "{\n" +
                "  \"completed\": false,\n" +
                "  \"reason\": \"specific verdict reason\",\n" +
                "  \"progressSummary\": \"what was actually achieved\",\n" +
                "  \"tips\": [\"specific next step\", \"specific next step\"]\n" +
                "}\n\nTask verification payload:\n" +
                checkPayloadJson;

            yield return Send(
                AiBackendClient.FeatureTaskCheck,
                prompt,
                raw =>
                {
                    try
                    {
                        JObject value = ParseObject(raw);
                        AIAdvisorTaskCheckEnvelope result =
                            value.ToObject<AIAdvisorTaskCheckEnvelope>();
                        if (result == null ||
                            string.IsNullOrWhiteSpace(result.reason))
                        {
                            onError?.Invoke("The adviser returned an incomplete verdict.");
                            return;
                        }
                        if (result.tips == null)
                            result.tips = new List<string>();
                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke("Could not parse the AI verdict: " + ex.Message);
                    }
                },
                onError);
        }

        private static IEnumerator Send(string feature, string prompt,
            Action<string> onRawText, Action<string> onError)
        {
            yield return AiBackendClient.Generate(
                feature,
                null,
                new List<string> { prompt },
                onSuccess: (text, _) => onRawText?.Invoke(text),
                // These two features need parseable JSON, so an unavailable adviser
                // cannot be shown as a task - it has to fail the request instead.
                onUnavailable: message => onError?.Invoke(message),
                onError: onError);
        }

        private static JObject ParseObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "The response was empty.");

            string cleaned = raw.Trim();
            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                int firstNewLine = cleaned.IndexOf('\n');
                int lastFence = cleaned.LastIndexOf(
                    "```", StringComparison.Ordinal);
                if (firstNewLine >= 0 && lastFence > firstNewLine)
                {
                    cleaned = cleaned.Substring(
                        firstNewLine + 1,
                        lastFence - firstNewLine - 1).Trim();
                }
            }

            int start = cleaned.IndexOf('{');
            int end = cleaned.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new InvalidOperationException(
                    "No JSON object was found.");
            return JObject.Parse(
                cleaned.Substring(start, end - start + 1));
        }
    }
}
