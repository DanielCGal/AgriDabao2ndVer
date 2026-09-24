using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class AiBackendClient
    {
        public const string FeatureAdvisor = "ADVISOR";
        public const string FeatureClimateEvaluation = "CLIMATE_EVALUATION";
        public const string FeatureTaskGenerate = "TASK_GENERATE";
        public const string FeatureTaskCheck = "TASK_CHECK";

        public const string OfflineMessage =
            "Sorry, Antonio is still busy with his farm, please try again later.";

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

        public static bool Completed(string finishReason)
        {
            return string.Equals(finishReason, "STOP", StringComparison.OrdinalIgnoreCase);
        }
    }
}
