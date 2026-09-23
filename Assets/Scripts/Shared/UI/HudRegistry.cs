using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>The individually hideable pieces of the farm HUD.</summary>
    public enum HudPiece
    {
        Joystick,
        JumpButton,
        Hotbar,
        BackpackButton,
        Money,
        WeatherPanel,
        Map,

        // Panels below are registered so the beginner guide can tell when the
        // player has actually opened and closed one. They are never hidden by the
        // guide - their own buttons already control that.
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

    /// <summary>
    /// A lookup of the farm HUD's parts, so the beginner guide can reveal them one
    /// at a time instead of dropping the whole interface on a new player at once.
    ///
    /// The alternative was a public Show/Hide method on each of the eight builders
    /// that create HUD elements. This is one line per element instead, added where
    /// the element is already being built, and it changes no existing behaviour -
    /// an unregistered or unrevealed piece simply stays as the builder left it.
    ///
    /// Everything is looked up defensively. Unity objects become "fake null" when
    /// their scene unloads, so a stale entry from a previous farm is treated as
    /// absent rather than throwing.
    /// </summary>
    public static class HudRegistry
    {
        private static readonly Dictionary<HudPiece, GameObject> Pieces =
            new Dictionary<HudPiece, GameObject>();

        private static readonly Dictionary<int, GameObject> IconButtons =
            new Dictionary<int, GameObject>();

        /// <summary>
        /// While true, the beginner guide owns HUD visibility: anything not yet
        /// revealed is hidden, including pieces that register later.
        ///
        /// This matters because the builders do not all finish at the same time.
        /// Hiding once, at one moment, only ever hid whatever happened to exist by
        /// then - the joystick, the jump button and any button built a frame later
        /// simply stayed on screen. Recording the intent instead, and applying it
        /// at registration, makes the order irrelevant.
        /// </summary>
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

        /// <summary>Registers one of the round buttons, keyed by its HudIconButton slot.</summary>
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

        /// <summary>
        /// Hands visibility to the beginner guide. Everything currently registered
        /// is hidden, and everything registered from now on is hidden as it arrives,
        /// until the guide reveals it by name.
        /// </summary>
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

        /// <summary>Gives visibility back to the game and shows the full HUD.</summary>
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

        /// <summary>
        /// Closes every other full-screen panel, so only one is ever open.
        ///
        /// Each panel used to be responsible only for itself, which meant tapping a
        /// second HUD button stacked its panel on top of the first instead of
        /// replacing it. Every panel now calls this as it opens and the previous
        /// one steps aside.
        ///
        /// The map is a special case: it is a permanent part of the HUD in its
        /// small form, so it is folded back down rather than switched off.
        /// </summary>
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

        /// <summary>
        /// Panels open and close on their own buttons, so the guide only watches
        /// them - it must never force one open by "revealing" it.
        /// </summary>
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

        /// <summary>Shows or hides every registered piece and every round button.</summary>
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

        /// <summary>
        /// Drops references to a farm that has been unloaded. Called when a farm
        /// scene is left, so the next farm's registrations start clean.
        /// </summary>
        /// <summary>
        /// Drops the beginner guide's ownership of the HUD when a farm is left, and
        /// forgets objects that went away with it.
        ///
        /// It deliberately does NOT wipe the whole registry. Several builders -
        /// the crop info panel, the objectives book, the save confirmation - build
        /// and register during Awake, and Unity raises sceneLoaded after every
        /// Awake but before any Start. Emptying the dictionaries there threw those
        /// registrations away a moment after they were made, which is why the
        /// objectives button was never hidden and why tapping a crop could not
        /// satisfy the inspection step: the guide was looking up a panel that was
        /// no longer in the registry.
        ///
        /// Entries belonging to the old scene are destroyed Unity objects, so they
        /// compare equal to null and are pruned here; anything still alive is
        /// simply overwritten when the new scene's builder registers.
        /// </summary>
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
