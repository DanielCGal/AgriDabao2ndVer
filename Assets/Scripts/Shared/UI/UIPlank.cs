using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public static class UIPlank
    {
        private static readonly Color TextShadow = new Color(0.12f, 0.07f, 0.02f, 0.95f);

        public static Vector2 SizeFor(Sprite sprite, float width, float fallbackHeight)
        {
            if (sprite == null || sprite.rect.height <= 0f)
                return new Vector2(width, fallbackHeight);

            return new Vector2(width, width * sprite.rect.height / sprite.rect.width);
        }

        public static GameObject Create(
            Transform parent,
            string name,
            Sprite sprite,
            bool sliced,
            Vector2 size,
            Vector2 anchoredPosition,
            string text,
            int fontSize,
            out Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            ApplyPlankArt(go.GetComponent<Image>(), sprite, sliced);

            label = CreateLabel(go.transform, text, fontSize, size);
            return go;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            Sprite sprite,
            bool sliced,
            Vector2 size,
            Vector2 anchoredPosition,
            string text,
            int fontSize,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = Create(parent, name, sprite, sliced, size, anchoredPosition,
                text, fontSize, out _);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            return button;
        }

        private static void ApplyPlankArt(Image image, Sprite sprite, bool sliced)
        {
            if (sprite == null)
            {
                image.color = new Color(0.42f, 0.28f, 0.13f, 0.96f);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;

            if (sliced)
            {
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.preserveAspect = true;
            }
        }

        private static Text CreateLabel(Transform parent, string text, int fontSize, Vector2 plankSize)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(plankSize.x * 0.10f, plankSize.y * 0.12f);
            rect.offsetMax = new Vector2(-plankSize.x * 0.10f, -plankSize.y * 0.12f);

            Text label = go.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = TextShadow;
            outline.effectDistance = new Vector2(2f, -2f);

            return label;
        }
    }
}
