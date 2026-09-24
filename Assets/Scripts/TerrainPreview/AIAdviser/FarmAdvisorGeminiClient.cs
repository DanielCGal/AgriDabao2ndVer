using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class FarmAdvisorGeminiClient : MonoBehaviour
    {
        [Header("Behavior")]
        [TextArea(8, 20)]
        public string extraSystemGuidance =
            "You are an AI farm adviser inside a semi-simulation farming game set in Davao City. " +
            "Always answer using TWO layers: (1) gameplay-aware advice based on the current game state and provided JSON context, and (2) real-world farming guidance when relevant. " +
            "If gameplay differs from real-world agriculture, clearly say that the game is a semi-simulation and explain the difference briefly. " +
            "Base the answer on the player's current soil, weather, district, time, and crop data if available. " +
            "If cropGroups is empty, clearly say there are no crops currently planted nearby and answer using soil, weather, district, and time instead. " +
            "Answer in this exact style: first give a direct answer in 2 to 3 short sentences, then give up to 8 short bullet points only if needed, then end with a short 2-sentence summary or recommendation. " +
            "Do not force bullet points if the question is already answered clearly. " +
            "Keep the tone practical, clear, concise, and specific to the provided game state. " +
            "Do not be overly verbose. Do not repeat the same point in different words. Prefer short paragraphs and short bullets. " +
            "If the player asks about crop condition, mention health, stress, moisture/water, fertility, drainage, and suitability when available.";

        public IEnumerator AskAdvisor(string question, string contextJson,
            Action<string> onSuccess, Action<string> onError, Action onIncomplete = null)
        {
            List<string> parts = new List<string>
            {
                "Here is the live game context as JSON.\n" +
                "Use this as the current in-game truth.\n\n" + contextJson,

                question
            };

            yield return AiBackendClient.Generate(
                AiBackendClient.FeatureAdvisor,
                AiResponseStyle.Apply(
                    extraSystemGuidance + FarmAdvisorToolReference.Block),
                parts,
                onSuccess: (text, finishReason) =>
                {
                    if (!AiBackendClient.Completed(finishReason) && onIncomplete != null)
                    {
                        Debug.LogWarning(
                            "[FarmAdvisor] Reply was cut off (finishReason=" + finishReason + ").");
                        onIncomplete.Invoke();
                        return;
                    }

                    onSuccess?.Invoke(text);
                },
                onUnavailable: message => onSuccess?.Invoke(message),
                onError: onError);
        }
    }
}
