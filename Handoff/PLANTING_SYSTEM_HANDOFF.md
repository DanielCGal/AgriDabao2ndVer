# AgriDabaw-3D — Handoff for the New Planting System

Written on 23 September 2026 at the end of a long working session. The next session should
read this whole file before doing anything.

---

## 0. Start here

**The user's current instruction:** do **not** start coding the planting system until they
say so. They are still generating the Meshy models and Artlist sprites and will send them
with the names in section 6.

**First things to do when the session opens**

1. Confirm the Unity MCP tools are present and connected to the open Editor (section 9).
2. Ask whether the Unity project is under git yet. It was not on 23 September. The only
   safety net was a manual copy in `C:\Users\Admin\Desktop\JustInCase`, made *before* the
   22 September script reorganisation, so it is older than the current code. Recommend
   `git init` with a Unity `.gitignore` before any large edit.
3. Wait for the go-ahead and the assets, then follow the build order in section 8.

**Where the rest of the history lives.** The full previous transcript is
`C:\Users\Admin\.claude\projects\D--AgriDabao-agridabao-api\5dd7afcf-2dfd-4f10-85e9-a38dcf761489.jsonl`.
It is very large; search it for details rather than reading it whole. A session opened on the
folder `D:\AgriDabao\agridabao-api` also loads the saved memories automatically. A session
opened on the Unity folder does not, which is why section 2 repeats them.

---

## 1. The project

- **Capstone:** *AgriDabaw-3D: A Simulation Game with AI-Advisor for Localized Farming
  Techniques.* Team GreenScape: Daniel C. Galam (the user) and Jessica Mae G. Suello.
  Assumption College of Davao. Adviser: Mr. Dennis B. Gajo, MIT.
- **Final defense: 14 October 2026.**
- **Why planting is changing:** at the pre-final defense the panel asked for a more
  realistic planting process. The user's limit: "at the end of the day, it's still a
  simulation". Do not add every real-world mitigation.

| Part | Path | Notes |
|---|---|---|
| Unity game | `D:\Unity\UnityProjects\AgriDabao2ndVer` | Unity 6000.3.11f1 (6.3 LTS), URP, Android ARM64, IL2CPP, min SDK 26. **Not a git repository.** 185 C# scripts |
| Backend | `D:\AgriDabao\agridabao-api` | Spring Boot 4.1, Java 21, Gradle. GitHub `DanielCGal/agridabao-api` (**public**). Railway auto-deploys `main` to `https://agridabao-api-production.up.railway.app`. PostgreSQL, Flyway V1–V11 |
| Website | `D:\Download\Agridabao-PromotionalWebsite` | GitHub `DanielCGal/agridabaw-3d`, served at `https://danielcgal.github.io/agridabaw-3d/`. APK: GitHub Release v8.2, 390.7 MB |

**Script layout** (reorganised 22 September): `Assets/Scripts/{MainMenu, AreaSelection,
TerrainPreview, Shared}/<purpose>/`. Planting code lives in `TerrainPreview/Farming`,
`TerrainPreview/CropInstances`, `TerrainPreview/Inventory`, `TerrainPreview/Economy` and
`TerrainPreview/Persistence`. Put new nursery code in a new `TerrainPreview/Nursery/`
folder. **A `.cs` file must never move without its `.cs.meta`**, or scene references break.

**How the game is built, in brief**

- **All UI is built in C# at runtime** by `*UIBuilder` classes. There are no UI prefabs and
  no scene canvases. Painted art sits in the `UIThemeSprites` ScriptableObject
  (`Shared/UI/UIThemeSprites.cs`); `UIPlank` draws live text on blank planks; `HudRegistry`
  registers panels.
- **Game clock:** `TerrainPreview/Weather/GameTimeSystem.cs`, `realSecondsPerGameDay = 900`.
- **Prices** are held in centavos (`TerrainPreview/Economy/PesoPrice.cs`) and come from the
  Davao City government price list. `TerrainPreview/Economy/ShopUIBuilder.cs` is the only
  price list in the client. The server keeps its own copy (section 5).
