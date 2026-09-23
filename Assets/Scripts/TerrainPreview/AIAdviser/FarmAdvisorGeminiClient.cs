using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Asks the farm adviser a question.
    ///
    /// The Gemini key and model used to live on this component, which meant they
    /// shipped inside the APK and could be recovered by unpacking it. The request
    /// now goes to the game's own backend, which holds the key. The prompt itself
    /// stays here so it remains tunable in the Inspector.
    /// </summary>
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

        /// <param name="onIncomplete">
        /// Raised when the reply was cut off before the model finished - almost
        /// always the token ceiling. The partial text is discarded rather than
        /// shown, since half a piece of farming advice can mislead.
        /// </param>
        public IEnumerator AskAdvisor(string question, string contextJson,
            Action<string> onSuccess, Action<string> onError, Action onIncomplete = null)
        {
            List<string> parts = new List<string>
            {
                "Here is the live game context as JSON.\n" +
                "Use this as the current in-game truth.\n\n" + contextJson,

                // The typed question goes last and goes bare, with none of our own
                // wording wrapped around it.
                //
                // The server quotes this part and writes the framing itself. Doing
                // it there rather than here is the whole point: this file ships
                // inside the APK, so anything written here can be rewritten by
                // someone who unpacks a build, and a guard that can be edited out
                // is not a guard. Adding wording here would also push the real
                // question further from the rules the server puts above it.
                question
            };

            yield return AiBackendClient.Generate(
                AiBackendClient.FeatureAdvisor,
                // Appends the brevity instruction when the player has AI
                // Summarization on, and returns the guidance untouched when off.
                // The tool reference is appended here rather than typed into
                // extraSystemGuidance, because that field is serialized into
                // TerrainPreview.unity - a code-only edit to its default would
                // silently do nothing, and an Inspector edit is one forgotten
                // step away from going stale. Appending keeps it tied to the
                // build. It goes after the guidance so the answer-style rules
                // stay dominant, and before the brevity directive Apply adds.
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
                // The adviser being switched off is a normal answer, not an error,
                // so it is delivered through the success path and reads in-character.
                onUnavailable: message => onSuccess?.Invoke(message),
                onError: onError);
        }
    }
}
