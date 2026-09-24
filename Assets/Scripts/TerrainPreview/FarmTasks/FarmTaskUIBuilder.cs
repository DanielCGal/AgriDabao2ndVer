using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FarmTaskUIBuilder : MonoBehaviour
    {
        private readonly List<Text> dailyTaskTexts = new List<Text>();
        private Text dailyRewardText;
        private Button collectButton;
        private Text collectButtonText;
        private Button skipDayButton;
        private Text aiTaskText;
        private ScrollRect aiTaskScroll;
        private Button giveTaskButton;
        private Button skipTaskButton;
        private Button checkTaskButton;
        private GameObject panel;
        private UIThemeSprites theme;
        private bool themedBook;

        private static readonly Color EnabledGreen =
            new Color(0.05f, 0.56f, 0.22f, 0.98f);
        private static readonly Color DisabledGrey =
            new Color(0.23f, 0.27f, 0.29f, 0.92f);

        private static readonly Color PageInk =
            new Color(0.24f, 0.15f, 0.07f, 1f);

        private const string NoAdviserTaskMessage =
            "Hey! If you want to have a harder Task, just call me! " +
            "Then I'll go to your farm and look around it before I'll give you a task!";

        private const string AdviserInspectingMessage =
            "Antonio is inspecting your farm";

        private void Awake()
        {
            EnsureEventSystem();
            Build();
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (DailyTaskSystem.Instance != null)
            {
                DailyTaskSystem.Instance.OnStateChanged -= Refresh;
                DailyTaskSystem.Instance.OnStateChanged += Refresh;
            }
            if (AIAdvisorTaskSystem.Instance != null)
            {
                AIAdvisorTaskSystem.Instance.OnStateChanged -= Refresh;
                AIAdvisorTaskSystem.Instance.OnStateChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (DailyTaskSystem.Instance != null)
                DailyTaskSystem.Instance.OnStateChanged -= Refresh;
            if (AIAdvisorTaskSystem.Instance != null)
                AIAdvisorTaskSystem.Instance.OnStateChanged -= Refresh;
        }

        public void TogglePanel()
        {
            if (panel != null)
            {
                bool opening = !panel.activeSelf;
                if (opening)
                    HudRegistry.CloseOtherPanels(HudPiece.ObjectivesPanel);

                panel.SetActive(opening);
                if (opening)
                {
                    panel.transform.SetAsLastSibling();
                    Refresh();

                    if (aiTaskScroll != null)
                        aiTaskScroll.verticalNormalizedPosition = 1f;
                }
            }
        }

        private void Build()
        {
            Canvas canvas = FindOrCreateCanvas();
            theme = UIThemeSprites.Instance;
            themedBook = theme != null && theme.objectivesBook != null;

            Transform existing = canvas.transform.Find("FarmTasksPanel");
            if (existing != null)
            {
                panel = existing.gameObject;
                return;
            }

            HudIconButton.Create(
                canvas.transform,
                "FarmObjectivesButton",
                theme?.farmObjectivesButton,
                HudIconButton.SlotFarmObjectives,
                "Objectives",
                TogglePanel,
                out _,
                out _);

            if (themedBook)
            {
                BuildBook(canvas);
                panel.SetActive(false);
                return;
            }

            panel = new GameObject(
                "FarmTasksPanel",
                typeof(RectTransform),
                typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.sizeDelta = new Vector2(500f, 760f);
            panelRect.anchoredPosition = new Vector2(-20f, 0f);
            panel.GetComponent<Image>().color =
                new Color(0.015f, 0.095f, 0.14f, 0.91f);

            Text title = CreateText(
                "Title", panel.transform, 31,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, -40f),
                new Vector2(25f, -70f), new Vector2(-25f, -15f));
            title.text = "FARM OBJECTIVES";

            Text dailyHeader = CreateText(
                "DailyHeader", panel.transform, 25,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(dailyHeader.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero,
                new Vector2(25f, -110f), new Vector2(-25f, -72f));
            dailyHeader.text = "DAILY TASKS - Complete both";

            for (int index = 0; index < 2; index++)
            {
                Text taskText = CreateText(
                    "DailyTask" + (index + 1),
                    panel.transform,
                    20,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft);
                float top = -120f - index * 118f;
                SetRect(taskText.rectTransform,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(0f, 102f),
                    new Vector2(28f, top - 102f),
                    new Vector2(-28f, top));
                taskText.text = "- Waiting for an available task...";
                dailyTaskTexts.Add(taskText);
            }

            dailyRewardText = CreateText(
                "DailyReward", panel.transform, 20,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(dailyRewardText.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero,
                new Vector2(28f, -370f), new Vector2(-28f, -330f));

            collectButton = CreateButton(
                "CollectDailyReward",
                panel.transform,
                "Collect Reward",
                new Vector2(0f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, 1f),
                new Vector2(-34f, 54f),
                new Vector2(26f, -378f));
            collectButtonText = collectButton.transform.Find("Text").GetComponent<Text>();
            collectButton.onClick.AddListener(() =>
                DailyTaskSystem.Instance?.CollectReward());

            skipDayButton = CreateButton(
                "SkipDay",
                panel.transform,
                "Skip to Next Day - 8:00 AM",
                new Vector2(0.5f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(-34f, 54f),
                new Vector2(8f, -378f));
            skipDayButton.onClick.AddListener(() =>
                DailyTaskSystem.Instance?.SkipToNextDayAtEight());

            GameObject divider = new GameObject(
                "Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(panel.transform, false);
            RectTransform dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.offsetMin = new Vector2(25f, -450f);
            dividerRect.offsetMax = new Vector2(-25f, -446f);
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

            Text aiHeader = CreateText(
                "AIHeader", panel.transform, 25,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(aiHeader.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero,
                new Vector2(25f, -500f), new Vector2(-25f, -458f));
            aiHeader.text = "AI-ADVISER TASK";

            ScrollRect aiScroll = CreateScrollArea(panel.transform);
            aiTaskText = aiScroll.content.Find("AITaskText").GetComponent<Text>();

            giveTaskButton = CreateButton(
                "GiveTask",
                panel.transform,
                "Give me a task!",
                new Vector2(0f, 0f),
                new Vector2(0.36f, 0f),
                new Vector2(0f, 0f),
                new Vector2(-14f, 58f),
                new Vector2(25f, 24f));
            giveTaskButton.onClick.AddListener(() =>
                AIAdvisorTaskSystem.Instance?.RequestTask());

            skipTaskButton = CreateButton(
                "SkipTask",
                panel.transform,
                "Skip Task",
                new Vector2(0.36f, 0f),
                new Vector2(0.68f, 0f),
                new Vector2(0f, 0f),
                new Vector2(-13f, 58f),
                new Vector2(8f, 24f));
            skipTaskButton.onClick.AddListener(() =>
                AIAdvisorTaskSystem.Instance?.SkipTask());

            checkTaskButton = CreateButton(
                "CheckTask",
                panel.transform,
                "Check Task",
                new Vector2(0.68f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(-38f, 58f),
                new Vector2(8f, 24f));
            checkTaskButton.onClick.AddListener(() =>
                AIAdvisorTaskSystem.Instance?.CheckTask());

            panel.SetActive(false);
        }

        private void BuildBook(Canvas canvas)
        {
            Vector2 size = theme.objectivesBookSize;

            panel = new GameObject("FarmTasksPanel", typeof(RectTransform), typeof(Image));
            HudRegistry.RegisterPiece(HudPiece.ObjectivesPanel, panel);
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = size;
            panelRect.anchoredPosition = Vector2.zero;

            Image bookImage = panel.GetComponent<Image>();
            bookImage.sprite = theme.objectivesBook;
            bookImage.color = Color.white;
            bookImage.raycastTarget = true;

            float leftX = -size.x * 0.216f;
            float rightX = size.x * 0.2215f;

            float pageW = size.x * 0.39f;
            float textW = size.x * 0.36f;

            CreatePageLabel("DailyLabel", theme?.dailyObjectivesLabel, "Daily Objectives",
                new Vector2(leftX, -size.y * 0.130f), new Vector2(pageW, size.y * 0.135f));
            CreatePageLabel("AntonioLabel", theme?.antonioObjectivesLabel, "Antonio Objectives!",
                new Vector2(rightX, -size.y * 0.130f), new Vector2(pageW, size.y * 0.135f));

            for (int index = 0; index < 2; index++)
            {
                Text taskText = CreatePageText(
                    "DailyTask" + (index + 1), 19, FontStyle.Normal, TextAnchor.UpperLeft,
                    new Vector2(leftX, -size.y * (0.275f + index * 0.166f)),
                    new Vector2(textW, size.y * 0.155f));
                taskText.text = "- Waiting for an available task...";
                dailyTaskTexts.Add(taskText);
            }

            dailyRewardText = CreatePageText(
                "DailyReward", 21, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(leftX, -size.y * 0.596f), new Vector2(textW, size.y * 0.07f));

            collectButton = CreateBookButton("CollectDailyReward", "Collect Reward",
                theme?.collectRewardButton,
                new Vector2(leftX - size.x * 0.094f, -size.y * 0.71f),
                new Vector2(size.x * 0.185f, size.y * 0.10f),
                () => DailyTaskSystem.Instance?.CollectReward());
            collectButtonText = collectButton.transform.Find("Text")?.GetComponent<Text>();

            skipDayButton = CreateBookButton("SkipDay", "Skip Next Day",
                theme?.skipNextDayButton,
                new Vector2(leftX + size.x * 0.094f, -size.y * 0.71f),
                new Vector2(size.x * 0.185f, size.y * 0.10f),
                () => DailyTaskSystem.Instance?.SkipToNextDayAtEight());

            aiTaskText = CreatePageScrollText(
                "AITaskText", 19,
                new Vector2(rightX, -size.y * 0.36f), new Vector2(textW, size.y * 0.30f));
            aiTaskScroll = aiTaskText.GetComponentInParent<ScrollRect>();

            giveTaskButton = CreateBookButton("CallAntonio", "Call Antonio",
                theme?.callAntonioButton,
                new Vector2(rightX - size.x * 0.075f, -size.y * 0.655f),
                new Vector2(size.x * 0.155f, size.y * 0.095f),
                () => AIAdvisorTaskSystem.Instance?.RequestTask());

            skipTaskButton = CreateBookButton("SkipTask", "Skip Task",
                theme?.skipTaskButton,
                new Vector2(rightX + size.x * 0.085f, -size.y * 0.655f),
                new Vector2(size.x * 0.135f, size.y * 0.095f),
                () => AIAdvisorTaskSystem.Instance?.SkipTask());

            checkTaskButton = CreateBookButton("CheckTask", "Check Task",
                theme?.checkTaskButton,
                new Vector2(rightX, -size.y * 0.775f),
                new Vector2(size.x * 0.185f, size.y * 0.105f),
                () => AIAdvisorTaskSystem.Instance?.CheckTask());
        }

        private void CreatePageLabel(string name, Sprite art, string fallback,
            Vector2 anchoredPosition, Vector2 size)
        {
            if (art != null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(panel.transform, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = anchoredPosition;

                Image image = go.GetComponent<Image>();
                image.sprite = art;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text text = CreatePageText(name, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
                anchoredPosition, size);
            text.text = fallback;
        }

        private Text CreatePageText(string name, int fontSize, FontStyle style,
            TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
        {
            Text text = CreateText(name, panel.transform, fontSize, style, alignment);
            text.color = PageInk;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private Text CreatePageScrollText(string name, int fontSize,
            Vector2 anchoredPosition, Vector2 size)
        {
            GameObject scrollGo = new GameObject(
                name + "Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);

            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = size;
            scrollRect.anchoredPosition = anchoredPosition;

            GameObject viewportGo = new GameObject(
                "Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);

            GameObject contentGo = new GameObject(
                "Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text text = CreateText(name, contentGo.transform, fontSize,
                FontStyle.Normal, TextAnchor.UpperLeft);
            text.color = PageInk;
            ContentSizeFitter textFitter =
                text.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.inertia = true;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar = null;
            return text;
        }

        private Button CreateBookButton(string name, string label, Sprite art,
            Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                return button;
            }

            image.color = EnabledGreen;
            Text text = CreateText("Text", go.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private ScrollRect CreateScrollArea(Transform parent)
        {
            GameObject scrollGo = new GameObject(
                "AITaskScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            RectTransform rect = scrollGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(25f, 98f);
            rect.offsetMax = new Vector2(-25f, -510f);
            scrollGo.GetComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.05f);

            GameObject viewportGo = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportGo.GetComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            aiTaskText = CreateText(
                "AITaskText",
                contentGo.transform,
                19,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            aiTaskText.horizontalOverflow = HorizontalWrapMode.Wrap;
            aiTaskText.verticalOverflow = VerticalWrapMode.Overflow;
            ContentSizeFitter textFitter = aiTaskText.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private void Refresh()
        {
            DailyTaskSystem daily = DailyTaskSystem.Instance;
            if (daily != null)
            {
                IReadOnlyList<DailyTaskInstance> tasks = daily.Tasks;
                for (int index = 0; index < dailyTaskTexts.Count; index++)
                {
                    DailyTaskInstance task = tasks != null && index < tasks.Count
                        ? tasks[index]
                        : null;
                    dailyTaskTexts[index].text = FormatDailyTask(index, task);
                }

                dailyRewardText.text = daily.RewardClaimed
                    ? "Reward: Collected"
                    : "Reward: P" + daily.RewardMoney;

                SetButtonState(collectButton, daily.CanCollectReward);
                if (collectButtonText != null)
                {
                    collectButtonText.text = daily.RewardClaimed
                        ? "Collected"
                        : "Collect P" + daily.RewardMoney;
                }
                SetButtonState(skipDayButton, daily.CanSkipDay);
            }

            AIAdvisorTaskSystem ai = AIAdvisorTaskSystem.Instance;
            if (ai != null)
            {
                if (ai.IsBusy)
                    aiTaskText.text = AdviserInspectingMessage;
                else if (!ai.HasActiveTask)
                    aiTaskText.text = NoAdviserTaskMessage;
                else
                    aiTaskText.text = ai.DisplayText;
                SetButtonState(
                    giveTaskButton,
                    !ai.HasActiveTask && !ai.IsBusy && !TutorialState.IsRunning);
                SetButtonState(
                    skipTaskButton,
                    ai.HasActiveTask && !ai.IsBusy);
                SetButtonState(
                    checkTaskButton,
                    ai.HasActiveTask && !ai.IsBusy);
            }
        }

        private static string FormatDailyTask(
            int index,
            DailyTaskInstance task)
        {
            if (task == null)
                return (index + 1) + ". Waiting for an available task...";

            string marker = task.completed ? "[x]" : "[ ]";
            string progress = BuildProgress(task);
            return marker + " " + (index + 1) + ". " + task.title +
                   "\n" + task.description +
                   (string.IsNullOrWhiteSpace(progress)
                       ? string.Empty
                       : "\nProgress: " + progress);
        }

        private static string BuildProgress(DailyTaskInstance task)
        {
            if (task.completed)
                return "Completed";

            DailyTaskKind kind = Enum.TryParse(
                task.kind,
                true,
                out DailyTaskKind parsedKind)
                ? parsedKind
                : DailyTaskKind.None;

            if (kind == DailyTaskKind.WaterCrop ||
                kind == DailyTaskKind.RaiseCropMoisture)
            {
                string actionProgress =
                    Mathf.Max(0, task.progressAmount) +
                    " / " + Mathf.Max(1, task.targetAmount) +
                    " watered";
                if (task.targetThreshold > 0f)
                {
                    actionProgress += "; moisture " +
                        (Mathf.Clamp01(task.currentMetric) * 100f)
                            .ToString("F0") + "% / " +
                        (Mathf.Clamp01(task.targetThreshold) * 100f)
                            .ToString("F0") + "%";
                }
                return actionProgress;
            }

            if (kind == DailyTaskKind.RaiseAverageHealth)
            {
                float gain = Mathf.Max(
                    0f,
                    task.currentMetric - task.baselineMetric);
                return gain.ToString("F1") +
                       " / " + task.requiredReduction.ToString("F1") +
                       " average health gained; actions " +
                       Mathf.Max(0, task.progressAmount) +
                       " / " + Mathf.Max(1, task.targetAmount);
            }

            if (kind == DailyTaskKind.LowerAverageStress)
            {
                float reduction = Mathf.Max(
                    0f,
                    task.baselineMetric - task.currentMetric);
                return reduction.ToString("F1") +
                       " / " + task.requiredReduction.ToString("F1") +
                       " average stress reduced; actions " +
                       Mathf.Max(0, task.progressAmount) +
                       " / " + Mathf.Max(1, task.targetAmount);
            }

            if (kind == DailyTaskKind.MitigateCondition ||
                kind == DailyTaskKind.ReduceAnyPestSeverity ||
                kind == DailyTaskKind.ReduceAnyDiseaseSeverity ||
                kind == DailyTaskKind.ReduceFarmConditionSeverity)
            {
                float reduction = Mathf.Max(
                    0f,
                    task.baselineMetric - task.currentMetric);
                if (task.requiredReduction > 0f)
                {
                    return reduction.ToString("F1") +
                           " / " + task.requiredReduction.ToString("F1") +
                           " severity reduced";
                }
                if (task.targetThreshold > 0f)
                {
                    return "Severity " +
                           task.currentMetric.ToString("F1") +
                           "% / target " +
                           task.targetThreshold.ToString("F1") + "%";
                }
            }

            if (task.targetValue > 0)
                return "P" + Mathf.Max(0, task.progressValue) +
                       " / P" + task.targetValue;
            if (task.targetAmount > 0)
                return Mathf.Max(0, task.progressAmount) +
                       " / " + task.targetAmount;
            if (task.requiredReduction > 0f)
            {
                float reduction = Mathf.Max(
                    0f,
                    task.baselineMetric - task.currentMetric);
                return reduction.ToString("F1") +
                       " / " + task.requiredReduction.ToString("F1") +
                       " reduced";
            }
            if (task.targetThreshold > 0f)
                return task.currentMetric.ToString("F1") +
                       " / " + task.targetThreshold.ToString("F1");
            return string.Empty;
        }

        private void SetButtonState(Button button, bool enabled)
        {
            if (button == null)
                return;

            button.interactable = enabled;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            if (image.sprite != null)
            {
                image.color = enabled
                    ? Color.white
                    : new Color(0.42f, 0.40f, 0.38f, 1f);
                return;
            }

            image.color = enabled ? EnabledGreen : DisabledGrey;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            int fontSize,
            FontStyle style,
            TextAnchor alignment)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            go.GetComponent<Image>().color = EnabledGreen;

            Text text = CreateText(
                "Text", go.transform, 18,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            return go.GetComponent<Button>();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject(
                    "Canvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
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