- **Farm saves** are a JSON snapshot (`TerrainPreview/Persistence/FarmSnapshotModels.cs`,
  `FarmPersistenceManager.CurrentSchemaVersion = 3`) stored on the server with an optimistic
  `revision`.
- **District limits:** `Shared/Districts/DistrictCropPools.cs` (client) and
  `farm/DistrictSeedPools.java` (server) must stay identical. The shop greys out seeds a
  district does not grow, the marketplace hides them, and trades refuse them. A new farm
  starts with 3 seed kinds × 3 seeds from its own district.
- One device per account (presence window 45 s, heartbeat every 15 s). JWT tokens last 30
  days. The AI Adviser runs on Gemini 2.5 Flash through the backend; the key never ships in
  the APK.

---

## 2. Standing rules from the user

These are also saved as memories. They are repeated here in case a session opens on the
Unity folder, where those memories do not load.

- **Tested vs verified.** Always say which parts were actually run against a live system and
  which were only compiled, read or reasoned through. The user checks.
- **"Can you …?" is a question.** Answer it and wait. Explicit requests ("fix it", "fill up
  these files", "push it") get done without friction.
- **Backend pushes.** Standing authorization to commit and push requested `agridabao-api`
  changes to `main` without asking, because Railway only rebuilds on a push. Compile first
  with `./gradlew compileJava`. Be very careful with Flyway migrations: a failed one stops
  the server for every player. Never force-push or rewrite history. For the website repo and
  anything else, ask before pushing.
- **Never create or change account data on the live Railway server while testing.** Read-only
  probes are fine: the health endpoint, and requests that should be refused (no token, bad
  credentials).
- **Credits.** Artlist: use GPT Image 2.5, sparingly, and never spend credits without asking.
  Treat Meshy the same way.
- **UI art.** New player-facing UI reuses sprites from `Assets/Sprites/UI`. Developer tools
  stay plain.
- **Crop ids** such as `mangosteen_1` are deliberate player-facing names. Never "tidy" them
  into prose.
- **Changing a rule, price or piece of content:** trace every dependent system in both repos,
  fix the bugs found, and report them unprompted.
- **Paper scope (settled).** Trade history, removing a friend, promotional discounts and
  player-triggered soil sampling are *not* features. Objective 2 covers typhoon and drought
  only. Tugbok is written as playable. When SoilGrids is unreachable, the game falls back to a
  preset soil sample on purpose; that is not a defect.
- Never commit `.claude/settings.local.json`. Never paste API keys or the JWT secret into chat.
- The user wants no new bugs, complete fixes, and simple wording in documents.

---

## 3. The new planting system — design (locked by the user)

**The planting material, not the crop, decides the route.** There are 16 materials across
the 13 crops.

**Ground preparations**

- **Planting hole** — cacao, durian, mangosteen, banana, pomelo, mango, coconut
- **Raised bed** — tomato, eggplant, strawberry, pineapple, squash
- **Tilled row** — corn

**Nursery route** — sown in a seedling bag kept in the seedling tent, left to grow, then
transplanted to the field.

| # | Material | Player flow | Nursery wait (game days) | From the user's notes |
|---|---|---|---|---|
| 1 | Cacao seed | buy → fill bag → sow → wait → dig hole → transplant → shade → water | ~6 | Shade management is critical for young cacao |
| 2 | Durian seed | buy → sow → germinate → prick into bag → wait → hole → transplant | ~6 | Prick into polybags 9–12 days after germination |
| 3 | Mangosteen seed | buy fresh seed → bag → sow → long wait → hole → transplant | ~8 (slowest) | Germination about 30 days; fresh seed over 1 g |
| 4 | Banana plantlet | buy tissue-culture plantlet → bag → harden → hole → transplant | ~4 | Macropropagation as the low-cost source of material |
| 5 | Pomelo seed | buy → sow → germinate → prick into bag → wait → hole → transplant | ~6 | Prick 21–28 days after germination |
| 6 | Mango grafted seedling | buy grafted seedling → hold in bag → hole → transplant at the same depth | ~2 | Plant at the depth it grew at in the nursery; bears earlier than seed |
| 7 | Tomato seed | buy → sow in bag → wait → build bed → transplant | ~3 | 14 days in a seed box; transplant at 3–5 true leaves |
| 8 | Eggplant seed | buy → sow in bag → wait → build bed → transplant | ~4 | Germination 7–10 days; transplant 4–6 weeks after sowing |
| 9 | Squash seed (nursery option) | buy → sow in bag → wait → build bed → transplant | ~2 | Transplant seedlings 2–3 weeks old |

**Direct route** — prepare the ground, then plant.

| # | Material | Player flow | From the user's notes |
|---|---|---|---|
| 10 | Banana sword sucker | buy sucker → till → dig hole → plant → water | Sword suckers about 20 cm, or 3–4 ft for Saba |
| 11 | Pineapple sucker | buy sucker → till → build bed → trim material → plant at depth | Trim or treat material; place at the correct depth |
| 12 | Coconut sprouted seednut | buy sprouted seednut → till → dig hole → plant | Seednuts stored 3–4 weeks or soaked 2 weeks to sprout |
| 13 | Strawberry rooted runner | buy runner → till → build bed → mulch → plant | Runner rooted, cut from the mother, then transplanted |
| 14 | Mango *liso* | buy seed → till → dig hole → plant | The seed-grown route; slower to bear than grafted |
| 15 | Squash seed (direct option) | buy → till → build bed → sow | Direct sowing is allowed alongside transplanting |
| 16 | Corn seed | buy → till → open furrow → sow | Not in the exported notes; standard land-preparation pattern |

**Decisions the user made**

- **Squash is the teaching pair.** The same seed can be direct-sown into a bed or raised in a
  bag first, and a ready-made **squash seedling** can be bought to skip the wait for more
  money.
- **Banana and mango each sell two materials:** plantlet or sucker; grafted seedling or
  *liso*.
- **Coconut** is sold as an already-sprouted seednut, which keeps it a single step.
- **Nursery waits are compressed** into a few game days, because a real cacao nursery would
  take about 45 real hours at 900 seconds per game day. The real-world figure stays visible
  in the crop information text.

The user's own crop documents are in this folder (`crop_production_notes.txt`,
`crop_materials.txt`) and are the source for every real-world figure above. Corn is not in
them.

