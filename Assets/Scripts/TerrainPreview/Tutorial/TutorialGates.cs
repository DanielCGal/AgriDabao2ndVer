using System;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The conditions the beginner guide waits on, in one place.
    ///
    /// Farm actions are read from <see cref="FarmTaskActionHub"/>, the same choke
    /// point the daily tasks use, so digging, planting, watering and mulching need
    /// no instrumentation of their own and arrive already de-duplicated. Panels are
    /// read from <see cref="HudRegistry"/> by watching whether their root object is
    /// active, which means "opened it and closed it again" is observable without
    /// any panel having to announce itself.
    /// </summary>
    public static class TutorialGates
    {
        // ------------------------------------------------------------- movement

        /// <summary>
        /// Walked a few metres, and jumped once if there is a jump button to press.
        ///
        /// Distance is measured from the player's own transform rather than from
        /// the joystick, so it works whatever is driving the character. That
        /// matters because MobileHudBuilder does not build the on-screen controls
        /// unless the game is running on a phone: gating on the joystick meant this
        /// step could never be completed in the Editor, and the tour simply stopped
        /// there. Where there is no jump button, walking alone is enough.
        /// </summary>
        public sealed class MoveAndJump
        {
            private const float RequiredMetres = 4f;

            private Transform player;
            private Vector3 lastPosition;
            private float travelled;
            private int jumpsAtStart = -1;

            public string Hint => MobileHudBuilder.Instance != null
                ? "Walk around, then jump"
                : "Walk around";

            public bool IsSatisfied()
            {
                if (player == null)
                {
                    FirstPersonTerrainController controller =
                        UnityEngine.Object.FindFirstObjectByType<FirstPersonTerrainController>();

                    if (controller == null)
                        return false;

                    player = controller.transform;
                    lastPosition = player.position;

                    if (MobileHudBuilder.Instance != null)
                        jumpsAtStart = MobileHudBuilder.Instance.JumpPressCount;
                }

                // Flattened: falling down a slope is not walking.
                Vector3 now = player.position;
                travelled += Vector2.Distance(
                    new Vector2(now.x, now.z),
                    new Vector2(lastPosition.x, lastPosition.z));
                lastPosition = now;

                if (travelled < RequiredMetres)
                    return false;

                MobileHudBuilder hud = MobileHudBuilder.Instance;
                if (hud == null)
                    return true;

                if (jumpsAtStart < 0)
                    jumpsAtStart = hud.JumpPressCount;

                return hud.JumpPressCount > jumpsAtStart;
            }
        }

        // -------------------------------------------------------- farm actions

        /// <summary>Waits for one recorded farm action of a given type.</summary>
        public sealed class FarmAction : IDisposable
        {
            private readonly string actionType;
            private readonly Func<ClimateActionRecord, bool> extra;
            private bool seen;

            public string Hint { get; }

            public FarmAction(string actionType, string hint,
                Func<ClimateActionRecord, bool> extra = null)
            {
                this.actionType = actionType;
                this.extra = extra;
                Hint = hint;

                FarmTaskActionHub.ActionRecorded += OnAction;
            }

            private void OnAction(ClimateActionRecord record)
            {
                if (record == null || seen)
                    return;

                if (!string.Equals(record.actionType, actionType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (extra != null && !extra(record))
                    return;

                seen = true;
            }

            public bool IsSatisfied() => seen;

            public void Dispose()
            {
                FarmTaskActionHub.ActionRecorded -= OnAction;
            }
        }

        // --------------------------------------------------------------- panels

        /// <summary>
        /// Waits for a panel to be opened and then closed again.
        ///
        /// Deliberately requires the open before the close: a panel that is already
        /// shut must not satisfy the gate the instant it is created.
        /// </summary>
        public sealed class PanelOpenedThenClosed
        {
            private readonly HudPiece piece;
            private bool wasOpen;

            public string Hint { get; }

            public PanelOpenedThenClosed(HudPiece piece, string hint)
            {
                this.piece = piece;
                Hint = hint;
            }

            public bool IsSatisfied()
            {
                bool open = HudRegistry.TryGetPiece(piece, out GameObject go) &&
                            go.activeInHierarchy;

                if (open)
                {
                    wasOpen = true;
                    return false;
                }

                return wasOpen;
            }
        }

        /// <summary>Waits only for a panel to be opened, not closed again.</summary>
        public sealed class PanelOpened
        {
            private readonly HudPiece piece;

            public string Hint { get; }

            public PanelOpened(HudPiece piece, string hint)
            {
                this.piece = piece;
                Hint = hint;
            }

            public bool IsSatisfied()
            {
                return HudRegistry.TryGetPiece(piece, out GameObject go) &&
                       go.activeInHierarchy;
            }
        }

        // ---------------------------------------------------------------- money

        /// <summary>Waits for the player to spend money and shut the shop again.</summary>
        public sealed class BoughtSomethingAndClosedShop
        {
            private readonly int startingMoney;
            private bool spent;

            public string Hint => "Buy anything, then close the shop";

            public BoughtSomethingAndClosedShop()
            {
                startingMoney = PlayerInventory.Instance != null
                    ? PlayerInventory.Instance.money
                    : 0;
            }

            public bool IsSatisfied()
            {
                if (PlayerInventory.Instance != null &&
                    PlayerInventory.Instance.money < startingMoney)
                {
                    spent = true;
                }

                bool shopOpen = HudRegistry.TryGetPiece(HudPiece.ShopPanel, out GameObject go) &&
                                go.activeInHierarchy;

                return spent && !shopOpen;
            }
        }

        // ------------------------------------------------------------ objectives

        /// <summary>Waits for the day's daily-task reward to be collected.</summary>
        public sealed class DailyRewardClaimed
        {
            public string Hint => "Collect your reward";

            public bool IsSatisfied()
            {
                return DailyTaskSystem.Instance != null &&
                       DailyTaskSystem.Instance.RewardClaimed;
            }
        }

        /// <summary>
        /// Waits for the player to turn in for the night with Skip Next Day.
        ///
        /// Counts from whatever the tally was when the gate opened rather than
        /// from zero, so a farm reloaded mid-tour cannot satisfy this with a skip
        /// that happened before Antonio asked for one.
        /// </summary>
        public sealed class DaySkipped
        {
            private int startCount = -1;

            public string Hint => "Press Skip Next Day";

            public bool IsSatisfied()
            {
                DailyTaskSystem daily = DailyTaskSystem.Instance;

                // No task system means nothing to wait for; do not strand the tour.
                if (daily == null)
                    return true;

                if (startCount < 0)
                    startCount = daily.DaySkipCount;

                return daily.DaySkipCount > startCount;
            }
        }
    }
}
