using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Routes every AI request through the game's own backend instead of calling
    /// Google directly.
    ///
    /// A Unity build serialises its Inspector values into the shipped package, so
    /// an API key held on a MonoBehaviour can be recovered by unpacking the APK.
    /// The phone now sends only the prompt and its player JWT; the backend holds
    /// the Gemini key, applies the generation settings, and can be switched off
    /// centrally without shipping a new build.
    /// </summary>
    public static class AiBackendClient
    {
        public const string FeatureAdvisor = "ADVISOR";
        public const string FeatureClimateEvaluation = "CLIMATE_EVALUATION";
        public const string FeatureTaskGenerate = "TASK_GENERATE";
        public const string FeatureTaskCheck = "TASK_CHECK";

        /// <summary>Shown if the server is unreachable, matching its own wording.</summary>
        public const string OfflineMessage =
            "Sorry, Antonio is still busy with his farm, please try again later.";

        /// <summary>
        /// Sends one generation request.
        ///
        /// <paramref name="onUnavailable"/> covers the adviser being switched off
        /// server-side, or the server failing to reach Gemini. It carries text
        /// written in the adviser's voice, so callers can show it as a normal
        /// reply rather than surfacing an error to the player.
        /// </summary>
        public static IEnumerator Generate(
            string feature,
            string systemInstruction,
            List<string> userParts,
            Action<string, string> onSuccess,
            Action<string> onUnavailable,
            Action<string> onError)
        {
            if (ApiClient.Instance == null)
            {
                onError?.Invoke("The backend connection is not ready yet.");
                yield break;
            }

            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                onError?.Invoke("You must be logged in to ask the adviser.");
                yield break;
            }

            AiGenerateRequestDto request = new AiGenerateRequestDto
            {
                feature = feature,
                systemInstruction = systemInstruction,
                userParts = userParts
            };

            AiGenerateResponseDto response = null;
            string error = null;

            yield return ApiClient.Instance.PostJson<AiGenerateRequestDto, AiGenerateResponseDto>(
                "/api/ai/generate",
                request,
                AuthSession.Instance.AccessToken,
                value => response = value,
                (message, _) => error = message);

            if (!string.IsNullOrWhiteSpace(error))
            {
                // The server itself is unreachable. Answer in character rather than
                // showing the player a transport error mid-game.
                Debug.LogWarning("[AI] Backend request failed: " + error);
                onUnavailable?.Invoke(OfflineMessage);
                yield break;
            }

            if (response == null)
            {
                onError?.Invoke("The adviser sent back an unreadable response.");
                yield break;
            }

            if (!response.available)
            {
                Debug.Log("[AI] Adviser is switched off server-side.");
                onUnavailable?.Invoke(string.IsNullOrWhiteSpace(response.message)
                    ? OfflineMessage
                    : response.message);
                yield break;
            }

            // An empty body with available=true should not happen - the server turns
            // blank replies into an unavailable response - but if it ever does, the
            // caller would render nothing and the player would see a blank panel
            // with no clue why. Fail loudly instead.
            if (string.IsNullOrWhiteSpace(response.text))
            {
                Debug.LogWarning("[AI] " + feature +
                    " returned no text (finishReason=" + response.finishReason + ").");
                onUnavailable?.Invoke(OfflineMessage);
                yield break;
            }

            Debug.Log("[AI] " + feature + " replied (" + response.text.Length +
                " chars, finishReason=" + response.finishReason + ").");
            onSuccess?.Invoke(response.text, response.finishReason);
        }

        /// <summary>True when the model finished its own sentence rather than being cut off.</summary>
        public static bool Completed(string finishReason)
        {
            return string.Equals(finishReason, "STOP", StringComparison.OrdinalIgnoreCase);
        }
    }
}