---

## 4. What has to change — Unity

Checked against the code on 22–23 September 2026.

| Area | File(s) | Work |
|---|---|---|
| Items | `TerrainPreview/Inventory/InventoryItemType.cs` | Append 8 values **at the end** (the file says new values always go last): `BananaPlantlet`, `BananaSucker`, `MangoGraftedSeedling`, `MangoLiso`, `CoconutSeednut`, `PineappleSucker`, `StrawberryRunner`, `SquashSeedling`. The existing `*Seed` items stay |
| Route data | new, modelled on `DistrictCropPools` | Material → route, ground preparation, nursery days, resulting crop, real-world figure |
| Ground prep | `TerrainPreview/Farming/FarmingInteractionSystem.cs` | The Shovel already digs a `DigSpot`; add build-bed and till-row |
| Seedling tent | `TerrainPreview/ClimateMaintenance/ClimateMitigationWorldObject.cs`, `WorldObjectSaveDto` | A placed structure like the existing `GreenhouseKit`, with its own panel |
| Seedling bags | new state, plus `TerrainPreview/Persistence/FarmSnapshotModels.cs` | A bag holds the material, sown game day, ready game day and sprout visual; new save list |
| Planting | `FarmingInteractionSystem.cs` (2,908 lines; three plant paths today: `TryPlantSeed`, `TryPlantBananaSeed`, `TryPlantTropicalSeed`) | Fill bag, sow, transplant, plant a direct material. Add `ReportFarmAction` labels so the UI response timer (device performance Test 5) still records |
| Crops | the 13 `TerrainPreview/CropInstances/*PlantInstance.cs` plus `TropicalCropPlantInstance` | A transplant starts at the seedling stage, not as a seed |
| Save format | `TerrainPreview/Persistence/FarmPersistenceManager.cs` | Bump `CurrentSchemaVersion` from 3 to 4, and make sure version-3 farms still load |
| Shop | `TerrainPreview/Economy/ShopUIBuilder.cs` | The only client price list (centavos via `PesoPrice`). Add the 8 new entries |
| Districts | `Shared/Districts/DistrictCropPools.cs` | New materials are district-restricted like seeds |
| Tutorial | `TerrainPreview/Tutorial/TutorialDirector.cs` | `Step3DigAndPlant` waits on `"DigPlantingSpot"` and `"PlantCrop"`; both change |
| Objectives | `TerrainPreview/FarmTasks/DailyTaskCatalog.cs`, `AIAdvisorTaskSystem.cs` | The `("Plant", "CornSeed")` task and its siblings must understand materials |
| AI Adviser | `TerrainPreview/AIAdviser/FarmAdvisorToolReference.cs` | Teach it the routes, or it will advise the old flow |
| Info text | `TerrainPreview/Farming/CropFieldDescriptions.cs` | Show the real-world timing beside the compressed one |
| UI and audio | runtime builders, `Shared/UI/UIThemeSprites.cs`, `Shared/Audio/GameAudioManager.cs` | New sprite slots. `PlayPlantSeed()` exists; add till, transplant and fill-bag |

