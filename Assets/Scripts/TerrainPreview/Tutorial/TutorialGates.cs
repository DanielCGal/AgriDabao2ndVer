using System;
using UnityEngine;

namespace AgriDabao3D
{
    public static class TutorialGates
    {
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

        public sealed class DailyRewardClaimed
        {
            public string Hint => "Collect your reward";

            public bool IsSatisfied()
            {
                return DailyTaskSystem.Instance != null &&
                       DailyTaskSystem.Instance.RewardClaimed;
            }
        }

        public sealed class DaySkipped
        {
            private int startCount = -1;

            public string Hint => "Press Skip Next Day";

            public bool IsSatisfied()
            {
                DailyTaskSystem daily = DailyTaskSystem.Instance;

                if (daily == null)
                    return true;

                if (startCount < 0)
                    startCount = daily.DaySkipCount;

                return daily.DaySkipCount > startCount;
            }
        }
    }
}
