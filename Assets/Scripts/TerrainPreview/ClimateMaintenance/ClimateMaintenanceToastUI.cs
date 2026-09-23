using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class ClimateMaintenanceToastUI : MonoBehaviour
    {
        private GameObject panel;
        private Text label;

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            panel = new GameObject("ClimateMaintenanceToast", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            Sprite plank = UIThemeSprites.Instance?.maintenanceToastPlank;

            rect.sizeDelta = plank != null ? new Vector2(820f, 110f) : new Vector2(760f, 90f);
            // The hotbar occupies y 25..175 from the screen bottom, so at the old 125
            // this toast opened underneath it. 195 clears its top edge by 20.
            rect.anchoredPosition = new Vector2(0f, 195f);

            Image bg = panel.GetComponent<Image>();
            if (plank != null)
            {
                bg.sprite = plank;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0f, 0f, 0f, 0.78f);
            }

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            // Inset past the plank's rope loops so the message stays on the wood.
            // MaintenanceToast.png is 1240x170 drawn at 820x110, which puts the ropes
            // at UI x 50-63 and 765-777; at the old 60 the text began on the left one.
            textRect.offsetMin = new Vector2(plank != null ? 80f : 18f, 10f);
            textRect.offsetMax = new Vector2(plank != null ? -80f : -18f, -10f);
            label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 23;
            label.fontStyle = plank != null ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            // Dark ink reads on light wood; white on the plain fallback.
            // White on both backgrounds. The plank art is mid-brown and the dark
            // brown this used to use on it sank into the wood grain, which is a
            // poor result for the label that carries every failure reason the
            // maintenance system reports.
            label.color = Color.white;
            panel.SetActive(false);
        }

        public void Show(string message)
        {
            if (panel == null || label == null) { Debug.Log("[ClimateMaintenance] " + message); return; }
            label.text = message;
            panel.SetActive(true);
            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), 3.2f);
            Debug.Log("[ClimateMaintenance] " + message);
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