---

## 5. What has to change — backend

Compile, then commit and push; Railway redeploys.

- `src/main/java/com/agridabao/api/farm/DistrictSeedPools.java` — the new materials per
  district, **identical** to the Unity table.
- `src/main/java/com/agridabao/api/farm/EconomyJsonService.java` — add each material to
  `TRADABLE_ITEMS` and to `BASE_VALUES` (centavos; for example `PineappleSeed → 1000` is
  P10). A material missing from these cannot be bought on the marketplace or traded, and the
  game will look broken.
- `src/main/java/com/agridabao/api/farm/WeatherMitigationTradableItems.java` — check whether
  any ground-preparation kit belongs there.
- **No Flyway migration is needed.** The farm save is a JSON snapshot, and the server accepts
  any `schemaVersion` of 1 or more.

---

## 6. Assets the user is generating

**Meshy — required:** `SeedlingTent`, `SeedlingBag_Empty`, `SeedlingBag_Filled`, and nine
sprouts: `Sprout_Cacao`, `Sprout_Durian`, `Sprout_Mangosteen`, `Sprout_Banana`,
`Sprout_Pomelo`, `Sprout_Mango`, `Sprout_Tomato`, `Sprout_Eggplant`, `Sprout_Squash`.
Nine, not thirteen: corn, pineapple, coconut, strawberry and the banana sucker never sit in a
bag.

**Meshy — optional:** `NurseryRack`, `Seednut_Coconut`, `Sucker_Banana`,
`Sucker_Pineapple`, `Runner_Strawberry`.

**Model specs:** pivot at the base, Y-up, real-world scale in metres, one material per model,
1024 textures, low triangle count. The Oppo A3 is the tightest test device.

**Artlist — UI sprites:** hanging sign "Seedling Tent"; round HUD button icon "Seedling
Tent"; seedling bag icon, empty; seedling bag icon, with sprout; a till or prepare-ground tool
icon only if a new tool is added rather than reusing the shovel.

**Artlist — new item icons:** `BananaPlantlet`, `BananaSucker`, `MangoGraftedSeedling`,
`MangoLiso`, `CoconutSeednut`, `PineappleSucker`, `StrawberryRunner`, `SquashSeedling`.

**Item icons to repaint or reuse:** `CacaoSeed`, `DurianSeed`, `MangosteenSeed`,
`PomeloSeed`, `TomatoSeed`, `EggplantSeed`, `SquashSeed`, `CornSeed`.

**Sound effects:** watering splash, tilling or digging soil, transplanting, filling a bag.

**Reuse, do not commission:** board backgrounds, buttons, planks, arrows and close buttons.
The tent panel sits on the existing board art, the same way the backpack and marketplace
panels do.

---

## 7. Decisions still open — ask the user

1. **Seedling tent.** A walk-up structure on the same farm (recommended; much cheaper), or a
   separate interior the player teleports into (what the user first described).
2. **Raised-bed overlap.** The game *already* has a `RaisedBedKit`, saved per crop as
   `maintenance.hasRaisedBed` and `raisedBedPatchId`, and the adviser limits it to pineapple,
   tomato, strawberry, squash and eggplant — the same five crops as the new bed preparation.
   Reuse that item and flag, or make the ground preparation a separate step?
