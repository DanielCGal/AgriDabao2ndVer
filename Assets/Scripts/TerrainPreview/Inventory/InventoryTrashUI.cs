using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Throwing things away from the backpack.
    ///
    /// Kept out of InventoryUIBuilder because it is a mode rather than a piece
    /// of furniture: the backpack behaves differently while it is on, and the
    /// two boards it puts up have nothing to do with laying out slots. The
    /// builder owns the button and asks this for the rest.
    ///
    /// Built at runtime out of planks the game already ships, like every other
    /// panel here - there are no UI prefabs to edit.
    /// </summary>
    public class InventoryTrashUI : MonoBehaviour
    {
        // TutorialPromptBoard.png is 1800x600 and carries no 9-slice borders, so
        // it cannot be stretched to an arbitrary box without the wood grain
        // stretching with it. Both boards are therefore drawn at the plank's own
        // 3:1 shape, and the height is derived from the sprite rather than typed
        // here, so replacing the art cannot silently distort it.
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

        /// <summary>The slot being emptied, and how much of it, once chosen.</summary>
        private int pendingSlot = -1;
        private int pendingAmount;
        private InventoryItemType pendingItem = InventoryItemType.None;

        /// <summary>Raised once a slot has actually been emptied, so the backpack can repaint.</summary>
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

        /// <summary>
        /// Starts the flow for one slot: the amount question first when there is
        /// more than one in the pile, otherwise straight to the confirmation.
        /// </summary>
        public void Begin(int slotIndex)
        {
            if (PlayerInventory.Instance == null)
                return;

            InventorySlotData slot = PlayerInventory.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
                return;

            // Tools are the "Owned" entries. They cannot be re-bought as a stack
            // and the farm stops working without them, so they are not trash.
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

        // ------------------------------------------------------------- steps

        private void OnAmountEntered()
        {
            InventorySlotData slot = CurrentSlot();
            if (slot == null)
            {
                Hide();
                return;
            }

            string typed = amountInput.text.Trim();

            // Every rejection re-asks rather than guessing. Silently rounding a
            // mistyped number would throw away a different amount than the
            // player asked for, and that is not recoverable.
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

            // The count sits beside the name exactly when it means something -
            // "delete 3 Mulch Bag" reads oddly for a single one.
            confirmQuestion.text = pendingAmount > 1
                ? "Are you sure you want to delete " + pendingAmount + "x " + name + " ?"
                : "Are you sure you want to delete " + name + " ?";

            ShowOnly(confirmBoard);
        }

        private void OnConfirmYes()
        {
            // Re-read the slot rather than trusting what was there when the board
            // opened. Nothing else should have touched it, but throwing items
            // away is the one action here that cannot be undone.
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

        /// <summary>The slot this flow started on, if it still holds what it did.</summary>
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

        // ---------------------------------------------------------- building

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            root = new GameObject("TrashPrompts", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            // Swallows presses on the slots behind, so a second item cannot be
            // picked while the first is still being confirmed.
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
            // Digits only, and never wider than the largest stack in the game.
            amountInput.contentType = InputField.ContentType.IntegerNumber;
            amountInput.characterLimit = 3;

            CreateBoardButton(amountBoard.transform, "TrashAmountEnter",
                theme?.enterButton, "ENTER", new Vector2(-110f, -212f),
                new Vector2(190f, 62f), OnAmountEntered);

            // A way out, which this board did not have. Enter was the only
            // button on it, so a slot tapped by mistake left the player having
            // to name an amount and then refuse it on the next board - and
            // pressing Enter on an empty box just re-asked, which reads as being
            // stuck rather than as a choice.
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
            // Dark on the pale plank, which is the same choice the trade and
            // logout prompts make on their own boards.
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
            // Never Truncate: Unity drops a whole line that does not fit rather
            // than clipping it, so at the largest text setting the question would
            // simply not be drawn.
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
