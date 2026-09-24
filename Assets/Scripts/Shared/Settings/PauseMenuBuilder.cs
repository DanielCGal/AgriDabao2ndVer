using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class PauseMenuBuilder : MonoBehaviour
    {
        private const string MainMenuScene = "MainMenu";

        public const float CornerButtonSize = 90f;

        private const float PauseContentX = 75f;

        private static readonly Vector2 PauseButtonSize = new Vector2(280f, 76f);

        private Canvas canvas;
        private GameObject pausePanel;
        private GameObject leavePrompt;
        private Text infoText;

        private UIThemeSprites theme;

        private IEnumerator Start()
        {
            for (int i = 0; i < 240; i++)
            {
                if (Object.FindFirstObjectByType<Canvas>() != null)
                {
                    Build();
                    yield break;
                }
                yield return null;
            }

            Debug.LogWarning("PauseMenuBuilder could not find a Canvas.");
        }

        private void Build()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            theme = UIThemeSprites.Instance;

            CreatePauseButton();
            CreatePausePanel();
        }

        private void CreatePauseButton()
        {
            GameObject go = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            HudRegistry.RegisterIconButton(HudIconButton.SlotPause, go);
            go.transform.SetParent(canvas.transform, false);

            Sprite art = theme?.pauseButton;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = art != null
                ? new Vector2(CornerButtonSize, CornerButtonSize)
                : new Vector2(120f, 56f);
            rect.anchoredPosition = new Vector2(20f, -20f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OpenPauseMenu);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.08f, 0.10f, 0.09f, 0.92f);
            Text label = CreateText(go.transform, "Text", 24, TextAnchor.MiddleCenter);
            label.text = "II Pause";
            Stretch(label.rectTransform);
        }

        private void CreatePausePanel()
        {
            GameObject root = new GameObject("PauseMenu_Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.55f);
            blocker.raycastTarget = true;
            pausePanel = root;

            Sprite board = theme?.pauseBoard;

            Vector2 panelSize = board != null ? new Vector2(900f, 618f) : new Vector2(640f, 480f);
            GameObject panel = CreatePanel(root.transform, "PausePanel", panelSize, board);

            if (board != null)
            {
                panel.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(-PauseContentX, 0f);
            }

            if (board == null)
            {
                Text title = CreateText(panel.transform, "Title", 40, TextAnchor.MiddleCenter);
                RectTransform titleRect = title.rectTransform;
                titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(560f, 70f);
                titleRect.anchoredPosition = new Vector2(0f, -30f);
                title.text = "Paused";
            }

            infoText = CreateText(panel.transform, "Info", 24, TextAnchor.MiddleCenter);
            RectTransform infoRect = infoText.rectTransform;
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.sizeDelta = new Vector2(board != null ? 560f : 520f, 100f);
            infoRect.anchoredPosition = new Vector2(board != null ? PauseContentX : 0f,
                board != null ? -80f : -120f);
            infoText.fontStyle = FontStyle.Bold;

            if (board != null)
            {
                CreateButton(panel.transform, "Settings", new Vector2(PauseContentX, -230f), PauseButtonSize,
                    theme?.pauseSettingsButton, new Color(0.12f, 0.55f, 0.20f, 1f), OnSettingsPressed);
                CreateButton(panel.transform, "Main Menu", new Vector2(PauseContentX, -330f), PauseButtonSize,
                    theme?.pauseMainMenuButton, new Color(0.55f, 0.35f, 0.12f, 1f), OnMainMenuPressed);
                CreateButton(panel.transform, "Resume", new Vector2(PauseContentX, -430f), PauseButtonSize,
                    theme?.pauseResumeButton, new Color(0.20f, 0.22f, 0.26f, 1f), ClosePauseMenu);
            }
            else
            {
                CreateButton(panel.transform, "Settings", new Vector2(0f, -270f), new Vector2(300f, 70f),
                    null, new Color(0.12f, 0.55f, 0.20f, 1f), OnSettingsPressed);
                CreateButton(panel.transform, "Main Menu", new Vector2(0f, -360f), new Vector2(300f, 70f),
                    null, new Color(0.55f, 0.35f, 0.12f, 1f), OnMainMenuPressed);
                CreateButton(panel.transform, "Resume", new Vector2(0f, -450f), new Vector2(300f, 60f),
                    null, new Color(0.20f, 0.22f, 0.26f, 1f), ClosePauseMenu);
            }

            BuildLeavePrompt(root.GetComponent<RectTransform>());

            pausePanel.SetActive(false);
        }

        private void BuildLeavePrompt(RectTransform parent)
        {
            Sprite board = theme != null && theme.leavePromptBoard != null
                ? theme.leavePromptBoard
                : theme?.panelBoard;

            leavePrompt = new GameObject("LeavePrompt", typeof(RectTransform), typeof(Image));
            leavePrompt.transform.SetParent(parent, false);
            RectTransform rect = leavePrompt.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = board != null ? new Vector2(820f, 635f) : new Vector2(600f, 280f);
            rect.anchoredPosition = Vector2.zero;

            Image image = leavePrompt.GetComponent<Image>();
            if (board != null)
            {
                image.sprite = board;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.05f, 0.09f, 0.06f, 0.99f);
            }

            Text message = CreateText(leavePrompt.transform, "Message", 26, TextAnchor.MiddleCenter);
            RectTransform msgRect = message.rectTransform;
            msgRect.anchorMin = new Vector2(0f, 1f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.pivot = new Vector2(0.5f, 1f);
            msgRect.sizeDelta = new Vector2(board != null ? -320f : -40f, 150f);
            msgRect.anchoredPosition = new Vector2(0f, board != null ? -95f : -25f);
            message.text = "Save your farm before leaving to the Main Menu?";
            message.fontStyle = FontStyle.Bold;

            if (board != null)
            {
                CreateButton(leavePrompt.transform, "Yes", new Vector2(-130f, -320f), new Vector2(200f, 66f),
                    theme?.yesButton, new Color(0.20f, 0.55f, 0.20f, 1f), OnSaveAndLeave);
                CreateButton(leavePrompt.transform, "No", new Vector2(130f, -320f), new Vector2(200f, 66f),
                    theme?.noButton, new Color(0.55f, 0.16f, 0.16f, 1f), OnLeaveWithoutSaving);
                CreateButton(leavePrompt.transform, "Cancel", new Vector2(0f, -440f), new Vector2(200f, 66f),
                    theme?.cancelButton, new Color(0.25f, 0.27f, 0.31f, 1f), CloseLeavePrompt);
            }
            else
            {
                CreateBottomButton(leavePrompt.transform, "Yes", new Vector2(-170f, 40f),
                    new Color(0.20f, 0.55f, 0.20f, 1f), OnSaveAndLeave);
                CreateBottomButton(leavePrompt.transform, "No", new Vector2(0f, 40f),
                    new Color(0.55f, 0.16f, 0.16f, 1f), OnLeaveWithoutSaving);
                CreateBottomButton(leavePrompt.transform, "Cancel", new Vector2(170f, 40f),
                    new Color(0.25f, 0.27f, 0.31f, 1f), CloseLeavePrompt);
            }

            leavePrompt.SetActive(false);
        }

        private void OpenPauseMenu()
        {
            string name = AuthSession.Instance != null && AuthSession.Instance.CurrentUser != null &&
                          !string.IsNullOrWhiteSpace(AuthSession.Instance.CurrentUser.displayName)
                ? AuthSession.Instance.CurrentUser.displayName
                : "Player";

            string district = string.IsNullOrWhiteSpace(SelectedAreaState.SelectedDistrictName)
                ? "Unknown"
                : SelectedAreaState.SelectedDistrictName;

            infoText.text = "Player: " + name + "\nDistrict: " + district;

            leavePrompt.SetActive(false);
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
        }

        private void ClosePauseMenu()
        {
            pausePanel.SetActive(false);
        }

        private void CloseLeavePrompt()
        {
            leavePrompt.SetActive(false);
        }

        private void OnSettingsPressed()
        {
            if (SettingsUIBuilder.Instance == null)
                new GameObject("SettingsUIBuilder").AddComponent<SettingsUIBuilder>();

            SettingsUIBuilder.Instance.Show();
        }

        private void OnMainMenuPressed()
        {
            leavePrompt.SetActive(true);
            leavePrompt.transform.SetAsLastSibling();
        }

        private void OnSaveAndLeave()
        {
            StartCoroutine(SaveThenLeave());
        }

        private void OnLeaveWithoutSaving()
        {
            SceneManager.LoadScene(MainMenuScene);
        }

        private IEnumerator SaveThenLeave()
        {
            if (FarmPersistenceManager.Instance != null)
                yield return FarmPersistenceManager.Instance.SaveFarm();

            SceneManager.LoadScene(MainMenuScene);
        }

        internal static void ApplySpriteTint(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        private void CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
            Sprite sprite, Color fallbackColor, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;
                ApplySpriteTint(button);
                return;
            }

            image.color = fallbackColor;
            Text text = CreateText(go.transform, "Text", 26, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform);
        }

        private void CreateBottomButton(Transform parent, string label, Vector2 anchoredPos,
            Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(160f, 62f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Button>().onClick.AddListener(action);

            Text text = CreateText(go.transform, "Text", 24, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Sprite board)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            if (board != null)
            {
                image.sprite = board;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.03f, 0.08f, 0.04f, 0.97f);
            }

            return panel;
        }

        private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
