using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public static class HudIconButton
    {
        public const float Size = 90f;
        public const float Gap = 12f;
        public const float StartX = 20f;
        public const float TopY = -20f;

        public const int SlotPause = 0;
        public const int SlotAdviserChat = 1;
        public const int SlotShop = 2;
        public const int SlotFarmObjectives = 3;
        public const int SlotSearchPlayers = 4;
        public const int SlotMarketplace = 5;
        public const int SlotSaveFarm = 6;
        public const int SlotSeedlingTent = 7;

        public static Vector2 PositionForSlot(int slot)
        {
            return new Vector2(StartX + slot * (Size + Gap), TopY);
        }

        public static Button Create(
            Transform parent,
            string objectName,
            Sprite art,
            int slot,
            string fallbackLabel,
            UnityAction onClick,
            out Image image,
            out Text label)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Size, Size);
            rect.anchoredPosition = PositionForSlot(slot);

            HudRegistry.RegisterIconButton(slot, go);

            image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                label = null;
                return button;
            }

            image.color = new Color(0.08f, 0.10f, 0.09f, 0.92f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = fallbackLabel;

            return button;
        }
    }
}
