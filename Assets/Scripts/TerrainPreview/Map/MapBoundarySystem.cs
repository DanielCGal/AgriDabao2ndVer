using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public class MapBoundarySystem : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "TerrainPreview" &&
                Object.FindFirstObjectByType<MapBoundarySystem>() == null)
            {
                new GameObject("MapBoundarySystem").AddComponent<MapBoundarySystem>();
            }
        }

        [Header("Collision Wall")]
        public bool buildWalls = true;
        [Tooltip("How far inside the terrain edge the wall sits (metres). The player stops just before the visible edge.")]
        public float wallInset = 1.5f;
        [Tooltip("Wall thickness (metres).")]
        public float wallThickness = 4f;
        [Tooltip("How far the wall extends above the highest terrain point (metres) - keep tall so it can't be jumped.")]
        public float wallHeightAbove = 120f;
        [Tooltip("How far the wall extends below the terrain base (metres).")]
        public float wallDepthBelow = 60f;

        [Header("Edge Fog Curtain")]
        public bool buildFog = true;

        [Tooltip("RECOMMENDED FOR DEVICE BUILDS. A material using an unlit transparent " +
                 "shader (e.g. Sprites/Default). Assigning an asset here guarantees the " +
                 "shader survives build stripping; otherwise the fog can vanish or turn " +
                 "magenta on a phone.")]
        public Material fogMaterialAsset;
        [Tooltip("Fog tint at full daylight.")]
        public Color dayFogColor = Color.white;
        [Tooltip("Fog tint at full night.")]
        public Color nightFogColor = Color.black;
        [Tooltip("Overall fog opacity at its densest point (0 = invisible, 1 = fully opaque).")]
        [Range(0f, 1f)]
        public float fogOpacity = 0.85f;
        [Tooltip("How far outside the terrain edge the fog curtain sits (metres).")]
        public float fogOutsideOffset = 2f;
        [Tooltip("How far the fog rises above the highest terrain point (metres). Kept short so it reads as ground mist, not a tall wall.")]
        public float fogHeightAbove = 8f;
        [Tooltip("How far the fog extends below the terrain base to bury the void (metres).")]
        public float fogDepthBelow = 80f;
        [Tooltip("Fraction of the curtain (from the bottom) that stays fully dense before it starts fading out toward the top. Kept small so it fades gradually almost right away.")]
        [Range(0f, 1f)]
        public float fogDensePortion = 0.12f;

        private bool built;
        private Material fogMaterial;
        private readonly List<GameObject> created = new List<GameObject>();

        private void Start()
        {
            StartCoroutine(WaitForTerrainThenBuild());
        }

        private void Update()
        {
            if (fogMaterial == null)
                return;

            Color tint = Color.Lerp(nightFogColor, dayFogColor, GetDaylightAmount01());
            fogMaterial.color = new Color(tint.r, tint.g, tint.b, fogOpacity);
        }

        private static float GetDaylightAmount01()
        {
            if (GameTimeSystem.Instance == null)
                return 1f;

            float timeOfDay01 = GameTimeSystem.Instance.TimeOfDay01;
            float daylight = Mathf.Clamp01(Mathf.Sin((timeOfDay01 - 0.25f) * Mathf.PI * 2f));
            return Mathf.Pow(daylight, 0.55f);
        }

        private IEnumerator WaitForTerrainThenBuild()
        {
            Terrain terrain = null;

            while (terrain == null)
            {
                terrain = ResolveTerrain();
                if (terrain == null)
                    yield return null;
            }

            Build(terrain);
        }

        private static Terrain ResolveTerrain()
        {
            TemporaryTerrainGenerator generator =
                Object.FindFirstObjectByType<TemporaryTerrainGenerator>();

            if (generator != null && generator.targetTerrain != null)
                return generator.targetTerrain;

            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain;

            return Object.FindFirstObjectByType<Terrain>();
        }

        private void Build(Terrain terrain)
        {
            if (built || terrain == null || terrain.terrainData == null)
                return;

            built = true;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            float minX = origin.x;
            float maxX = origin.x + size.x;
            float minZ = origin.z;
            float maxZ = origin.z + size.z;
            float centerX = origin.x + size.x * 0.5f;
            float centerZ = origin.z + size.z * 0.5f;

            if (buildWalls)
                BuildWalls(origin, size, minX, maxX, minZ, maxZ, centerX, centerZ);

            if (buildFog)
                BuildFog(origin, size, minX, maxX, minZ, maxZ, centerX, centerZ);
        }

        private void BuildWalls(
            Vector3 origin, Vector3 size,
            float minX, float maxX, float minZ, float maxZ,
            float centerX, float centerZ)
        {
            float bottomY = origin.y - wallDepthBelow;
            float topY = origin.y + size.y + wallHeightAbove;
            float wallCenterY = (bottomY + topY) * 0.5f;
            float wallHeight = topY - bottomY;

            float lengthX = (maxX - minX) + wallThickness * 2f;
            float lengthZ = (maxZ - minZ) + wallThickness * 2f;

            CreateWall("BoundaryWall_North",
                new Vector3(centerX, wallCenterY, maxZ - wallInset),
                new Vector3(lengthX, wallHeight, wallThickness));

            CreateWall("BoundaryWall_South",
                new Vector3(centerX, wallCenterY, minZ + wallInset),
                new Vector3(lengthX, wallHeight, wallThickness));

            CreateWall("BoundaryWall_East",
                new Vector3(maxX - wallInset, wallCenterY, centerZ),
                new Vector3(wallThickness, wallHeight, lengthZ));

            CreateWall("BoundaryWall_West",
                new Vector3(minX + wallInset, wallCenterY, centerZ),
                new Vector3(wallThickness, wallHeight, lengthZ));
        }

        private void CreateWall(string wallName, Vector3 center, Vector3 boxSize)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(transform, false);
            wall.transform.position = center;

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = boxSize;

            created.Add(wall);
        }

        private void BuildFog(
            Vector3 origin, Vector3 size,
            float minX, float maxX, float minZ, float maxZ,
            float centerX, float centerZ)
        {
            float bottomY = origin.y - fogDepthBelow;
            float topY = origin.y + size.y + fogHeightAbove;
            float fogCenterY = (bottomY + topY) * 0.5f;
            float fogHeight = topY - bottomY;

            fogMaterial = CreateFogMaterial();

            if (fogMaterial == null)
                return;

            float lengthX = (maxX - minX) + fogOutsideOffset * 2f;
            float lengthZ = (maxZ - minZ) + fogOutsideOffset * 2f;

            CreateFogPanel("EdgeFog_North", fogMaterial,
                new Vector3(centerX, fogCenterY, maxZ + fogOutsideOffset),
                Quaternion.identity, lengthX, fogHeight);

            CreateFogPanel("EdgeFog_South", fogMaterial,
                new Vector3(centerX, fogCenterY, minZ - fogOutsideOffset),
                Quaternion.identity, lengthX, fogHeight);

            Quaternion sideRotation = Quaternion.Euler(0f, 90f, 0f);

            CreateFogPanel("EdgeFog_East", fogMaterial,
                new Vector3(maxX + fogOutsideOffset, fogCenterY, centerZ),
                sideRotation, lengthZ, fogHeight);

            CreateFogPanel("EdgeFog_West", fogMaterial,
                new Vector3(minX - fogOutsideOffset, fogCenterY, centerZ),
                sideRotation, lengthZ, fogHeight);
        }

        private void CreateFogPanel(
            string panelName, Material material,
            Vector3 center, Quaternion rotation, float width, float height)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = panelName;
            panel.transform.SetParent(transform, false);
            panel.transform.SetPositionAndRotation(center, rotation);
            panel.transform.localScale = new Vector3(width, height, 1f);

            Collider panelCollider = panel.GetComponent<Collider>();
            if (panelCollider != null)
                Destroy(panelCollider);

            MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            created.Add(panel);
        }

        private Material CreateFogMaterial()
        {
            if (fogMaterialAsset != null)
                return new Material(fogMaterialAsset);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            if (shader == null)
            {
                Debug.LogWarning(
                    "MapBoundarySystem: no fog shader found. Assign a Fog Material " +
                    "asset, or add 'Sprites/Default' to Project Settings > Graphics > " +
                    "Always Included Shaders so it survives build stripping.");
                return null;
            }

            Color initialTint = Color.Lerp(nightFogColor, dayFogColor, GetDaylightAmount01());

            Material material = new Material(shader)
            {
                mainTexture = CreateFogGradientTexture(),
                color = new Color(initialTint.r, initialTint.g, initialTint.b, fogOpacity)
            };

            return material;
        }

        private Texture2D CreateFogGradientTexture()
        {
            const int width = 4;
            const int height = 128;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);

                float alpha = v <= fogDensePortion
                    ? 1f
                    : 1f - Mathf.SmoothStep(fogDensePortion, 1f, v);

                Color pixel = new Color(1f, 1f, 1f, alpha);
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, pixel);
            }

            texture.Apply();
            return texture;
        }
    }
}
