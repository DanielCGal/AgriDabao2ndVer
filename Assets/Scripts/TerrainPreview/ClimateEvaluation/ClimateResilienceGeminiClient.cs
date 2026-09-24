using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
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

            List<string> parts = new List<string>
            {
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
