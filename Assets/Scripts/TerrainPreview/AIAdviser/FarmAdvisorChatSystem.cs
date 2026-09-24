using System.Collections;
using UnityEngine;

namespace AgriDabao3D
{
    public class FarmAdvisorChatSystem : MonoBehaviour
    {
        public FarmAdvisorUIBuilder uiBuilder;
        public FarmAdvisorContextBuilder contextBuilder;
        public FarmAdvisorGeminiClient geminiClient;

        private bool isBusy;

        private IEnumerator Start()
        {
            Debug.Log("[FarmAdvisor] ChatSystem Start()");

            if (uiBuilder == null)
                uiBuilder = Object.FindFirstObjectByType<FarmAdvisorUIBuilder>();

            if (contextBuilder == null)
                contextBuilder = Object.FindFirstObjectByType<FarmAdvisorContextBuilder>();

            if (geminiClient == null)
                geminiClient = Object.FindFirstObjectByType<FarmAdvisorGeminiClient>();

            Debug.Log("[FarmAdvisor] uiBuilder = " + (uiBuilder != null ? uiBuilder.name : "NULL"));
            Debug.Log("[FarmAdvisor] contextBuilder = " + (contextBuilder != null ? contextBuilder.name : "NULL"));
            Debug.Log("[FarmAdvisor] geminiClient = " + (geminiClient != null ? geminiClient.name : "NULL"));

            float timeout = 3f;
            float t = 0f;

            while (t < timeout)
            {
                if (uiBuilder == null)
                    uiBuilder = Object.FindFirstObjectByType<FarmAdvisorUIBuilder>();

                if (uiBuilder != null && uiBuilder.sendButton != null)
                    break;

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (uiBuilder == null || uiBuilder.sendButton == null)
            {
                Debug.LogError("[FarmAdvisor] Send button is still NULL after waiting.");
                yield break;
            }

            uiBuilder.sendButton.onClick.RemoveListener(OnSendPressed);
            uiBuilder.sendButton.onClick.AddListener(OnSendPressed);

            Debug.Log("[FarmAdvisor] Send button listener attached successfully.");
        }

        private void OnSendPressed()
        {
            Debug.Log("[FarmAdvisor] OnSendPressed() fired.");

            if (isBusy)
            {
                Debug.LogWarning("[FarmAdvisor] Request ignored because system is busy.");
                return;
            }

            if (uiBuilder == null || contextBuilder == null || geminiClient == null)
            {
                Debug.LogError("[FarmAdvisor] Missing references.");
                return;
            }

            string question = uiBuilder.GetInputText();
            Debug.Log("[FarmAdvisor] Raw input question: " + question);

            if (string.IsNullOrWhiteSpace(question))
            {
                Debug.LogWarning("[FarmAdvisor] Question is empty.");
                return;
            }

            string contextJson = contextBuilder.BuildContextJson();

            uiBuilder.AppendMessage("Player", question);
            uiBuilder.ClearInput();

            uiBuilder.SetMood(AdvisorMood.Thinking);

            SetBusy(true);

            StartCoroutine(
                geminiClient.AskAdvisor(
                    question,
                    contextJson,
                    onSuccess: reply =>
                    {
                        Debug.Log("[FarmAdvisor] Gemini success callback received.");
                        Debug.Log("[FarmAdvisor] Reply:\n" + reply);

                        uiBuilder.AppendMessage("AI Adviser", CropNaming.Humanize(AiText.StripMarkdown(reply)));
                        uiBuilder.SetMood(AdvisorMood.Teaching);
                        SetBusy(false);
                    },
                    onError: error =>
                    {
                        Debug.LogError("[FarmAdvisor] Gemini error callback received.\n" + error);
                        uiBuilder.AppendMessage("AI Adviser", "Error:\n" + error);
                        uiBuilder.SetMood(AdvisorMood.Teaching);
                        SetBusy(false);
                    },
                    onIncomplete: () =>
                    {
                        Debug.LogWarning("[FarmAdvisor] Reply was incomplete; asking the player to repeat.");
                        uiBuilder.AppendMessage("AI Adviser", "Can you repeat that question again?");
                        uiBuilder.SetMood(AdvisorMood.Teaching);
                        SetBusy(false);
                    }
                )
            );
        }

        private void SetBusy(bool busy)
        {
            isBusy = busy;

            if (uiBuilder != null && uiBuilder.sendButton != null)
                uiBuilder.sendButton.interactable = !busy;
        }
    }
}
