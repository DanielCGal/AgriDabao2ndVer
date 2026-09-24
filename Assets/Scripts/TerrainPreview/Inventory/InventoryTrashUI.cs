using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class InventoryTrashUI : MonoBehaviour
    {
        private const float BoardWidth = 900f;
        private const float BoardFallbackHeight = 300f;

        private UIThemeSprites theme;
        private Canvas canvas;

        private GameObject root;
        private GameObject amountBoard;
        private GameObject confirmBoard;

        private Text amountQuestion;
        private Text confirmQuestion;
        private InputField amountInput;

        private int pendingSlot = -1;
        private int pendingAmount;
        private InventoryItemType pendingItem = InventoryItemType.None;

        public System.Action Discarded;

        public bool IsShowing => root != null && root.activeSelf;

        public static InventoryTrashUI Create(Canvas parentCanvas)
        {
            GameObject go = new GameObject("InventoryTrashUI", typeof(RectTransform));
            InventoryTrashUI trash = go.AddComponent<InventoryTrashUI>();
            trash.canvas = parentCanvas;
            trash.Build();
            return trash;
        }

        public void Begin(int slotIndex)
        {
            if (PlayerInventory.Instance == null)
                return;

            InventorySlotData slot = PlayerInventory.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
                return;

            if (PlayerInventory.IsTool(slot.itemType))
                return;

            pendingSlot = slotIndex;
            pendingItem = slot.itemType;

            root.SetActive(true);
            root.transform.SetAsLastSibling();

            if (slot.amount > 1)
            {
                pendingAmount = 0;
                amountQuestion.text =
                    "How many " + SocialMarketplaceCatalog.FriendlyName(pendingItem) +
                    " would you want to trash?\n(you have " + slot.amount + ")";
                amountInput.text = string.Empty;
                ShowOnly(amountBoard);
                return;
            }

            pendingAmount = 1;
            ShowConfirm();
        }

        public void Hide()
        {
            pendingSlot = -1;
            pendingAmount = 0;
            pendingItem = InventoryItemType.None;
            if (root != null)
                root.SetActive(false);
        }

        private void OnAmountEntered()
        {
            InventorySlotData slot = CurrentSlot();
            if (slot == null)
            {
                Hide();
                return;
            }

            string typed = amountInput.text.Trim();

            if (!int.TryParse(typed, out int amount))
            {
                amountQuestion.text = "Enter a number between 1 and " + slot.amount + ".";
                amountInput.text = string.Empty;
                return;
            }

            if (amount <= 0)
            {
                amountQuestion.text = "Enter at least 1, or press No on the next board to keep it all.";
                amountInput.text = string.Empty;
                return;
            }

            if (amount > slot.amount)
            {
                amountQuestion.text =
                    "You only have " + slot.amount + " " +
                    SocialMarketplaceCatalog.FriendlyName(pendingItem) +
                    ". Enter " + slot.amount + " or fewer.";
                amountInput.text = string.Empty;
                return;
            }

            pendingAmount = amount;
            ShowConfirm();
        }

        private void ShowConfirm()
        {
            string name = SocialMarketplaceCatalog.FriendlyName(pendingItem);

            confirmQuestion.text = pendingAmount > 1
                ? "Are you sure you want to delete " + pendingAmount + "x " + name + " ?"
                : "Are you sure you want to delete " + name + " ?";

            ShowOnly(confirmBoard);
        }

        private void OnConfirmYes()
        {
            InventorySlotData slot = CurrentSlot();
            if (slot != null && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.DiscardFromSlot(
                    pendingSlot, Mathf.Min(pendingAmount, slot.amount));
                Discarded?.Invoke();
            }

            Hide();
        }

        private void OnConfirmNo()
        {
            Hide();
        }

        private InventorySlotData CurrentSlot()
        {
            if (PlayerInventory.Instance == null || pendingSlot < 0)
                return null;

            InventorySlotData slot = PlayerInventory.Instance.GetSlot(pendingSlot);
            if (slot == null || slot.IsEmpty || slot.itemType != pendingItem)
                return null;

            return slot;
        }

        private void ShowOnly(GameObject board)
        {
            amountBoard.SetActive(board == amountBoard);
            confirmBoard.SetActive(board == confirmBoard);
        }

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            root = new GameObject("TrashPrompts", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            blocker.raycastTarget = true;

            BuildAmountBoard();
            BuildConfirmBoard();

            root.SetActive(false);
        }

        private void BuildAmountBoard()
        {
            amountBoard = CreateBoard("TrashAmountBoard");

            amountQuestion = CreateBoardText(amountBoard.transform, 23, new Vector2(0f, -40f), 100f);

            GameObject fieldGo = new GameObject("Amount",
                typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(amountBoard.transform, false);

            RectTransform fieldRect = fieldGo.GetComponent<RectTransform>();
            fieldRect.anchorMin = fieldRect.anchorMax = new Vector2(0.5f, 1f);
            fieldRect.pivot = new Vector2(0.5f, 1f);
            fieldRect.sizeDelta = new Vector2(240f, 52f);
            fieldRect.anchoredPosition = new Vector2(0f, -150f);
            fieldGo.GetComponent<Image>().color = Color.white;

            Text fieldText = CreateText(fieldGo.transform, "Text", 24, TextAnchor.MiddleCenter);
            fieldText.color = Color.black;
            StretchWithPadding(fieldText.rectTransform, 12f, 6f);

            amountInput = fieldGo.GetComponent<InputField>();
            amountInput.textComponent = fieldText;
            amountInput.contentType = InputField.ContentType.IntegerNumber;
            amountInput.characterLimit = 3;

            CreateBoardButton(amountBoard.transform, "TrashAmountEnter",
                theme?.enterButton, "ENTER", new Vector2(-110f, -212f),
                new Vector2(190f, 62f), OnAmountEntered);

            CreateBoardButton(amountBoard.transform, "TrashAmountBack",
                theme?.verifyBackButton, "BACK", new Vector2(110f, -212f),
                new Vector2(190f, 62f), Hide);
        }

        private void BuildConfirmBoard()
        {
            confirmBoard = CreateBoard("TrashConfirmBoard");

            confirmQuestion = CreateBoardText(confirmBoard.transform, 24, new Vector2(0f, -48f), 110f);

            CreateBoardButton(confirmBoard.transform, "TrashYes",
                theme?.confirmYesButton, "YES", new Vector2(-130f, -180f),
                new Vector2(190f, 62f), OnConfirmYes);
            CreateBoardButton(confirmBoard.transform, "TrashNo",
                theme?.confirmNoButton, "NO", new Vector2(130f, -180f),
                new Vector2(190f, 62f), OnConfirmNo);
        }

        private GameObject CreateBoard(string name)
        {
            GameObject board = new GameObject(name, typeof(RectTransform), typeof(Image));
            board.transform.SetParent(root.transform, false);

            RectTransform rect = board.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = UIPlank.SizeFor(
                theme?.tutorialPromptBoard, BoardWidth, BoardFallbackHeight);

            Image image = board.GetComponent<Image>();
            Sprite plank = theme?.tutorialPromptBoard;

            if (plank != null)
            {
                image.sprite = plank;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.42f, 0.28f, 0.13f, 0.98f);
            }

            board.SetActive(false);
            return board;
        }

        private Text CreateBoardText(Transform parent, int size, Vector2 position, float height)
        {
            Text text = CreateText(parent, "Question", size, TextAnchor.UpperCenter);
            text.color = new Color(0.16f, 0.09f, 0.03f, 1f);
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(BoardWidth - 200f, height);
            rect.anchoredPosition = position;
            return text;
        }

        private void CreateBoardButton(Transform parent, string name, Sprite art, string fallbackLabel,
            Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.20f, 0.45f, 0.22f, 0.96f);
            Text label = CreateText(go.transform, "Text", 24, TextAnchor.MiddleCenter);
            label.text = fallbackLabel;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(label.rectTransform);
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
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float x, float y)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }
    }
}
