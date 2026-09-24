using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public enum HudPiece
    {
        Joystick,
        JumpButton,
        Hotbar,
        BackpackButton,
        Money,
        WeatherPanel,
        Map,

        CropInfoPanel,
        ShopPanel,
        ObjectivesPanel,
        SearchPlayersPanel,
        MarketplacePanel,
        ConfirmPopup,
        AdviserChatPanel,
        BackpackPanel,
        SeedlingTentPanel
    }

    public static class HudRegistry
    {
        private static readonly Dictionary<HudPiece, GameObject> Pieces =
            new Dictionary<HudPiece, GameObject>();

        private static readonly Dictionary<int, GameObject> IconButtons =
            new Dictionary<int, GameObject>();

        private static bool tutorialControlsVisibility;

        private static readonly HashSet<HudPiece> RevealedPieces = new HashSet<HudPiece>();
        private static readonly HashSet<int> RevealedButtons = new HashSet<int>();

        public static void RegisterPiece(HudPiece piece, GameObject go)
        {
            if (go == null)
                return;

            Pieces[piece] = go;

            if (tutorialControlsVisibility && !IsPanel(piece))
                go.SetActive(RevealedPieces.Contains(piece));
        }

        public static void RegisterIconButton(int slot, GameObject go)
        {
            if (go == null)
                return;

            IconButtons[slot] = go;

            if (tutorialControlsVisibility)
                go.SetActive(RevealedButtons.Contains(slot));
        }

        public static void SetPieceVisible(HudPiece piece, bool visible)
        {
            if (visible)
                RevealedPieces.Add(piece);
            else
                RevealedPieces.Remove(piece);

            if (Pieces.TryGetValue(piece, out GameObject go) && go != null)
                go.SetActive(visible);
        }

        public static void SetIconButtonVisible(int slot, bool visible)
        {
            if (visible)
                RevealedButtons.Add(slot);
            else
                RevealedButtons.Remove(slot);

            if (IconButtons.TryGetValue(slot, out GameObject go) && go != null)
                go.SetActive(visible);
        }

        public static void BeginTutorialControl()
        {
            tutorialControlsVisibility = true;
            RevealedPieces.Clear();
            RevealedButtons.Clear();

            foreach (KeyValuePair<HudPiece, GameObject> entry in Pieces)
            {
                if (entry.Value != null && !IsPanel(entry.Key))
                    entry.Value.SetActive(false);
            }

            foreach (KeyValuePair<int, GameObject> entry in IconButtons)
            {
                if (entry.Value != null)
                    entry.Value.SetActive(false);
            }
        }

        public static void EndTutorialControl()
        {
            tutorialControlsVisibility = false;
            RevealedPieces.Clear();
            RevealedButtons.Clear();

            foreach (KeyValuePair<HudPiece, GameObject> entry in Pieces)
            {
                if (entry.Value != null && !IsPanel(entry.Key))
                    entry.Value.SetActive(true);
            }

            foreach (KeyValuePair<int, GameObject> entry in IconButtons)
            {
                if (entry.Value != null)
                    entry.Value.SetActive(true);
            }
        }

        public static void CloseOtherPanels(HudPiece opening)
        {
            foreach (KeyValuePair<HudPiece, GameObject> entry in Pieces)
            {
                if (entry.Key == opening || !IsPanel(entry.Key))
                    continue;

                if (entry.Value != null && entry.Value.activeSelf)
                    entry.Value.SetActive(false);
            }

            if (opening != HudPiece.Map)
                CollapseMap();
        }

        private static void CollapseMap()
        {
            FarmMapUIBuilder map = Object.FindFirstObjectByType<FarmMapUIBuilder>();
            if (map != null && map.IsExpanded)
                map.Collapse();
        }

        private static bool IsPanel(HudPiece piece)
        {
            switch (piece)
            {
                case HudPiece.CropInfoPanel:
                case HudPiece.ShopPanel:
                case HudPiece.ObjectivesPanel:
                case HudPiece.SearchPlayersPanel:
                case HudPiece.MarketplacePanel:
                case HudPiece.ConfirmPopup:
                case HudPiece.AdviserChatPanel:
                case HudPiece.BackpackPanel:
                case HudPiece.SeedlingTentPanel:
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetPiece(HudPiece piece, out GameObject go)
        {
            go = Pieces.TryGetValue(piece, out GameObject found) && found != null
                ? found
                : null;

            return go != null;
        }

        public static void SetAllVisible(bool visible)
        {
            foreach (KeyValuePair<HudPiece, GameObject> entry in Pieces)
            {
                if (entry.Value != null)
                    entry.Value.SetActive(visible);
            }

            foreach (KeyValuePair<int, GameObject> entry in IconButtons)
            {
                if (entry.Value != null)
                    entry.Value.SetActive(visible);
            }
        }

        public static void ReleaseControl()
        {
            tutorialControlsVisibility = false;
            RevealedPieces.Clear();
            RevealedButtons.Clear();

            PruneDead(Pieces);
            PruneDead(IconButtons);
        }

        private static void PruneDead<TKey>(Dictionary<TKey, GameObject> map)
        {
            List<TKey> dead = null;

            foreach (KeyValuePair<TKey, GameObject> entry in map)
            {
                if (entry.Value != null)
                    continue;

                dead ??= new List<TKey>();
                dead.Add(entry.Key);
            }

            if (dead == null)
                return;

            foreach (TKey key in dead)
                map.Remove(key);
        }
    }
}
