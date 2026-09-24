using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HudRegistry.ReleaseControl();

            if (scene.name == "TerrainPreview" &&
                UnityEngine.Object.FindFirstObjectByType<TutorialDirector>() == null)
            {
                new GameObject("TutorialDirector").AddComponent<TutorialDirector>();
            }
        }

        private abstract class Beat { }

        private sealed class LineBeat : Beat
        {
            public string Speaker;
            public string Text;
            public AntonioExpression? Expression;
        }

        private sealed class DoBeat : Beat
        {
            public Action Run;
        }

        private sealed class GateBeat : Beat
        {
            public Func<string> Hint;
            public Func<bool> IsSatisfied;
            public Action Cleanup;

            public bool LiveHint;
        }

        private readonly List<Beat> beats = new List<Beat>();
        private int index = -1;
        private float nextHintRefresh;

        private TutorialDialogueUI dialogue;
        private Image blackout;
        private GateBeat activeGate;
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private const string Antonio = "Antonio";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            foreach (IDisposable disposable in disposables)
                disposable?.Dispose();

            disposables.Clear();

            if (Instance == this)
                Instance = null;
        }

        private IEnumerator Start()
        {
            float waitedUntil = Time.unscaledTime + 60f;
            while (FarmLoadContext.IsRestoring && Time.unscaledTime < waitedUntil)
                yield return null;

            if (FarmLoadContext.IsRestoring)
            {
                Debug.LogWarning("[Tutorial] Farm still loading after 60s; skipping the beginner guide.");
                yield break;
            }

            if (TutorialState.Completed)
                yield break;

            yield return null;
            yield return null;

            dialogue = gameObject.AddComponent<TutorialDialogueUI>();

            TutorialState.Offered = true;
            HideEverything();

            CreateBlackout();

            AskWhetherToRun();
        }

        private void CreateBlackout()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            GameObject go = new GameObject("TutorialBlackout", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            blackout = go.GetComponent<Image>();
            blackout.color = Color.black;

            blackout.raycastTarget = true;

            go.transform.SetAsLastSibling();
        }

        private IEnumerator FadeOutBlackout(float seconds)
        {
            if (blackout == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / seconds);
                blackout.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }

            Destroy(blackout.gameObject);
            blackout = null;
        }

        private void AskWhetherToRun()
        {
            FarmConfirmPopup popup = FarmConfirmPopup.Instance;

            if (popup == null)
            {
                Debug.LogWarning("[Tutorial] No FarmConfirmPopup found; starting the guide without asking.");
                BeginTour();
                return;
            }

            popup.Show(
                "Play the Tutorial? (Recommended for new Players)",
                BeginTour,
                UIThemeSprites.Instance?.tutorialPromptLabel,
                DeclineTour);

            if (HudRegistry.TryGetPiece(HudPiece.ConfirmPopup, out GameObject popupGo))
                popupGo.transform.SetAsLastSibling();
        }

        private void BeginTour()
        {
            StartCoroutine(RevealThenBeginTour());
        }

        private IEnumerator RevealThenBeginTour()
        {
            yield return FadeOutBlackout(2.5f);
            yield return new WaitForSecondsRealtime(0.4f);

            BuildBeats();
            Advance();
        }

        private void DeclineTour()
        {
            Debug.Log("[Tutorial] Declined; granting the starting kit and restoring the HUD.");

            Grant(InventoryItemType.Shovel, 1);
            Grant(InventoryItemType.WateringCan, 1);
            Grant(InventoryItemType.MulchBag, 3);

            foreach (InventoryItemType seed in TutorialState.StartingSeeds)
                Grant(seed, DistrictCropPools.SeedsPerKind);

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.AddMoney(500);

            Finish();
            StartCoroutine(FadeOutBlackout(1.6f));
        }

        private void Update()
        {
            if (activeGate == null)
                return;

            if (!activeGate.IsSatisfied())
            {
                RefreshLiveHint();
                return;
            }

            activeGate.Cleanup?.Invoke();
            activeGate = null;
            Advance();
        }

        private void RefreshLiveHint()
        {
            if (!activeGate.LiveHint || dialogue == null || Time.unscaledTime < nextHintRefresh)
                return;

            nextHintRefresh = Time.unscaledTime + 0.5f;
            dialogue.SetObjectiveText(activeGate.Hint?.Invoke());
        }

        private void Advance()
        {
            index++;

            while (index < beats.Count)
            {
                Beat beat = beats[index];

                if (beat is DoBeat doBeat)
                {
                    doBeat.Run?.Invoke();
                    index++;
                    continue;
                }

                if (beat is GateBeat gate)
                {
                    activeGate = gate;
                    dialogue.ShowObjective(gate.Hint?.Invoke());
                    return;
                }

                LineBeat line = (LineBeat)beat;
                TutorialState.CurrentStep = index;
                dialogue.ShowLine(line.Speaker, line.Text, line.Expression, Advance);
                return;
            }

            Finish();
        }

        private void Finish()
        {
            dialogue.Hide();

            HudRegistry.EndTutorialControl();
            TutorialState.CropInspectionLocked = false;

            GrantCompletionKit();

            TutorialState.Completed = true;
            TutorialState.CurrentStep = -1;

            Debug.Log("[Tutorial] Beginner guide finished; saving.");

            if (FarmPersistenceManager.Instance != null)
                StartCoroutine(FarmPersistenceManager.Instance.SaveFarm());
        }

        private void HideEverything()
        {
            HudRegistry.BeginTutorialControl();
            TutorialState.CropInspectionLocked = true;


            ShowPanelsAsBuilt();
        }

        private static void ShowPanelsAsBuilt()
        {
            HudPiece[] panels =
            {
                HudPiece.CropInfoPanel, HudPiece.ShopPanel, HudPiece.ObjectivesPanel,
                HudPiece.SearchPlayersPanel, HudPiece.MarketplacePanel, HudPiece.ConfirmPopup,
                HudPiece.SeedlingTentPanel
            };

            foreach (HudPiece panel in panels)
            {
                if (HudRegistry.TryGetPiece(panel, out GameObject go))
                    go.SetActive(false);
            }
        }

        private void Say(string text, AntonioExpression expression)
        {
            beats.Add(new LineBeat { Speaker = Antonio, Text = text, Expression = expression });
        }

        private void Narrate(string text)
        {
            beats.Add(new LineBeat { Speaker = null, Text = text, Expression = null });
        }

        private void Do(Action action)
        {
            beats.Add(new DoBeat { Run = action });
        }

        private void Reveal(HudPiece piece)
        {
            Do(() => HudRegistry.SetPieceVisible(piece, true));
        }

        private void RevealButton(int slot)
        {
            Do(() => HudRegistry.SetIconButtonVisible(slot, true));
        }

        private void WaitFor(Func<bool> satisfied, string hint, Action cleanup = null)
        {
            beats.Add(new GateBeat
            {
                IsSatisfied = satisfied,
                Hint = () => hint,
                Cleanup = cleanup
            });
        }

        private void WaitForAction(string actionType, string hint,
            Func<ClimateActionRecord, bool> extra = null)
        {
            TutorialGates.FarmAction gate = new TutorialGates.FarmAction(actionType, hint, extra);
            disposables.Add(gate);
            WaitFor(gate.IsSatisfied, hint, gate.Dispose);
        }

        private void WaitForAction(string actionType, Func<string> liveHint,
            Func<ClimateActionRecord, bool> extra = null)
        {
            TutorialGates.FarmAction gate = new TutorialGates.FarmAction(actionType, null, extra);
            disposables.Add(gate);
            beats.Add(new GateBeat
            {
                IsSatisfied = gate.IsSatisfied,
                Hint = liveHint,
                Cleanup = gate.Dispose,
                LiveHint = true
            });
        }

        private static void Grant(InventoryItemType item, int amount)
        {
            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.AddItem(item, amount);
        }

        private static string District =>
            string.IsNullOrWhiteSpace(SelectedAreaState.SelectedDistrictName)
                ? "Davao"
                : SelectedAreaState.SelectedDistrictName;

        private static string PlayerName
        {
            get
            {
                string name = AuthSession.Instance != null && AuthSession.Instance.CurrentUser != null
                    ? AuthSession.Instance.CurrentUser.displayName
                    : null;

                return string.IsNullOrWhiteSpace(name) ? "neighbour" : name.Trim();
            }
        }

        private static void GrantCompletionKit()
        {
            Grant(InventoryItemType.Machete, 1);
            Grant(InventoryItemType.FruitBag, 10);
        }

        private void BuildBeats()
        {
            Step1Arrival();
            Step2Controls();
            Step3PrepareAndPlant();
            Step4Water();
            Step4BSeedlingTent();
            Step5Inspect();
            Step6HowPlantsLive();
            Step6BMulch();
            Step7Threats();
            Step8WeatherBoard();
            Step9Map();
            Step10Shop();
            Step11Objectives();
            Step12Farmers();
            Step13Marketplace();
            Step14SaveFarm();
            Step15Farewell();
        }

        private void Step1Arrival()
        {
            Narrate("You bought a small plot here in " + District +
                    ", Davao City. Today your new life as a farmer starts.");
            Say("Hey! Over here!", AntonioExpression.Hello);
            Narrate("Your neighbour sets down his rice and comes over, grinning.");
            Say("You're the new farmer everyone's talking about? I'm Antonio - " +
                "my plot is right beside yours.", AntonioExpression.Hello);
            Narrate("You shake his hand and tell him your name.");
            Say("Ohhh, " + PlayerName + "! Welcome to " + District +
                "! So - do you know how to farm?", AntonioExpression.Surprise);
            Narrate("You shake your head.");
            Say("Ha! Don't worry, I'll teach you everything. Ready?", AntonioExpression.Hello);
        }

        private void Step2Controls()
        {
            Reveal(HudPiece.Joystick);
            Reveal(HudPiece.JumpButton);

            Say("First - you can't farm land you can't walk. The left circle moves you, " +
                "the right button jumps.", AntonioExpression.Teaching);
            Say("Go on. Walk around, and give me one jump.", AntonioExpression.Teaching);

            TutorialGates.MoveAndJump moveGate = new TutorialGates.MoveAndJump();
            WaitFor(moveGate.IsSatisfied, moveGate.Hint);

            Say("Look at you. Already moving like a farmer.", AntonioExpression.Hello);
        }

        private static PlantingMaterialInfo LessonMaterial()
        {
            foreach (InventoryItemType item in TutorialState.StartingSeeds)
            {
                if (PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) && info.PlantDirect)
                    return info;
            }

            PlantingMaterialCatalog.TryGet(InventoryItemType.BananaSucker, out PlantingMaterialInfo fallback);
            return fallback;
        }

        private static PlantingMaterialInfo TentLessonMaterial()
        {
            foreach (InventoryItemType item in TutorialState.StartingSeeds)
            {
                if (PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) &&
                    info.SowInBag && !info.PlantDirect)
                {
                    return info;
                }
            }

            foreach (InventoryItemType item in TutorialState.StartingSeeds)
            {
                if (PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) && info.SowInBag)
                    return info;
            }

            return null;
        }

        private void Step3PrepareAndPlant()
        {
            PlantingMaterialInfo lesson = LessonMaterial();
            bool lessonInDeal = TutorialState.StartingSeeds.Contains(lesson.Item);
            string ground = PlantingMaterialCatalog.PlotName(lesson.Plot);
            string choice = lesson.Plot == PreparedPlotKind.Hole ? "Planting Hole"
                : lesson.Plot == PreparedPlotKind.RaisedBed ? "Raised Bed"
                : "Furrow";

            Reveal(HudPiece.Hotbar);
            Do(() =>
            {
                Grant(InventoryItemType.Shovel, 1);
                foreach (InventoryItemType seed in TutorialState.StartingSeeds)
                    Grant(seed, DistrictCropPools.SeedsPerKind);

                if (!lessonInDeal)
                    Grant(lesson.Item, 1);

                if (lesson.NeedsMulchedBed)
                    Grant(InventoryItemType.MulchBag, 1);
            });

            Say("Now the real work. Here - take these.", AntonioExpression.Teaching);
            Narrate("Antonio hands you a shovel and a bundle of planting material.");
            Say("These grow well in " + District + " soil. That bar along the bottom is your " +
                "hotbar - tap a slot to hold an item.", AntonioExpression.Teaching);
            Say("Nothing grows in hard ground. Take the shovel and till a patch of soil first.",
                AntonioExpression.Teaching);

            WaitForAction("TillGround", "Till a patch of ground with the shovel");

            Say("Good. Now each crop wants its ground ready in its own way. Your " + lesson.Name +
                " needs a " + ground + ": tap the tilled soil with the shovel again and choose " +
                choice + ".", AntonioExpression.Teaching);

            WaitForAction("DigPlantingSpot", () => GroundHint(lesson, choice, false),
                record => string.Equals(record.itemType, lesson.Plot.ToString(), StringComparison.OrdinalIgnoreCase));

            if (lesson.NeedsMulchedBed)
            {
                Say("Strawberry runners are planted through mulch. Hold the mulch bag and tap the " +
                    "bed to cover it.", AntonioExpression.Teaching);
                WaitForAction("MulchBed", "Cover the raised bed with a mulch bag");
            }

            Say("There you go. Now hold your " + lesson.Name + " and tap the " + ground +
                " to plant it.", AntonioExpression.Hello);

            WaitForAction("PlantCrop", () => GroundHint(lesson, choice, true));

            Say("Your very first crop! You're a farmer now, neighbour. Officially.",
                AntonioExpression.Surprise);
        }

        private static string GroundHint(PlantingMaterialInfo lesson, string choice, bool planting)
        {
            bool tilled = false;
            bool ready = false;
            bool bedNeedsMulch = false;
            DigSpot wrongKind = null;

            foreach (DigSpot spot in UnityEngine.Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
            {
                if (spot == null || spot.occupied)
                    continue;

                if (spot.plotKind == PreparedPlotKind.Tilled)
                    tilled = true;
                else if (spot.plotKind == lesson.Plot)
                {
                    if (!lesson.NeedsMulchedBed || spot.mulched)
                        ready = true;
                    else
                        bedNeedsMulch = true;
                }
                else if (spot.IsPrepared && wrongKind == null)
                    wrongKind = spot;
            }

            string ground = PlantingMaterialCatalog.PlotName(lesson.Plot);

            if (planting && ready)
                return "Hold your " + lesson.Name + " and tap the " + ground;
            if (planting && bedNeedsMulch)
                return "Cover the raised bed with a mulch bag, then plant your " + lesson.Name;
            if (tilled)
                return "Tap the tilled ground with the shovel and choose " + choice;
            if (wrongKind != null)
            {
                return "That is a " + wrongKind.DisplayName + ", not a " + ground +
                       ". Till another patch with the shovel and choose " + choice;
            }

            return "Till a patch of ground with the shovel, then choose " + choice;
        }

        private void Step4BSeedlingTent()
        {
            PlantingMaterialInfo tentLesson = TentLessonMaterial();

            RevealButton(HudIconButton.SlotSeedlingTent);

            Say("Not everything goes straight into the ground, mind. Some crops start life in a " +
                "little bag of soil, where you can look after them.", AntonioExpression.Thinking);
            Say("That's what your Seedling Tent is for. The tent button up top opens it from " +
                "anywhere on your farm.", AntonioExpression.Teaching);

            if (tentLesson != null)
            {
                Say("Open it, fill a bag with soil, then sow your " + tentLesson.Name + " in it.",
                    AntonioExpression.Teaching);

                WaitForAction("SowSeedlingBag", "Open the Seedling Tent and sow a seedling bag");

                Say("In a few days it'll be ready. Pick it in the tent, carry it out, and " +
                    "transplant it into prepared ground. The days in the bag count toward its " +
                    "growing, so nothing is lost.", AntonioExpression.Teaching);
            }
            else
            {
                Say("Seeds like cacao and tomato, banana plantlets and grafted mangoes all start " +
                    "there. The shop sells them when you're ready.", AntonioExpression.Teaching);
            }

            Say("Close the tent for now and let's keep going.", AntonioExpression.Hello);

            TutorialGates.PanelOpenedThenClosed closed = new TutorialGates.PanelOpenedThenClosed(
                HudPiece.SeedlingTentPanel, "Open the Seedling Tent, then close it");
            WaitFor(closed.IsSatisfied, closed.Hint);
        }

        private void Step4Water()
        {
            Do(() => Grant(InventoryItemType.WateringCan, 1));

            Say("Something just planted isn't settled yet. It needs water - especially early.",
                AntonioExpression.Thinking);
            Narrate("He unhooks a spare watering can from his belt and holds it out.");
            Say("Take mine. Select it, then give that crop a drink.", AntonioExpression.Teaching);

            WaitForAction("WaterCrop", "Water your crop");

            Say("See? Not so hard.", AntonioExpression.Hello);
        }

        private void Step5Inspect()
        {
            Do(() => TutorialState.CropInspectionLocked = false);

            Say("You can check on a crop any time. Tap the one you just planted.",
                AntonioExpression.Teaching);

            TutorialGates.PanelOpened inspect =
                new TutorialGates.PanelOpened(HudPiece.CropInfoPanel, "Tap your crop");
            WaitFor(inspect.IsSatisfied, inspect.Hint);

            Say("Health, stress, moisture, and how well the soil suits it. A plant tells you " +
                "something is wrong long before it dies - but only if you look.",
                AntonioExpression.Teaching);
        }

        private void Step6HowPlantsLive()
        {
            Say("Here's what beginners get wrong: a plant isn't simply watered or not watered.",
                AntonioExpression.Thinking);
            Say("Soil, weather, temperature, moisture, pests - all of it moves its health every " +
                "day, even while you're away. Same seed, different ground, different result.",
                AntonioExpression.Teaching);
        }

        private void Step6BMulch()
        {
            Do(() => Grant(InventoryItemType.MulchBag, 3));

            Narrate("He drops a few sacks of mulch at your feet.");
            Say("So push back. Mulch holds the moisture in and steadies the soil - it helps in " +
                "any weather. Put some on your crop.", AntonioExpression.Teaching);

            WaitForAction("ApplyCropMaintenance", "Apply mulch to your crop");

            Say("That's the idea behind every tool you'll buy: see the problem coming, and " +
                "soften it before it lands.", AntonioExpression.Teaching);
        }

        private void Step7Threats()
        {
            Say("Your farm will get tested. Typhoons, drought, heavy rain, pests, disease - " +
                "they come and go with the months and the heat.", AntonioExpression.Thinking);
            Say("How much it hurts comes down to one thing: what you did before it arrived. " +
                "Nets, windbreaks, drainage, sprays, traps - it's all at the shop.",
                AntonioExpression.Teaching);
            Say("And you won't face it alone. I'll warn you when I see weather coming, watch how " +
                "you handle it, and tell you after what you did right.", AntonioExpression.Hello);
        }

        private void Step8WeatherBoard()
        {
            Reveal(HudPiece.WeatherPanel);

            Say("That board top-left is the weather, time, date and temperature. Read it every " +
                "morning - the month tells you what's coming.", AntonioExpression.Teaching);
        }

        private void Step9Map()
        {
            Reveal(HudPiece.Map);

            Say("Your land is bigger than it looks. That little board on the right is your map - " +
                "the white dot is you, the brown ones are your crops.", AntonioExpression.Teaching);
            Say("Open it, have a good look, then close it again.", AntonioExpression.Teaching);

            WaitFor(MapWasOpenedAndClosed, "Open the map, then close it");

            Say("There. Now you'll never lose a plant again.", AntonioExpression.Hello);
        }

        private bool mapSeenOpen;

        private bool MapWasOpenedAndClosed()
        {
            FarmMapUIBuilder map = UnityEngine.Object.FindFirstObjectByType<FarmMapUIBuilder>();
            if (map == null)
                return true;

            if (map.IsExpanded)
            {
                mapSeenOpen = true;
                return false;
            }

            return mapSeenOpen;
        }

        private void Step10Shop()
        {
            RevealButton(HudIconButton.SlotShop);
            Reveal(HudPiece.Money);
            Do(() =>
            {
                if (PlayerInventory.Instance != null)
                    PlayerInventory.Instance.AddMoney(500);
            });

            Say("Farming takes money, too.", AntonioExpression.Teaching);
            Narrate("Antonio presses a folded bundle of notes into your hand.");
            Say("A welcome gift - pay me back in mangoes. Your money sits top right, and that " +
                "cart is the shop: seeds, tools, sprays, everything.", AntonioExpression.Hello);
            Say("Seeds and planting material your district does not grow are greyed out. You " +
                "cannot buy those here, but you can still read about them.", AntonioExpression.Teaching);
            Say("Open it, buy one thing - anything - then close it up.", AntonioExpression.Teaching);

            Do(() => shopGate = new TutorialGates.BoughtSomethingAndClosedShop());
            WaitFor(() => shopGate != null && shopGate.IsSatisfied(),
                "Buy anything, then close the shop");

            Say("Spending money to make money. That's farming.", AntonioExpression.Hello);
        }

        private TutorialGates.BoughtSomethingAndClosedShop shopGate;

        private void Step11Objectives()
        {
            RevealButton(HudIconButton.SlotFarmObjectives);

            Narrate("Antonio pulls a worn little notebook from his bag and holds it out.");
            Say("Ever wake up wondering what to do today? This is for that. The book button up " +
                "top opens it - take a look.", AntonioExpression.Teaching);

            TutorialGates.PanelOpened opened =
                new TutorialGates.PanelOpened(HudPiece.ObjectivesPanel, "Open the objectives book");
            WaitFor(opened.IsSatisfied, opened.Hint);

            Say("A few simple jobs each day, and money once you finish them all. Today wanted " +
                "ground prepared and something planted - you've done both. Collect your reward.",
                AntonioExpression.Teaching);

            TutorialGates.DailyRewardClaimed claimed = new TutorialGates.DailyRewardClaimed();
            WaitFor(claimed.IsSatisfied, claimed.Hint);

            Say("Paid for work you'd already done. And once the day's jobs are finished, you " +
                "don't have to wait around for sunset.", AntonioExpression.Hello);
            Say("Press 'Skip Next Day' beside the reward. Go on, I'll wait.",
                AntonioExpression.Teaching);

            TutorialGates.DaySkipped skipped = new TutorialGates.DaySkipped();
            WaitFor(skipped.IsSatisfied, skipped.Hint);

            Say("Morning! Eight sharp, and the book already has fresh jobs. Do the work, take " +
                "your pay, turn in, go again.", AntonioExpression.Hello);
            Say("There's my kind of job too. If that book ever feels too easy, call me - I'll " +
                "walk your farm myself and give you something harder, for a bigger reward.",
                AntonioExpression.Teaching);
            Say("Not yet though. Close the book and follow me.", AntonioExpression.Hello);

            TutorialGates.PanelOpenedThenClosed closed =
                new TutorialGates.PanelOpenedThenClosed(HudPiece.ObjectivesPanel, "Close the book");
            WaitFor(closed.IsSatisfied, closed.Hint);
        }

        private void Step12Farmers()
        {
            RevealButton(HudIconButton.SlotSearchPlayers);

            Say("And you're not out here on your own - plenty of farmers working their own plots.",
                AntonioExpression.Thinking);
            Say("That button finds them. Add them, message them, trade with them. Open it, then " +
                "close it again.", AntonioExpression.Teaching);

            TutorialGates.PanelOpenedThenClosed gate = new TutorialGates.PanelOpenedThenClosed(
                HudPiece.SearchPlayersPanel, "Open the farmer list, then close it");
            WaitFor(gate.IsSatisfied, gate.Hint);

            Say("Good neighbours are worth more than good soil.", AntonioExpression.Hello);
        }

        private void Step13Marketplace()
        {
            RevealButton(HudIconButton.SlotMarketplace);

            Say("This one is the marketplace. Farmers sell their seeds, tools and harvest here, " +
                "and you can sell yours the same way.", AntonioExpression.Teaching);
            Say("There's a small fee every time you list something, so price it properly. Take a " +
                "look inside, then close it.", AntonioExpression.Thinking);

            TutorialGates.PanelOpenedThenClosed gate = new TutorialGates.PanelOpenedThenClosed(
                HudPiece.MarketplacePanel, "Open the marketplace, then close it");
            WaitFor(gate.IsSatisfied, gate.Hint);
        }

        private void Step14SaveFarm()
        {
            RevealButton(HudIconButton.SlotSaveFarm);

            Say("That barn button saves your farm - not onto this phone, but somewhere far away.",
                AntonioExpression.Teaching);
            Say("Save it and you can sign in on any device and find everything exactly as you " +
                "left it. Open it and look, but don't save - I'll do that before I go.",
                AntonioExpression.Teaching);

            TutorialGates.PanelOpenedThenClosed gate = new TutorialGates.PanelOpenedThenClosed(
                HudPiece.ConfirmPopup, "Open the save panel, then close it");
            WaitFor(gate.IsSatisfied, gate.Hint);
        }

        private void Step15Farewell()
        {
            Say("I should get back to my rice before the birds finish it. But here - take this.",
                AntonioExpression.Thinking);
            Narrate("He scribbles a number on a strip of old feed sack and folds it into your hand.");

            RevealButton(HudIconButton.SlotAdviserChat);

            Say("My number. That phone button up top - ask me about your crops, your soil, the " +
                "weather, what to plant. Any time.", AntonioExpression.Teaching);
            Say("You'll do just fine out here, " + PlayerName + ". Welcome to " + District + "!",
                AntonioExpression.Hello);
            Narrate("Antonio waves, swings his rice over his shoulder, and heads off whistling.");

            RevealButton(HudIconButton.SlotPause);
        }
    }
}
