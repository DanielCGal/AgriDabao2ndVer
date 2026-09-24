using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class SoilAwareTerrainGenerator : MonoBehaviour
    {
        [Header("Base Generation")]
        public TemporaryTerrainGenerator baseGenerator;
        public SoilGridsService soilService;

        [Header("Soil Grid")]
        public int soilGridResolution = 64;
        public float randomVariation = 0.12f;

        [Header("UI")]
        public Text soilInfoText;

        [Header("District")]
        public DavaoDistrictService districtService;

        [Header("Terrain Grass Texture")]
        public Texture2D grassGroundTexture;
        public Vector2 grassTextureTileSize = new Vector2(25f, 25f);

        [Header("Fallback Grass Look")]
        public Color grassBaseColor = new Color(0.22f, 0.55f, 0.18f);
        public Color grassLightColor = new Color(0.36f, 0.72f, 0.26f);
        public float grassNoiseScale = 18f;
        public float grassFineNoiseScale = 55f;



        private SoilSample[,] soilGrid;
        private Terrain terrain;

        private SoilInfoPanelBuilder soilInfoPanel;

        private void EnsureSoilInfoText()
        {
            if (soilInfoPanel == null)
                soilInfoPanel = Object.FindFirstObjectByType<SoilInfoPanelBuilder>();

            if (soilInfoText == null && soilInfoPanel != null)
                soilInfoText = soilInfoPanel.createdText;
        }

        public void ShowMessage(string text)
        {
            SetInspectionText(text);
        }

        private void SetInspectionText(string text)
        {
            EnsureSoilInfoText();

            if (soilInfoPanel != null)
            {
                soilInfoPanel.ShowInfo(text);
                return;
            }

            if (soilInfoText != null)
                soilInfoText.text = text;
        }

        private void EnsureDistrictService()
        {
            if (districtService != null) return;
            districtService = Object.FindFirstObjectByType<DavaoDistrictService>();
        }

        private IEnumerator Start()
        {
            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.Show("Preparing selected area...");

            yield return null;

            EnsureDistrictService();

            EnsureSoilInfoText();

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.SetStatus("Generating terrain from official Davao elevation and slope maps...");

            yield return null;

            baseGenerator.Generate();
            terrain = baseGenerator.targetTerrain;

            yield return null;

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.SetStatus("Positioning player on generated terrain...");

            FirstPersonTerrainController player = Object.FindFirstObjectByType<FirstPersonTerrainController>();
            if (player != null)
            {
                player.SnapToTerrain(terrain, 2f);
            }

            yield return null;

            Rect selected = SelectedAreaState.NormalizedRect;
            FarmSnapshotDto restoreSnapshot = FarmLoadContext.PendingSnapshot;

            Vector2 a = DavaoGeoReference.GetSamplePointA(selected);
            Vector2 b = DavaoGeoReference.GetSamplePointB(selected);

            SoilSample sampleA = null;
            SoilSample sampleB = null;

            bool canRestoreSoil =
                restoreSnapshot != null &&
                restoreSnapshot.soil != null &&
                restoreSnapshot.soil.sampleA != null &&
                restoreSnapshot.soil.sampleB != null;

            if (canRestoreSoil)
            {
                if (WorldLoadingScreen.Instance != null)
                    WorldLoadingScreen.Instance.SetStatus("Restoring saved soil samples...");

                sampleA = restoreSnapshot.soil.sampleA;
                sampleB = restoreSnapshot.soil.sampleB;
                a = new Vector2(
                    restoreSnapshot.soil.sampleALatitude,
                    restoreSnapshot.soil.sampleALongitude
                );
                b = new Vector2(
                    restoreSnapshot.soil.sampleBLatitude,
                    restoreSnapshot.soil.sampleBLongitude
                );
            }
            else
            {
                if (WorldLoadingScreen.Instance != null)
                    WorldLoadingScreen.Instance.SetStatus("Requesting soil information from SoilGrids...");

                yield return StartCoroutine(soilService.QuerySoil(a.x, a.y, s => sampleA = s));

                if (WorldLoadingScreen.Instance != null)
                    WorldLoadingScreen.Instance.SetStatus("Analyzing second soil sample...");

                yield return StartCoroutine(soilService.QuerySoil(b.x, b.y, s => sampleB = s));
            }

            if (sampleA == null || sampleB == null)
            {
                Debug.LogError("Soil data is unavailable. The farm world cannot be finalized.");
                if (WorldLoadingScreen.Instance != null)
                    WorldLoadingScreen.Instance.SetStatus("Could not load soil data.");
                yield break;
            }

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.SetStatus("Building soil grid for the selected area...");

            yield return null;

            BuildSoilGrid(sampleA, sampleB);

            yield return null;

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.SetStatus("Applying grass texture and terrain visuals...");

            yield return null;

            ApplyGrassTextureOverlay();

            yield return null;

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.SetStatus("Finalizing farm world...");

            yield return new WaitForSeconds(0.35f);

            if (FarmPersistenceManager.Instance != null)
                FarmPersistenceManager.Instance.NotifyWorldReady(sampleA, sampleB, a, b);

            if (WorldLoadingScreen.Instance != null)
                WorldLoadingScreen.Instance.Hide();
        }

        private void ApplyGrassTextureOverlay()
        {
            if (terrain == null)
                return;

            TerrainData td = terrain.terrainData;

            TerrainLayer layer = new TerrainLayer();

            if (grassGroundTexture != null)
            {
                layer.diffuseTexture = grassGroundTexture;
                layer.tileSize = grassTextureTileSize;
            }
            else
            {
                layer.diffuseTexture = GenerateFallbackGrassTexture();
                layer.tileSize = new Vector2(td.size.x, td.size.z);
            }

            layer.metallic = 0f;
            layer.smoothness = 0f;
            layer.normalScale = 0.35f;

            TemporaryTerrainGenerator.ClearSmoothnessAlpha(layer.diffuseTexture as Texture2D);

            td.terrainLayers = new TerrainLayer[] { layer };

            float[,,] splat = new float[td.alphamapResolution, td.alphamapResolution, 1];

            for (int y = 0; y < td.alphamapResolution; y++)
            {
                for (int x = 0; x < td.alphamapResolution; x++)
                {
                    splat[y, x, 0] = 1f;
                }
            }

            td.SetAlphamaps(0, 0, splat);
        }

        private Texture2D GenerateFallbackGrassTexture()
        {
            int texSize = 1024;
            Texture2D grassTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float u = x / (float)(texSize - 1);
                    float v = y / (float)(texSize - 1);

                    float largeNoise = Mathf.PerlinNoise(
                        u * grassNoiseScale + 13.7f,
                        v * grassNoiseScale + 8.4f
                    );

                    float fineNoise = Mathf.PerlinNoise(
                        u * grassFineNoiseScale + 31.2f,
                        v * grassFineNoiseScale + 19.8f
                    );

                    float blend = largeNoise * 0.75f + fineNoise * 0.25f;

                    Color c = Color.Lerp(grassBaseColor, grassLightColor, blend);
                    grassTex.SetPixel(x, y, c);
                }
            }

            grassTex.Apply();
            return grassTex;
        }

        private void BuildSoilGrid(SoilSample a, SoilSample b)
        {
            soilGrid = new SoilSample[soilGridResolution, soilGridResolution];

            for (int y = 0; y < soilGridResolution; y++)
            {
                for (int x = 0; x < soilGridResolution; x++)
                {
                    float u = x / (float)(soilGridResolution - 1);
                    float v = y / (float)(soilGridResolution - 1);

                    float diagonalBlend = (u * 0.65f) + (v * 0.35f);
                    float zoneNoise = Mathf.PerlinNoise(u * 2.2f + 17.3f, v * 2.2f + 9.1f) - 0.5f;
                    float patchNoise = Mathf.PerlinNoise(u * 5.8f + 41.7f, v * 5.8f + 12.4f) - 0.5f;

                    float t = Mathf.Clamp01(diagonalBlend + zoneNoise * 0.45f + patchNoise * 0.18f);

                    SoilSample s = new SoilSample();
                    s.sand = Mathf.Lerp(a.sand, b.sand, t);
                    s.silt = Mathf.Lerp(a.silt, b.silt, t);
                    s.clay = Mathf.Lerp(a.clay, b.clay, t);
                    s.phh2o = Mathf.Lerp(a.phh2o, b.phh2o, t) + patchNoise * 0.15f;
                    s.soc = Mathf.Lerp(a.soc, b.soc, t) + zoneNoise * 3.0f;
                    s.cfvo = Mathf.Lerp(a.cfvo, b.cfvo, t) + patchNoise * 4.0f;
                    s.bdod = Mathf.Lerp(a.bdod, b.bdod, t) + zoneNoise * 6.0f;
                    s.nitrogen = Mathf.Lerp(a.nitrogen, b.nitrogen, t) + patchNoise * 2.5f;

                    s.sand = Mathf.Max(0f, s.sand);
                    s.silt = Mathf.Max(0f, s.silt);
                    s.clay = Mathf.Max(0f, s.clay);
                    s.phh2o = Mathf.Clamp(s.phh2o, 3.5f, 9.5f);
                    s.soc = Mathf.Clamp(s.soc, 0f, 100f);
                    s.cfvo = Mathf.Clamp(s.cfvo, 0f, 100f);
                    s.bdod = Mathf.Clamp(s.bdod, 60f, 180f);
                    s.nitrogen = Mathf.Clamp(s.nitrogen, 0f, 100f);

                    NormalizeTextureFractions(s);
                    soilGrid[x, y] = s;
                }
            }
        }

        private void NormalizeTextureFractions(SoilSample s)
        {
            float total = Mathf.Max(0.001f, s.sand + s.silt + s.clay);
            s.sand = (s.sand / total) * 100f;
            s.silt = (s.silt / total) * 100f;
            s.clay = (s.clay / total) * 100f;
        }

        private void ApplySoilColorOverlay()
        {
            if (terrain == null) return;

            int texSize = 1024;
            Texture2D soilTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float u = x / (float)(texSize - 1);
                    float v = y / (float)(texSize - 1);

                    SoilSample s = SampleSoilGrid(u, v);
                    Color c = GetSoilColor(s);

                    float macro = Mathf.PerlinNoise(u * 8.0f + 3.1f, v * 8.0f + 5.7f);
                    c *= Mathf.Lerp(0.94f, 1.06f, macro);

                    soilTex.SetPixel(x, y, c);
                }
            }

            soilTex.Apply();

            TerrainData td = terrain.terrainData;
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = soilTex;
            layer.tileSize = new Vector2(td.size.x, td.size.z);
            td.terrainLayers = new TerrainLayer[] { layer };

            float[,,] splat = new float[td.alphamapResolution, td.alphamapResolution, 1];
            for (int y = 0; y < td.alphamapResolution; y++)
            {
                for (int x = 0; x < td.alphamapResolution; x++)
                {
                    splat[y, x, 0] = 1f;
                }
            }
            td.SetAlphamaps(0, 0, splat);
        }

        private void ApplyGrassColorOverlay()
        {
            if (terrain == null)
                return;

            int texSize = 1024;
            Texture2D grassTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float u = x / (float)(texSize - 1);
                    float v = y / (float)(texSize - 1);

                    float largeNoise = Mathf.PerlinNoise(
                        u * grassNoiseScale + 13.7f,
                        v * grassNoiseScale + 8.4f
                    );

                    float fineNoise = Mathf.PerlinNoise(
                        u * grassFineNoiseScale + 31.2f,
                        v * grassFineNoiseScale + 19.8f
                    );

                    float blend = largeNoise * 0.75f + fineNoise * 0.25f;

                    Color c = Color.Lerp(grassBaseColor, grassLightColor, blend);
                    grassTex.SetPixel(x, y, c);
                }
            }

            grassTex.Apply();

            TerrainData td = terrain.terrainData;

            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = grassTex;
            layer.tileSize = new Vector2(td.size.x, td.size.z);

            td.terrainLayers = new TerrainLayer[] { layer };

            float[,,] splat = new float[td.alphamapResolution, td.alphamapResolution, 1];

            for (int y = 0; y < td.alphamapResolution; y++)
            {
                for (int x = 0; x < td.alphamapResolution; x++)
                {
                    splat[y, x, 0] = 1f;
                }
            }

            td.SetAlphamaps(0, 0, splat);
        }



        private SoilSample SampleSoilGrid(float u, float v)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (soilGridResolution - 1)), 0, soilGridResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (soilGridResolution - 1)), 0, soilGridResolution - 1);
            return soilGrid[x, y];
        }

        private float GetDominanceStrength(SoilSample s)
        {
            float maxValue = Mathf.Max(s.sand, Mathf.Max(s.silt, s.clay));
            float midValue = s.sand + s.silt + s.clay - maxValue - Mathf.Min(s.sand, Mathf.Min(s.silt, s.clay));
            return Mathf.InverseLerp(8f, 28f, maxValue - midValue);
        }

        private Color GetSoilColor(SoilSample s)
        {
            Color sandy = new Color(0.82f, 0.71f, 0.47f);
            Color silty = new Color(0.62f, 0.53f, 0.39f);
            Color clayey = new Color(0.49f, 0.31f, 0.25f);
            Color loamy = new Color(0.66f, 0.50f, 0.34f);
            Color organic = new Color(0.24f, 0.19f, 0.15f);
            Color rocky = new Color(0.53f, 0.51f, 0.49f);

            Color textureBlend =
                sandy * (s.sand / 100f) +
                silty * (s.silt / 100f) +
                clayey * (s.clay / 100f);

            Color dominantType;
            if (s.clay >= s.sand && s.clay >= s.silt)
                dominantType = clayey;
            else if (s.sand >= s.silt)
                dominantType = sandy;
            else
                dominantType = silty;

            float dominance = GetDominanceStrength(s);
            Color baseColor = Color.Lerp(loamy, dominantType, dominance);
            baseColor = Color.Lerp(baseColor, textureBlend, 0.55f);

            if (s.soc >= 35f)
                baseColor = Color.Lerp(baseColor, organic, 0.45f);
            else if (s.soc >= 20f)
                baseColor = Color.Lerp(baseColor, organic, 0.18f);

            if (s.cfvo >= 25f)
                baseColor = Color.Lerp(baseColor, rocky, 0.35f);
            else if (s.cfvo >= 12f)
                baseColor = Color.Lerp(baseColor, rocky, 0.15f);

            if (s.phh2o < 5.5f)
                baseColor = Color.Lerp(baseColor, new Color(0.43f, 0.23f, 0.19f), 0.18f);

            if (s.phh2o > 7.5f)
                baseColor = Color.Lerp(baseColor, new Color(0.74f, 0.70f, 0.56f), 0.18f);

            float densityDarken = Mathf.InverseLerp(80f, 160f, s.bdod);
            baseColor *= Mathf.Lerp(1.06f, 0.84f, densityDarken);

            return baseColor;
        }

        private string GetDistrictNameForTerrainUV(float localU, float localV)
        {
            EnsureDistrictService();

            Rect selected = SelectedAreaState.NormalizedRect;

            float globalU = Mathf.Lerp(selected.xMin, selected.xMax, localU);
            float globalV = Mathf.Lerp(selected.yMin, selected.yMax, localV);

            if (districtService != null &&
                districtService.TryGetDistrictAtNormalized(globalU, globalV, out string district, out bool insideCity, out bool productive))
            {
                if (!insideCity)
                    return "Outside Davao City";

                if (!productive)
                    return district + " (Non-Production Area)";

                return district;
            }

            if (!string.IsNullOrEmpty(SelectedAreaState.SelectedDistrictName))
                return SelectedAreaState.SelectedDistrictName;

            return "Unknown";
        }

        public void InspectAtScreenPosition(Vector2 screenPosition)
        {
            EnsureSoilInfoText();

            if (soilInfoPanel != null)
                soilInfoPanel.HideInfo();
        }

        public bool TryGetSoilAtWorldPosition(Vector3 worldPosition, out SoilSample sample, out string districtName)
        {
            sample = null;
            districtName = "Unknown";

            if (terrain == null || soilGrid == null)
                return false;

            Vector3 local = worldPosition - terrain.transform.position;
            float u = Mathf.Clamp01(local.x / terrain.terrainData.size.x);
            float v = Mathf.Clamp01(local.z / terrain.terrainData.size.z);

            sample = SampleSoilGrid(u, v);
            districtName = GetDistrictNameForTerrainUV(u, v);
            return sample != null;
        }

        public static GameObject LastInspectedCrop { get; private set; }

        public void ShowTreeInfo(CoconutTreeInstance tree)
        {
            if (tree == null)
                return;

            LastInspectedCrop = tree.gameObject;

            SetInspectionText(
                tree.GetInspectionText() +
                PestDiseaseAffectedCrop.GetInspectionTextFor(tree.gameObject));
        }

        public void ShowTreeInfo(BananaPlantInstance tree)
        {
            if (tree == null)
                return;

            LastInspectedCrop = tree.gameObject;

            SetInspectionText(
                tree.GetInspectionText() +
                PestDiseaseAffectedCrop.GetInspectionTextFor(tree.gameObject));
        }

        public void ShowTreeInfo(TropicalCropPlantInstance plant)
        {
            if (plant == null)
                return;

            LastInspectedCrop = plant.gameObject;

            SetInspectionText(
                plant.GetInspectionText() +
                PestDiseaseAffectedCrop.GetInspectionTextFor(plant.gameObject));
        }

        public bool ShowFullCropInfo(GameObject cropObject)
        {
            if (cropObject == null)
                return false;

            CoconutTreeInstance coconut =
                cropObject.GetComponentInParent<CoconutTreeInstance>();
            if (coconut != null)
            {
                ShowTreeInfo(coconut);
                return true;
            }

            BananaPlantInstance banana =
                cropObject.GetComponentInParent<BananaPlantInstance>();
            if (banana != null)
            {
                ShowTreeInfo(banana);
                return true;
            }

            TropicalCropPlantInstance tropical =
                cropObject.GetComponentInParent<TropicalCropPlantInstance>();
            if (tropical != null)
            {
                ShowTreeInfo(tropical);
                return true;
            }

            return false;
        }

        public void ShowPestDiseaseInfo(PestDiseaseAffectedCrop disease)
        {
            if (disease == null)
                return;

            if (ShowFullCropInfo(disease.gameObject))
                return;

            SetInspectionText(
                $"Crop: {disease.CropDisplayName}\n" +
                $"Health: {disease.Health:F1}/100\n" +
                $"Stress: {disease.Stress:F1}/100\n" +
                $"Water: {(disease.Moisture * 100f):F0}%" +
                disease.GetPestInspectionText());
        }



    }
}
