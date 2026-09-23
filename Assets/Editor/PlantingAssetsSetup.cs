using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Builds the planting system's prefabs from the Meshy models, sets the new
    /// UI images up as sprites, and wires both into the farm scene and the UI
    /// theme. Kept as menu items so any step can be run again after a model or
    /// sprite is replaced.
    ///
    /// The Meshy models all import with a 100x scale on their root and are about
    /// 1.9 units across at that scale, pivot at their centre. Each prefab here
    /// is an empty root standing at the model's base, with the model inside it
    /// scaled to its real size, so every one of them can be placed on the ground
    /// by its root.
    /// </summary>
    public static class PlantingAssetsSetup
    {
        private const string PrefabFolder = "Assets/Prefab/";
        private const string DugSoilMaterialPath = "Assets/Models/DirtSpot/Materials/DugSoil.mat";
        private const float RackShelfHeight = 0.955f;

        private static readonly string[] UiSprites =
        {
            "Assets/Sprites/UI/SeedlingTent.png",
            "Assets/Sprites/UI/SeedlingBagEmpty.png",
            "Assets/Sprites/UI/SeedlingBagWithSprout.png",
            "Assets/Sprites/UI/Seedlingbagnodirt.png",
            "Assets/Sprites/UI/Seedlingbagwithdirt.png",
            "Assets/Sprites/CropFruitsSeeds/BananaPlantlet.png",
            "Assets/Sprites/CropFruitsSeeds/BananaSucker.png",
            "Assets/Sprites/CropFruitsSeeds/MangoGraftedSeedling.png",
            "Assets/Sprites/CropFruitsSeeds/MangoLiso.png",
            "Assets/Sprites/CropFruitsSeeds/CoconutSeednut.png",
            "Assets/Sprites/CropFruitsSeeds/PineappleSucker.png",
            "Assets/Sprites/CropFruitsSeeds/StrawberryRunner.png",
            "Assets/Sprites/CropFruitsSeeds/SquashSeedling.png"
        };

        [MenuItem("AgriDabao/Planting/1. Set Up Sprites")]
        public static void SetUpSprites()
        {
            foreach (string path in UiSprites)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[PlantingSetup] Missing image: " + path);
                    continue;
                }

                // Matches the existing HUD and item icons.
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            Debug.Log("[PlantingSetup] Sprites set up: " + UiSprites.Length);
        }

        [MenuItem("AgriDabao/Planting/2. Build Prefabs")]
        public static void BuildPrefabs()
        {
            Material dugSoil = EnsureDugSoilMaterial();

            // Seedlings: shown in the tent's bags, and for a transplanted crop's
            // first days in the field.
            BuildModelPrefab("Seedling_Cacao", "SproutCacao", 0.18f, true);
            BuildModelPrefab("Seedling_Durian", "SproutDurian", 0.18f, true);
            BuildModelPrefab("Seedling_Mangosteen", "SproutMangosteen", 0.18f, true);
            BuildModelPrefab("Seedling_Pomelo", "SproutPomelo", 0.18f, true);
            BuildModelPrefab("Plantlet_Banana", "SproutBanana", 0.18f, true);
            BuildModelPrefab("Seedling_Mango", "SproutMango", 0.18f, true);
            BuildModelPrefab("Seedling_Tomato", "SproutTomato", 0.15f, true);
            BuildModelPrefab("Seedling_Eggplant", "SproutEggplant", 0.15f, true);
            BuildModelPrefab("Seedling_Squash", "SproutSquash", 0.15f, true);
            BuildModelPrefab("Seedling_Corn", "SproutCorn", 0.15f, true);

            // Planting materials that go straight into the ground.
            BuildModelPrefab("Seednut_Coconut", "Seednut_Coconut", 0.3f, true);
            BuildModelPrefab("Sucker_Banana", "Sucker_Banana", 0.4f, true);
            BuildModelPrefab("Sucker_Pineapple", "Sucker_Pineapple", 0.25f, true);
            BuildModelPrefab("Runner_Strawberry", "Runner_Strawberry", 0.15f, true);

            // Seedling bags, about 20 cm tall like a real polybag.
            BuildModelPrefab("SeedlingBag_Empty", "SeedlingBag_Empty", 0.1f, false);
            BuildModelPrefab("SeedlingBag_Filled", "SeedlingBag_Filled", 0.105f, false);

            BuildGroundPrefabs(dugSoil);
            BuildTentPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("[PlantingSetup] Prefabs built in " + PrefabFolder);
        }

        [MenuItem("AgriDabao/Planting/3. Wire TerrainPreview Scene")]
        public static void WireScene()
        {
            FarmingInteractionSystem farming = Object.FindFirstObjectByType<FarmingInteractionSystem>();
            InventoryUIBuilder inventory = Object.FindFirstObjectByType<InventoryUIBuilder>();

            if (farming == null || inventory == null)
            {
                Debug.LogError("[PlantingSetup] Open the TerrainPreview scene first.");
                return;
            }

            Undo.RecordObject(farming, "Wire planting prefabs");
            farming.tilledGroundPrefab = Prefab("Ground_Tilled");
            farming.plantingHolePrefab = Prefab("Ground_PlantingHole");
            farming.raisedBedPrefab = Prefab("Ground_RaisedBed");
            farming.furrowPrefab = Prefab("Ground_Furrow");
            farming.bedMulchPrefab = Prefab("Ground_BedMulch");
            farming.seedlingTentPrefab = Prefab("SeedlingTent");
            farming.seedlingBagEmptyPrefab = Prefab("SeedlingBag_Empty");
            farming.seedlingBagFilledPrefab = Prefab("SeedlingBag_Filled");
            farming.cacaoSeedlingPrefab = Prefab("Seedling_Cacao");
            farming.durianSeedlingPrefab = Prefab("Seedling_Durian");
            farming.mangosteenSeedlingPrefab = Prefab("Seedling_Mangosteen");
            farming.pomeloSeedlingPrefab = Prefab("Seedling_Pomelo");
            farming.bananaPlantletPrefab = Prefab("Plantlet_Banana");
            farming.mangoSeedlingPrefab = Prefab("Seedling_Mango");
            farming.tomatoSeedlingPrefab = Prefab("Seedling_Tomato");
            farming.eggplantSeedlingPrefab = Prefab("Seedling_Eggplant");
            farming.squashSeedlingPrefab = Prefab("Seedling_Squash");
            farming.cornSeedlingPrefab = Prefab("Seedling_Corn");
            farming.coconutSeednutPrefab = Prefab("Seednut_Coconut");
            farming.bananaSuckerPrefab = Prefab("Sucker_Banana");
            farming.pineappleSuckerPrefab = Prefab("Sucker_Pineapple");
            farming.strawberryRunnerPrefab = Prefab("Runner_Strawberry");
            EditorUtility.SetDirty(farming);

            Undo.RecordObject(inventory, "Wire planting icons");
            inventory.bananaPlantletSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/BananaPlantlet.png");
            inventory.bananaSuckerSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/BananaSucker.png");
            inventory.mangoGraftedSeedlingSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/MangoGraftedSeedling.png");
            inventory.mangoLisoSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/MangoLiso.png");
            inventory.coconutSeednutSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/CoconutSeednut.png");
            inventory.pineappleSuckerSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/PineappleSucker.png");
            inventory.strawberryRunnerSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/StrawberryRunner.png");
            inventory.squashSeedlingSprite = LoadSprite("Assets/Sprites/CropFruitsSeeds/SquashSeedling.png");
            EditorUtility.SetDirty(inventory);

            UIThemeSprites theme = AssetDatabase.LoadAssetAtPath<UIThemeSprites>("Assets/Resources/UITheme.asset");
            if (theme != null)
            {
                Undo.RecordObject(theme, "Wire Seedling Tent sprites");
                theme.seedlingTentButton = LoadSprite("Assets/Sprites/UI/SeedlingTent.png");
                theme.seedlingBagNoSoil = LoadSprite("Assets/Sprites/UI/Seedlingbagnodirt.png");
                theme.seedlingBagWithSoil = LoadSprite("Assets/Sprites/UI/Seedlingbagwithdirt.png");
                theme.seedlingBagEmptyIcon = LoadSprite("Assets/Sprites/UI/SeedlingBagEmpty.png");
                theme.seedlingBagSproutIcon = LoadSprite("Assets/Sprites/UI/SeedlingBagWithSprout.png");
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkSceneDirty(farming.gameObject.scene);
            Debug.Log("[PlantingSetup] TerrainPreview wired. Save the scene to keep it.");
        }

        // ----------------------------------------------------------- building

        private static void BuildModelPrefab(string prefabName, string modelFolder, float scale, bool collider)
        {
            GameObject root = new GameObject(prefabName);
            AddModel(root.transform, modelFolder, scale, Vector3.one, "Model");

            if (collider)
                FitBoxCollider(root, false);

            Save(root, prefabName);
        }

        /// <summary>
        /// Prepared ground, drawn from the dug-soil model: flattened for tilled
        /// ground and the top of a raised bed, with a dark opening for a hole and
        /// a trench between two ridges for a furrow.
        /// </summary>
        private static void BuildGroundPrefabs(Material dugSoil)
        {
            GameObject tilled = new GameObject("Ground_Tilled");
            AddModel(tilled.transform, "DirtSpot", 1f, new Vector3(1.15f, 0.30f, 1.15f), "Soil");
            Save(tilled, "Ground_Tilled");

            GameObject hole = new GameObject("Ground_PlantingHole");
            GameObject holeSoil = AddModel(hole.transform, "DirtSpot", 1f, new Vector3(0.95f, 0.40f, 0.95f), "Soil");
            float soilTop = WorldBounds(holeSoil).max.y;
            AddPrimitive(hole.transform, PrimitiveType.Cylinder, "Hole", dugSoil,
                new Vector3(0f, soilTop - 0.012f, 0f), new Vector3(0.62f, 0.012f, 0.62f));
            GameObject spoil = AddModel(hole.transform, "DirtSpot", 1f, new Vector3(0.32f, 0.75f, 0.32f), "DugOutSoil");
            spoil.transform.parent.localPosition = new Vector3(1.0f, 0f, 0.35f);
            Save(hole, "Ground_PlantingHole");

            GameObject furrow = new GameObject("Ground_Furrow");
            GameObject left = AddModel(furrow.transform, "DirtSpot", 1f, new Vector3(0.30f, 0.55f, 1.45f), "RidgeLeft");
            left.transform.parent.localPosition = new Vector3(-0.42f, 0f, 0f);
            GameObject right = AddModel(furrow.transform, "DirtSpot", 1f, new Vector3(0.30f, 0.55f, 1.45f), "RidgeRight");
            right.transform.parent.localPosition = new Vector3(0.42f, 0f, 0f);
            AddPrimitive(furrow.transform, PrimitiveType.Cube, "Trench", dugSoil,
                new Vector3(0f, 0.01f, 0f), new Vector3(0.36f, 0.02f, 2.3f));
            Save(furrow, "Ground_Furrow");

            GameObject bed = new GameObject("Ground_RaisedBed");
            AddModel(bed.transform, "DirtSpot", 1f, new Vector3(1.2f, 0.35f, 0.9f), "BedSoil");
            Save(bed, "Ground_RaisedBed");

            GameObject bedMulch = new GameObject("Ground_BedMulch");
            AddModel(bedMulch.transform, "MulchPatch", 1f, new Vector3(1.1f, 0.14f, 0.85f), "Mulch");
            Save(bedMulch, "Ground_BedMulch");
        }

        /// <summary>
        /// The Seedling Tent: the tent, two racks along its side walls, and eight
        /// bag positions on the racks' middle shelves. The tent is a trigger so the
        /// player can walk in among the racks; the racks are solid.
        /// </summary>
        private static void BuildTentPrefab()
        {
            GameObject root = new GameObject("SeedlingTent");
            GameObject tentModel = AddModel(root.transform, "SeedlingTent", 3f, Vector3.one, "Model");
            Bounds tentBounds = WorldBounds(tentModel);

            BoxCollider tapTarget = root.AddComponent<BoxCollider>();
            tapTarget.isTrigger = true;
            tapTarget.center = tentBounds.center;
            tapTarget.size = tentBounds.size;

            float rackX = tentBounds.extents.x - 0.62f;
            List<Transform> anchors = new List<Transform>();
            anchors.AddRange(BuildRack(root.transform, "RackLeft", new Vector3(-rackX, 0f, 0f), 90f));
            anchors.AddRange(BuildRack(root.transform, "RackRight", new Vector3(rackX, 0f, 0f), -90f));

            SeedlingTentInstance tent = root.AddComponent<SeedlingTentInstance>();
            tent.bagAnchors = anchors.ToArray();

            Save(root, "SeedlingTent");
        }

        private static List<Transform> BuildRack(Transform parent, string name, Vector3 position, float yaw)
        {
            GameObject rack = new GameObject(name);
            rack.transform.SetParent(parent, false);

            GameObject model = AddModel(rack.transform, "NurseryRack", 0.85f, Vector3.one, "Model");
            Bounds bounds = WorldBounds(model);
            BoxCollider solid = rack.AddComponent<BoxCollider>();
            solid.center = bounds.center;
            solid.size = bounds.size;

            List<Transform> anchors = new List<Transform>();
            float usable = bounds.size.x - 0.3f;
            for (int i = 0; i < 4; i++)
            {
                GameObject anchor = new GameObject("BagAnchor_" + i);
                anchor.transform.SetParent(rack.transform, false);
                float x = -usable * 0.5f + usable * (i + 0.5f) / 4f;
                anchor.transform.localPosition = new Vector3(x, RackShelfHeight, bounds.center.z);
                anchors.Add(anchor.transform);
            }

            rack.transform.localPosition = position;
            rack.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return anchors;
        }

        /// <summary>
        /// Adds a model under a holder whose scale reshapes it, then seats it so its
        /// base is on the holder's origin and it is centred over it. Returns the
        /// model instance; its parent is the holder.
        /// </summary>
        private static GameObject AddModel(Transform parent, string modelFolder, float scale, Vector3 shape, string name)
        {
            GameObject source = LoadModel(modelFolder);

            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localScale = shape;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, holder.transform);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = source.transform.localRotation;
            instance.transform.localScale = source.transform.localScale * scale;

            // Built at the world origin with no rotation, so world and local agree.
            Bounds bounds = WorldBounds(instance);
            Vector3 holderPosition = holder.transform.position;
            Vector3 correction = new Vector3(
                holderPosition.x - bounds.center.x,
                holderPosition.y - bounds.min.y,
                holderPosition.z - bounds.center.z);
            instance.transform.position += correction;

            return instance;
        }

        private static void AddPrimitive(Transform parent, PrimitiveType type, string name, Material material,
            Vector3 position, Vector3 scale)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void FitBoxCollider(GameObject root, bool trigger)
        {
            Bounds bounds = WorldBounds(root);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.isTrigger = trigger;
            box.center = bounds.center - root.transform.position;
            box.size = bounds.size;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject LoadModel(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models/" + folder });
            if (guids.Length == 0)
                throw new System.InvalidOperationException("No model found in Assets/Models/" + folder);

            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static Material EnsureDugSoilMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DugSoilMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = "DugSoil" };
            material.SetColor("_BaseColor", new Color(0.17f, 0.11f, 0.07f, 1f));
            material.SetFloat("_Smoothness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, DugSoilMaterialPath);
            return material;
        }

        private static void Save(GameObject root, string prefabName)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject Prefab(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefabName + ".prefab");
            if (prefab == null)
                Debug.LogWarning("[PlantingSetup] Missing prefab " + prefabName + " - run Build Prefabs first.");
            return prefab;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning("[PlantingSetup] Not a sprite yet: " + path + " - run Set Up Sprites first.");
            return sprite;
        }
    }
}
