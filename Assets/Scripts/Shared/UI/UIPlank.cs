using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// A wooden plank with words written on it.
    ///
    /// Most signs in the game are pictures with their wording painted in, so the
    /// builders just drop a sprite. The account and recovery screens cannot work
    /// that way - "Change Display Name", a player's own email address, the
    /// heading that has to say a different thing on four different panels - so
    /// they reuse the blank planks the tutorial and trade screens already ship
    /// and print live text on top.
    ///
    /// Shared between the login board and the account panel because both need
    /// exactly this and neither should own it.
    /// </summary>
    public static class UIPlank
    {
        /// <summary>Dark brown, the colour the wood art is outlined in.</summary>
        private static readonly Color TextShadow = new Color(0.12f, 0.07f, 0.02f, 0.95f);

        /// <summary>
        /// A box the plank fills exactly, rather than one it is letterboxed
        /// inside.
        ///
        /// The non-sliced planks are drawn with preserveAspect, which fits the
        /// art inside its box and leaves empty space on two sides when the box is
        /// the wrong shape. Text stretched across that box would then sit off the
        /// wood. Deriving the height from the sprite means the box is always the
        /// plank's own shape and there is no empty space to sit in.
        /// </summary>
        public static Vector2 SizeFor(Sprite sprite, float width, float fallbackHeight)
        {
            if (sprite == null || sprite.rect.height <= 0f)
                return new Vector2(width, fallbackHeight);

            return new Vector2(width, width * sprite.rect.height / sprite.rect.width);
        }

        /// <summary>
        /// A plank carrying one line of text, anchored to the top of its parent
        /// like everything else on these boards.
        /// </summary>
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

        /// <summary>The same plank, pressable.</summary>
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
                // 9-sliced planks keep their rope ends at any size, so the box
                // can be whatever shape the layout wants.
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
            // Inset from the painted rope ends and the plank's rounded edges,
            // proportionally so it holds at every plank size.
            //
            // The vertical inset is the smaller of the two on purpose. Playpen
            // Sans draws a line box about 1.8x its font size - well over half
            // again what the old built-in font needed - so at the largest text
            // setting a generous top-and-bottom margin is what pushes a line out
            // of its plank. There is nothing painted along those edges to avoid
            // anyway; the rope ends are on the left and right.
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
            // Never Truncate: Unity throws away a line that does not fit rather
            // than clipping it, and this font's line box is tall enough that a
            // label at the largest text setting would simply vanish.
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;

            // White reads on the dark tutorial plank but not on the lighter trade
            // plank, and both are used here. The outline carries it on either.
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = TextShadow;
            outline.effectDistance = new Vector2(2f, -2f);

            return label;
        }
    }
}
