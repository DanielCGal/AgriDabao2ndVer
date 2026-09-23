using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Grades how well the player handled a typhoon or extreme drought.
    ///
    /// The Gemini key used to sit on this component and therefore shipped inside
    /// the APK. The call now goes through the game's backend, which holds the key
    /// and can switch the adviser off centrally. The evaluation prompt stays here
    /// so it remains tunable in the Inspector.
    /// </summary>
    public class ClimateResilienceGeminiClient : MonoBehaviour
    {
        [Header("Behavior")]
        [TextArea(12, 24)]
        public string extraSystemGuidance =
            "You are an AI climate-resilience evaluator inside a farming simulation set in Davao City. " +
            "The JSON contains a Typhoon or ExtremeDrought event, crop states before and after, and every recorded player action. " +
            "Each action may include itemType, crop, before/after moisture-health-stress-drainage-fertility, whether it succeeded, " +
            "whether the local rules considered it recommended, an effectivenessScore, position, and details. " +
            "Evaluate all actions, including unnecessary pest-control placements, repeated or failed maintenance, and counterproductive watering. " +
            "Recognize appropriate drought actions such as irrigation, water storage, mulch, and shade; and typhoon actions such as drainage canals, " +
            "raised beds, windbreaks, support stakes, trellises, pruning, and greenhouse protection. " +
            "Use precomputedScore as the final score unless the payload clearly contradicts it. " +
            "Respond with 2-3 direct sentences, up to 8 concise bullets, and 2 short closing sentences. Be practical and specific.";

        public IEnumerator EvaluateClimateEvent(string payloadJson,
            Action<string> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                onError?.Invoke("Payload JSON is empty.");
                yield break;
            }

            // Guidance and payload are kept in a single block, exactly as this
            // client composed them before the call moved server-side.
            List<string> parts = new List<string>
            {
                // Brevity is applied to the guidance rather than to the whole
                // block, so it stays ahead of the payload and is never read as
                // part of the JSON being evaluated.
                AiResponseStyle.Apply(extraSystemGuidance) +
                "\n\nEvaluate this climate resilience event JSON:\n" + payloadJson
            };

            yield return AiBackendClient.Generate(
                AiBackendClient.FeatureClimateEvaluation,
                null,
                parts,
                onSuccess: (text, _) => onSuccess?.Invoke(text),
                onUnavailable: message => onSuccess?.Invoke(message),
                onError: onError);
        }
    }
}
