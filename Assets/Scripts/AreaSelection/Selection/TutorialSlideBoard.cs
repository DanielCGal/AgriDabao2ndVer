using System;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>One page of a picture-and-caption tutorial.</summary>
    [Serializable]
    public class TutorialSlide
    {
        [Tooltip("The screenshot at the top of the page. Leave it empty and a dark panel " +
                 "holds its place, so the page lays out the same with or without it.")]
        public Sprite picture;

        [Tooltip("The words under the picture.")]
        [TextArea(3, 8)]
        public string text;

        public TutorialSlide()
        {
        }

        public TutorialSlide(string text)
        {
            this.text = text;
        }
    }

    /// <summary>
    /// A short picture-book tutorial: one page at a time on the log-ended board,
    /// turned with the >> plank, closing itself after the last page.
    ///
    /// It sits over a dim full-screen layer that swallows every click, so the
    /// screen behind cannot be used while a page is up - otherwise a player could
    /// press ENTER through the page explaining what ENTER does.
    ///
    /// A plain class rather than a MonoBehaviour, the same as DescriptionBoard: it
    /// owns its objects and needs no update loop. It also uses DescriptionBoard's
    /// board art and lettering, so the tutorial reads as part of the same screen
    /// rather than as something bolted on.
    /// </summary>
    public class TutorialSlideBoard
    {
        private const float BoardWidth = 1440f;
        private const float BoardHeight = 900f;

        // Heights measured up from the board's bottom edge.
        private const float ButtonBottom = 42f;
        private const float ButtonHeight = 112f;
        private const float TextBottom = 165f;
        private const float TextTop = 345f;
        private const float PictureBottom = 360f;
        private const float PictureTop = 790f;

        /// <summary>RightButton.png is 400x180, so preserveAspect keeps this true.</summary>
        private const float ButtonWidth = 250f;

        private GameObject root;
        private RectTransform boardRect;
        private Image pictureImage;
        private GameObject picturePlaceholder;
        private Text bodyText;

        private TutorialSlide[] pages;
        private int pageIndex;
        private Action onFinished;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Builds the board hidden. Call <see cref="Show"/> to open it.</summary>
        public static TutorialSlideBoard Create(Transform parent, UIThemeSprites theme)
        {
            var board = new TutorialSlideBoard();
            board.Build(parent, theme);
            return board;
        }

        private void Build(Transform parent, UIThemeSprites theme)
        {
            root = new GameObject("TutorialSlides", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            var boardGo = new GameObject("Board", typeof(RectTransform), typeof(Image));
            boardGo.transform.SetParent(root.transform, false);

            boardRect = boardGo.GetComponent<RectTransform>();
            boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.sizeDelta = new Vector2(BoardWidth, BoardHeight);
            boardRect.anchoredPosition = Vector2.zero;

            Image boardImage = boardGo.GetComponent<Image>();
            Sprite art = theme != null ? theme.tradeConfirmBoard : null;
            if (art != null)
            {
                boardImage.sprite = art;
                boardImage.type = Image.Type.Sliced;
                boardImage.color = Color.white;
            }
            else
            {
                boardImage.color = new Color(0.30f, 0.19f, 0.09f, 0.97f);
            }

            boardImage.raycastTarget = true;

            // How far in from each side the planks start, clear of the painted logs.
            float inset = DescriptionBoard.ContentInset(theme, BoardWidth);

            BuildPicture(boardGo.transform, inset);
            BuildText(boardGo.transform, inset);
            BuildNextButton(boardGo.transform, theme, inset);

            root.SetActive(false);
        }

        private void BuildPicture(Transform board, float inset)
        {
            picturePlaceholder = new GameObject("PicturePlaceholder", typeof(RectTransform), typeof(Image));
            picturePlaceholder.transform.SetParent(board, false);
            PlaceBand(picturePlaceholder.GetComponent<RectTransform>(), inset, PictureBottom, PictureTop);

            Image placeholder = picturePlaceholder.GetComponent<Image>();
            placeholder.color = new Color(0f, 0f, 0f, 0.35f);
            placeholder.raycastTarget = false;

            var pictureGo = new GameObject("Picture", typeof(RectTransform), typeof(Image));
            pictureGo.transform.SetParent(board, false);
            PlaceBand(pictureGo.GetComponent<RectTransform>(), inset, PictureBottom, PictureTop);

            pictureImage = pictureGo.GetComponent<Image>();

            // Screenshots come in whatever shape they were captured at; they are
            // fitted inside the band rather than stretched to it.
            pictureImage.preserveAspect = true;
            pictureImage.raycastTarget = false;
            pictureImage.enabled = false;
        }

        private void BuildText(Transform board, float inset)
        {
            bodyText = DescriptionBoard.CreateLabel(board, 30, BoardWidth - inset * 2f, TextTop - TextBottom);
            PlaceBand(bodyText.rectTransform, inset, TextBottom, TextTop);

            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;

            // The longest page is about three times the length of the shortest, so
            // the size gives way before any line can be cut off.
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 16;
            bodyText.resizeTextMaxSize = 30;
        }

        private void BuildNextButton(Transform board, UIThemeSprites theme, float inset)
        {
            var go = new GameObject("NextPage", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);

            // Bottom right, its right edge against the log, where the mockups put it.
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rect.anchoredPosition = new Vector2(-inset, ButtonBottom);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnNextPressed);

            Sprite art = theme != null ? theme.nextDistrictButton : null;
            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.45f, 0.30f, 0.14f, 0.95f);

            Text label = DescriptionBoard.CreateLabel(go.transform, 30, ButtonWidth, ButtonHeight);
            label.text = ">>";

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// A horizontal band of the board, inset from the logs on both sides and
        /// running between two heights measured up from the board's bottom edge.
        /// </summary>
        private static void PlaceBand(RectTransform rect, float inset, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(inset, bottom);
            rect.offsetMax = new Vector2(-inset, top);
        }

        /// <summary>
        /// Opens on the first page. <paramref name="finished"/> runs once the last
        /// page is turned. With no pages to show it runs straight away, so a
        /// caller waiting on it is never left stuck.
        /// </summary>
        public void Show(TutorialSlide[] slides, Action finished)
        {
            if (root == null || slides == null || slides.Length == 0)
            {
                finished?.Invoke();
                return;
            }

            pages = slides;
            onFinished = finished;
            pageIndex = 0;

            FitToScreen();

            root.SetActive(true);
            root.transform.SetAsLastSibling();

            ShowPage(0);
        }

        public void Hide()
        {
            pages = null;
            onFinished = null;

            if (root != null)
                root.SetActive(false);
        }

        /// <summary>
        /// Shrinks the board on a screen too narrow or short for it, so the >> plank
        /// can never end up off the edge where the player could not press it.
        /// </summary>
        private void FitToScreen()
        {
            RectTransform parentRect = root.transform.parent as RectTransform;
            if (parentRect == null || boardRect == null)
                return;

            float width = parentRect.rect.width;
            float height = parentRect.rect.height;

            // Before the canvas has laid out its size reads as zero; full size is
            // the right answer then, not the smallest allowed.
            if (width < 1f || height < 1f)
            {
                boardRect.localScale = Vector3.one;
                return;
            }

            float fit = Mathf.Min((width - 40f) / BoardWidth, (height - 40f) / BoardHeight);
            float scale = Mathf.Clamp(fit, 0.4f, 1f);
            boardRect.localScale = new Vector3(scale, scale, 1f);
        }

        private void ShowPage(int index)
        {
            TutorialSlide slide = pages[index];
            Sprite picture = slide != null ? slide.picture : null;

            pictureImage.sprite = picture;
            pictureImage.enabled = picture != null;
            picturePlaceholder.SetActive(picture == null);

            bodyText.text = slide != null && slide.text != null ? slide.text : string.Empty;
        }

        private void OnNextPressed()
        {
            if (pages == null)
            {
                Hide();
                return;
            }

            if (pageIndex < pages.Length - 1)
            {
                pageIndex++;
                ShowPage(pageIndex);
                return;
            }

            Action finished = onFinished;
            Hide();
            finished?.Invoke();
        }
    }
}