3. **Prices for the 16 materials**, from the same Department of Agriculture and Davao City
   sources the paper already cites. Suckers, runners and grafted seedlings have real prices in
   those documents.
4. **District availability** of each new material, and whether a new farm's starting items
   can include them.

---

## 8. Build order and scope

1. Item and route data (needs no art).
2. The three ground preparations.
3. Seedling tent, bag state and timers, and the save format (schema 4; old saves still load).
4. Shop prices and district pools in both repositories; push the backend.
5. Wire in the Meshy models and Artlist sprites as they arrive.
6. Tutorial, daily objectives, adviser reference and crop information text.
7. UI polish.

**In scope for this early stage:** the 16 materials as real items, nursery versus direct
routes, the tent as a farm structure with a panel, bag states and timers, the three ground
preparations, transplanting, and crop information text showing real-world timings.

**Out, for later:** the walkable tent interior, the almanac, per-crop field sprout art, extra
maintenance mechanics, character animation.

**After it works:** Appendix F/G, the test forms and the website describe the old flow ("buy
seed → dig → plant"). They need a pass, and the affected test cases need rerunning, before
the 14 October defense.

---

## 9. Unity MCP

**Status on 23 September:** not installed yet. `claude mcp list` showed only Figma and
Artlist, and `Packages/manifest.json` had no MCP package. The user is setting up **MCP for
Unity** (CoplayDev, MIT licence, free) from the Package Manager git URL
`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`. It needs Python 3.10+
(3.13.2 is installed) and `uv` (was not installed). Its HTTP server defaults to port 8080,
the same port the Spring Boot backend binds when run locally, so it was to be moved to 8090.
Unity's official MCP (in `com.unity.ai.assistant`) was the alternative, but it needs the
project linked to Unity Cloud and a Unity AI trial or subscription.

**Using MCP safely on this project**

- Build UI in C#, never as scene canvases or prefabs (section 1).
- Never move or rename a script without its `.meta` file.
- Read the Editor console through MCP after every change instead of assuming it compiled.
- Do not trigger an Android build that overwrites the release APK without asking.
- The package runs inside the Editor. Its documentation did not say whether anything ships in
  the player build, so compare the next APK's size with v8.2 (390.7 MB).

---

## 10. Other outstanding work (not planting)

- **Retests the user owes:** ISS-15, ISS-16 and ISS-17 (they need a new APK); Test 5
  farming-action timings on the three phones; FR-8.8 (offering more than you hold in a trade);
  NFR-04 and NFR-05 percentages from UAT.
- **Before a release build:** untick `Show Fps Counter` and `Show Ui Response Timer`.
- **Website:** re-capture `assets/img/shot-climate.jpg`. It shows raw markdown
  (`* **Right:**`) from a build older than the `AiText.StripMarkdown` fix.
- **Tugbok** has no entry in `DavaoDistrictService.FallbackFrames`. This only matters when
  the district texture cannot be read.
- **Source File Catalogue** (on the Desktop) still lists the paths from before the
  reorganisation.
- **Week 3 submission** (Desktop: `Deliverable 1` to `4` and the Day 2 workshop output): needs
  an outside tester and screenshots. The user must confirm checklist step 15 and
  troubleshooting entry 2.

---

## 11. Files in this folder

| File | What it is |
|---|---|
| `PLANTING_SYSTEM_HANDOFF.md` | This brief |
| `crop_production_notes.txt` | The user's crop production notes, exported from their Google Docs. Real timings for nursery, transplant and harvest |
| `crop_materials.txt` | The user's per-crop materials and planting-process lists |
| `compile_unity.py` | Compiles `Assembly-CSharp` from the generated `.csproj` with the .NET 8 Roslyn compiler, without the Editor. Run `python compile_unity.py <label>`; output goes to `out_<label>/` beside it. Useful when the Editor is closed. If the project goes under git, ignore `Handoff/out_*/` |

**Checks the previous session relied on:** `./gradlew compileJava` for the backend;
`compile_unity.py` for the client; read-only probes of `/actuator/health` and of the 401
paths on the live server.
