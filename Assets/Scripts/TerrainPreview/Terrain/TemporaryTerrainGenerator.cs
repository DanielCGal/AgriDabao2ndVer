using UnityEngine;

namespace AgriDabao3D
{
    public class TemporaryTerrainGenerator : MonoBehaviour
    {
        [Header("Source Sprites")]
        public Sprite davaoMapSprite;
        public Sprite elevationMapSprite;
        public Sprite slopeMapSprite;

        [Header("Terrain")]
        public Terrain targetTerrain;

        [Tooltip("REQUIRED FOR DEVICE BUILDS. A material using the shader " +
                 "'Universal Render Pipeline/Terrain/Lit'. Without an asset assigned " +
                 "here the terrain renders magenta on a phone, because the terrain is " +
                 "created at runtime and its shader gets stripped from the build.")]
        public Material terrainMaterial;
        public int heightmapResolution = 257;
        public int alphamapResolution = 256;
        public Vector3 terrainSize = new Vector3(700f, 90f, 700f);

        [Header("Official Elevation Map Settings")]
        public float maxLegendElevationMeters = 1700f;
        public float lowlandElevationMeters = 100f;
        public float elevationColorTolerance = 0.55f;

        [Header("Official Slope Map Settings")]
        public float slopeColorTolerance = 0.55f;
        public float slopeReliefMultiplier = 0.65f;

        [Header("Terrain Shaping")]
        [Range(0, 12)] public int blurIterations = 5;
        [Range(0.5f, 3f)] public float heightPower = 0.85f;
        [Range(0.01f, 0.50f)] public float flatAreaHeightScale = 0.08f;
        [Range(0.10f, 1.00f)] public float mountainAreaHeightScale = 0.75f;
        [Range(0f, 0.25f)] public float slopeHeightContribution = 0.10f;

        [Header("Flat Area Feature Hills")]
        public bool addFeatureHillsInFlatAreas = true;
        public float flatTerrainRangeThresholdMeters = 120f;
        [Range(1, 4)] public int flatlandFeatureHillCount = 2;
        public float flatlandHillMinHeightMeters = 45f;
        public float flatlandHillMaxHeightMeters = 130f;
        [Range(0.05f, 0.40f)] public float flatlandHillMinRadius01 = 0.12f;
        [Range(0.05f, 0.45f)] public float flatlandHillMaxRadius01 = 0.26f;
        [Range(0.8f, 3f)] public float flatlandHillShapePower = 1.6f;

        [Header("Noise Detail")]
        public float broadNoiseScale = 1.8f;
        public float mediumNoiseScale = 5.5f;
        public float fineNoiseScale = 13f;

        [Header("Natural Flatland Relief")]
        [Tooltip("Rolling ground for the lowlands.\n\n" +
                 "The metre-space detail added earlier survives neither the blur nor " +
                 "the final normalisation: heights are rescaled between the region's " +
                 "lowest and highest point, so when a mountain is in view a few metres " +
                 "of lowland variation collapse into almost nothing. This layer is " +
                 "added AFTER both steps, in normalised units, so it cannot be flattened.")]
        public bool addNaturalFlatlandRelief = true;

        [Tooltip("Height of the rolling relief as a fraction of Terrain Size Y. " +
                 "At Y=120 a value of 0.045 gives roughly 5m of undulation.")]
        [Range(0f, 0.20f)] public float flatlandReliefStrength = 0.045f;

        [Tooltip("Lower = broader, gentler hills. Higher = tighter, busier ground.")]
        [Range(1f, 16f)] public float flatlandReliefScale = 4.5f;

        [Tooltip("Layers of noise. More octaves add finer bumps on top of the broad shape.")]
        [Range(1, 6)] public int flatlandReliefOctaves = 4;

        [Tooltip("How quickly each octave fades. 0.5 is the natural-looking default.")]
        [Range(0.2f, 0.8f)] public float flatlandReliefPersistence = 0.5f;

        [Header("Preview")]
        public bool autoGenerateOnStart = true;
        public bool applyMapTextureToTerrain = true;
        public bool autoPositionCamera = true;

        private readonly Color[] elevationLegendColors =
        {
            From255(128, 255, 0),
            From255(255, 255, 0),
            From255(255, 166, 0),
            From255(255, 80, 0),
            From255(255, 0, 0)
        };

        private readonly Color[] slopeLegendColors =
        {
            From255(128, 255, 0),
            From255(255, 255, 0),
            From255(255, 180, 0),
            From255(255, 85, 0),
            From255(255, 0, 0)
        };

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Official Davao Terrain")]
        public void Generate()
        {
            if (davaoMapSprite == null || elevationMapSprite == null || slopeMapSprite == null)
            {
                Debug.LogError(
                    "TemporaryTerrainGenerator on \"" + name + "\": assign the Davao satellite map, " +
                    "the official elevation map and the official slope map.", this);
                return;
            }

            Texture2D mapTexture = davaoMapSprite.texture;
            Texture2D elevationTexture = elevationMapSprite.texture;
            Texture2D slopeTexture = slopeMapSprite.texture;

            if (!mapTexture.isReadable || !elevationTexture.isReadable || !slopeTexture.isReadable)
            {
                Debug.LogError(
                    "TemporaryTerrainGenerator on \"" + name + "\": turn on Read/Write Enabled for the " +
                    "satellite map, the elevation map and the slope map.", this);
                return;
            }

            WarnIfSourceMapsDisagree();

            Rect area = SelectedAreaState.NormalizedRect;
            Debug.Log($"Selected area: {area}");

            Texture2D croppedMap = CropTexture(mapTexture, area);
            float[,] heights = BuildHeightsFromOfficialMaps(elevationTexture, slopeTexture, area, heightmapResolution);

            Terrain terrain = GetOrCreateTerrain();

            TerrainData td = new TerrainData();
            td.heightmapResolution = heightmapResolution;
            td.alphamapResolution = alphamapResolution;
            td.size = terrainSize;
            td.SetHeights(0, 0, heights);

            terrain.terrainData = td;

            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider == null)
            {
                terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
            }

            terrainCollider.terrainData = td;
            terrain.Flush();

            if (applyMapTextureToTerrain)
            {
                ApplyMapTexture(td, croppedMap);
            }

            if (autoPositionCamera)
            {
                PositionCamera(terrain);
            }

            Debug.Log("Official Davao elevation/slope terrain generated.");
        }

        private float[,] BuildHeightsFromOfficialMaps(
            Texture2D elevationTexture,
            Texture2D slopeTexture,
            Rect normalizedRect,
            int resolution)
        {
            float[,] rawMeters = new float[resolution, resolution];
            float[,] heights = new float[resolution, resolution];

            float[,] slopeMap = new float[resolution, resolution];

            float min = float.MaxValue;
            float max = float.MinValue;
            float slopeSum = 0f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float localU = x / (float)(resolution - 1);
                    float localV = y / (float)(resolution - 1);

                    float globalU = Mathf.Lerp(normalizedRect.xMin, normalizedRect.xMax, localU);
                    float globalV = Mathf.Lerp(normalizedRect.yMin, normalizedRect.yMax, localV);

                    float elevationRamp = SampleLegendRampRobust(
                        elevationTexture,
                        globalU,
                        globalV,
                        elevationLegendColors,
                        elevationColorTolerance,
                        0f
                    );

                    float slopeRamp = SampleLegendRampRobust(
                        slopeTexture,
                        globalU,
                        globalV,
                        slopeLegendColors,
                        slopeColorTolerance,
                        0f
                    );

                    float baseElevationMeters = GetElevationMeters(elevationRamp);


                    float lowland = Mathf.Clamp01(1f - elevationRamp);
                    if (lowland > 0f)
                    {
                        float hillNoise =
                            Mathf.PerlinNoise(
                                localU * 2.5f,
                                localV * 2.5f
                            );


                        baseElevationMeters +=
                            hillNoise * 15f * lowland;
                    }
                    float slope01 = GetSlopeStrength01(slopeRamp);
                    slopeSum += slope01;
                    slopeMap[y, x] = slope01;

                    float broadNoise = Mathf.PerlinNoise(
                        localU * broadNoiseScale + 13.4f,
                        localV * broadNoiseScale + 7.8f
                    ) - 0.5f;

                    float mediumNoise = Mathf.PerlinNoise(
                        localU * mediumNoiseScale + 41.1f,
                        localV * mediumNoiseScale + 19.6f
                    ) - 0.5f;

                    float fineNoise = Mathf.PerlinNoise(
                        localU * fineNoiseScale + 91.7f,
                        localV * fineNoiseScale + 55.2f
                    ) - 0.5f;

                    float reliefMeters =
    GetSlopeReliefMeters(slopeRamp)
    * slopeReliefMultiplier;


                    reliefMeters *= Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(slopeRamp - 1f));

                    float terrainVariation =
                        broadNoise * reliefMeters * 0.70f +
                        mediumNoise * reliefMeters * 0.45f +
                        fineNoise * reliefMeters * 0.18f;

                    float finalMeters = baseElevationMeters + terrainVariation;

                    rawMeters[y, x] = finalMeters;

                    if (finalMeters < min) min = finalMeters;
                    if (finalMeters > max) max = finalMeters;
                }
            }

            FindMinMax(rawMeters, out min, out max);
            float preSmoothRange = max - min;

            bool shouldAddFeatureHills =
                addFeatureHillsInFlatAreas &&
                preSmoothRange <= flatTerrainRangeThresholdMeters;

            Debug.Log(
                $"Terrain flat check -> range={preSmoothRange:F1}m, addFeatureHills={shouldAddFeatureHills}"
            );

            for (int i = 0; i < 3; i++)
            {
                rawMeters = SmoothElevationMap(rawMeters, resolution);
            }

            if (shouldAddFeatureHills)
            {
                rawMeters = AddFlatlandFeatureHills(rawMeters, resolution, normalizedRect);
            }

            for (int i = 0; i < blurIterations; i++)
            {
                rawMeters = Blur(rawMeters);
            }

            FindMinMax(rawMeters, out min, out max);

            float range = Mathf.Max(0.001f, max - min);
            float averageSlope01 = slopeSum / Mathf.Max(1, resolution * resolution);

            float heightScale = Mathf.Lerp(
                flatAreaHeightScale,
                mountainAreaHeightScale,
                Mathf.InverseLerp(25f, 650f, range)
            );

            heightScale += averageSlope01 * slopeHeightContribution;
            heightScale = Mathf.Clamp01(heightScale);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float normalized = Mathf.InverseLerp(min, max, rawMeters[y, x]);
                    normalized = Mathf.Pow(normalized, heightPower);
                    float value = normalized * heightScale;

                    if (addNaturalFlatlandRelief && flatlandReliefStrength > 0f)
                    {
                        float localU = x / (float)(resolution - 1);
                        float localV = y / (float)(resolution - 1);

                        float flatness = 1f - Mathf.Clamp01(slopeMap[y, x]);
                        float amount = flatlandReliefStrength * flatness;

                        if (amount > 0f)
                        {
                            float detail = Fbm(
                                localU * flatlandReliefScale + 137.2f,
                                localV * flatlandReliefScale + 61.9f,
                                flatlandReliefOctaves,
                                flatlandReliefPersistence);

                            value += amount * detail;
                        }
                    }

                    heights[y, x] = Mathf.Clamp01(value);
                }
            }

            return heights;
        }

        private static float Fbm(float x, float y, int octaves, float persistence)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < Mathf.Max(1, octaves); i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            return maxAmplitude <= 0f ? 0.5f : total / maxAmplitude;
        }

        private float[,] SmoothElevationMap(float[,] source, int resolution)
        {
            float[,] result = new float[resolution, resolution];

            int radius = 6;


            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sum = 0f;
                    int count = 0;


                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            int nx = Mathf.Clamp(x + ox, 0, resolution - 1);
                            int ny = Mathf.Clamp(y + oy, 0, resolution - 1);


                            sum += source[ny, nx];
                            count++;
                        }
                    }


                    float average = sum / count;


                    result[y, x] = Mathf.Lerp(
                        source[y, x],
                        average,
                        0.35f
                    );
                }
            }


            return result;
        }

        private float[,] AddFlatlandFeatureHills(float[,] source, int resolution, Rect normalizedRect)
        {
            float[,] result = (float[,])source.Clone();

            int seed =
                Mathf.Abs(Mathf.RoundToInt(
                    normalizedRect.xMin * 1000f +
                    normalizedRect.yMin * 2000f +
                    normalizedRect.width * 3000f +
                    normalizedRect.height * 4000f
                )) + 1337;

            System.Random rng = new System.Random(seed);

            int hillCount = Mathf.Max(1, flatlandFeatureHillCount);

            float minRadius = Mathf.Min(flatlandHillMinRadius01, flatlandHillMaxRadius01);
            float maxRadius = Mathf.Max(flatlandHillMinRadius01, flatlandHillMaxRadius01);

            float minHeight = Mathf.Min(flatlandHillMinHeightMeters, flatlandHillMaxHeightMeters);
            float maxHeight = Mathf.Max(flatlandHillMinHeightMeters, flatlandHillMaxHeightMeters);

            for (int h = 0; h < hillCount; h++)
            {
                float centerU = Mathf.Lerp(0.18f, 0.82f, (float)rng.NextDouble());
                float centerV = Mathf.Lerp(0.18f, 0.82f, (float)rng.NextDouble());

                float radius = Mathf.Lerp(minRadius, maxRadius, (float)rng.NextDouble());
                float hillHeight = Mathf.Lerp(minHeight, maxHeight, (float)rng.NextDouble());

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float u = x / (float)(resolution - 1);
                        float v = y / (float)(resolution - 1);

                        float dx = (u - centerU) / radius;
                        float dy = (v - centerV) / radius;

                        float distance = Mathf.Sqrt(dx * dx + dy * dy);

                        if (distance > 1f)
                            continue;

                        float t = 1f - distance;
                        float dome = Mathf.Pow(
                            Mathf.SmoothStep(0f, 1f, t),
                            flatlandHillShapePower
                        );

                        float naturalUnevenness = Mathf.Lerp(
                            0.85f,
                            1.15f,
                            Mathf.PerlinNoise(
                                u * 6f + h * 17.2f,
                                v * 6f + h * 31.7f
                            )
                        );

                        result[y, x] += hillHeight * dome * naturalUnevenness;
                    }
                }
            }

            Debug.Log($"Added {hillCount} flatland feature hills.");

            return result;
        }

        private float SampleLegendRampRobust(
            Texture2D texture,
            float u,
            float v,
            Color[] legendColors,
            float tolerance,
            float fallbackPosition)
        {
            float bestClass = fallbackPosition;
            float bestDistance = float.MaxValue;

            float pixelU = 1f / Mathf.Max(1, texture.width);
            float pixelV = 1f / Mathf.Max(1, texture.height);

            for (int oy = -2; oy <= 2; oy++)
            {
                for (int ox = -2; ox <= 2; ox++)
                {
                    float sampleU = Mathf.Clamp01(u + ox * pixelU);
                    float sampleV = Mathf.Clamp01(v + oy * pixelV);

                    Color c = texture.GetPixelBilinear(sampleU, sampleV);

                    if (!IsUsableLegendPixel(c))
                        continue;

                    float rampPosition = GetLegendRampPosition(c, legendColors, out float distance);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestClass = rampPosition;
                    }
                }
            }

            if (bestDistance > tolerance)
                return fallbackPosition;

            return bestClass;
        }

        private bool IsUsableLegendPixel(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);

            if (c.a < 0.1f)
                return false;

            if (s < 0.25f)
                return false;

            if (v < 0.12f)
                return false;

            return true;
        }

        private float GetLegendRampPosition(Color pixel, Color[] legendColors, out float bestDistance)
        {
            bestDistance = float.MaxValue;

            if (legendColors == null || legendColors.Length == 0)
                return 0f;

            if (legendColors.Length == 1)
            {
                bestDistance = ColorDistance(pixel, legendColors[0]);
                return 0f;
            }

            float bestPosition = 0f;

            for (int i = 0; i < legendColors.Length - 1; i++)
            {
                Color a = legendColors[i];
                Color b = legendColors[i + 1];

                float abr = b.r - a.r;
                float abg = b.g - a.g;
                float abb = b.b - a.b;

                float lengthSquared = abr * abr + abg * abg + abb * abb;

                float t = 0f;
                if (lengthSquared > 0.000001f)
                {
                    t = ((pixel.r - a.r) * abr + (pixel.g - a.g) * abg + (pixel.b - a.b) * abb) / lengthSquared;
                    t = Mathf.Clamp01(t);
                }

                float dr = pixel.r - (a.r + abr * t);
                float dg = pixel.g - (a.g + abg * t);
                float db = pixel.b - (a.b + abb * t);

                float distance = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPosition = i + t;
                }
            }

            return bestPosition;
        }

        private float LerpAcrossClasses(float rampPosition, float c0, float c1, float c2, float c3, float c4)
        {
            float clamped = Mathf.Clamp(rampPosition, 0f, 4f);
            int low = Mathf.Min(4, Mathf.FloorToInt(clamped));
            int high = Mathf.Min(4, low + 1);
            float t = clamped - low;

            return Mathf.Lerp(ClassValue(low, c0, c1, c2, c3, c4), ClassValue(high, c0, c1, c2, c3, c4), t);
        }

        private static float ClassValue(int index, float c0, float c1, float c2, float c3, float c4)
        {
            switch (index)
            {
                case 1: return c1;
                case 2: return c2;
                case 3: return c3;
                case 4: return c4;
                default: return c0;
            }
        }

        private float GetElevationMeters(float elevationRamp)
        {
            return LerpAcrossClasses(
                elevationRamp, lowlandElevationMeters, 350f, 750f, 1250f, maxLegendElevationMeters);
        }

        private float GetSlopeStrength01(float slopeRamp)
        {
            return LerpAcrossClasses(slopeRamp, 0.04f, 0.13f, 0.24f, 0.40f, 0.65f);
        }

        private float GetSlopeReliefMeters(float slopeRamp)
        {
            return LerpAcrossClasses(slopeRamp, 4f, 10f, 22f, 42f, 70f);
        }

        private void WarnIfSourceMapsDisagree()
        {
            float mapAspect = SpriteAspect(davaoMapSprite);
            if (mapAspect <= 0f)
                return;

            WarnIfAspectDiffers(elevationMapSprite, mapAspect, "elevation map");
            WarnIfAspectDiffers(slopeMapSprite, mapAspect, "slope map");
        }

        private void WarnIfAspectDiffers(Sprite sprite, float mapAspect, string label)
        {
            float aspect = SpriteAspect(sprite);
            if (aspect <= 0f)
                return;

            if (Mathf.Abs(aspect - mapAspect) / mapAspect <= 0.01f)
                return;

            Debug.LogWarning(
                "TemporaryTerrainGenerator on " + name + ": the " + label + " (" + sprite.name +
                ") is a different shape from the satellite map, so the two do not cover the same " +
                "ground and the terrain will be built from the wrong place. Export all three " +
                "on the same canvas size.", this);
        }

        private static float SpriteAspect(Sprite sprite)
        {
            if (sprite == null || sprite.rect.width < 0.5f)
                return 0f;

            return sprite.rect.height / sprite.rect.width;
        }

        public static Terrain ResolveActiveTerrain()
        {
            TemporaryTerrainGenerator[] generators =
                Object.FindObjectsByType<TemporaryTerrainGenerator>(FindObjectsSortMode.None);

            foreach (TemporaryTerrainGenerator generator in generators)
            {
                if (generator != null && generator.targetTerrain != null)
                    return generator.targetTerrain;
            }

            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain;

            return Object.FindFirstObjectByType<Terrain>();
        }

        private Terrain GetOrCreateTerrain()
        {
            if (targetTerrain != null)
            {
                ApplyTerrainMaterial(targetTerrain);
                return targetTerrain;
            }

            Terrain existing = Object.FindFirstObjectByType<Terrain>();
            if (existing != null)
            {
                targetTerrain = existing;
                ApplyTerrainMaterial(targetTerrain);
                return targetTerrain;
            }

            TerrainData td = new TerrainData();
            td.heightmapResolution = heightmapResolution;
            td.size = terrainSize;

            GameObject terrainGO = Terrain.CreateTerrainGameObject(td);
            terrainGO.name = "GeneratedTerrain";

            targetTerrain = terrainGO.GetComponent<Terrain>();
            ApplyTerrainMaterial(targetTerrain);
            return targetTerrain;
        }

        private void ApplyTerrainMaterial(Terrain terrain)
        {
            if (terrain == null)
                return;

            if (terrainMaterial != null)
            {
                terrain.materialTemplate = terrainMaterial;
                return;
            }

            if (terrain.materialTemplate != null)
                return;

            Debug.LogWarning(
                "TemporaryTerrainGenerator: no Terrain Material assigned. The terrain " +
                "will render magenta in a device build because the URP terrain shader " +
                "gets stripped. Create a material using 'Universal Render Pipeline/" +
                "Terrain/Lit' and assign it to the Terrain Material field.");
        }

        private Texture2D CropTexture(Texture2D source, Rect normalizedRect)
        {
            int x = Mathf.RoundToInt(normalizedRect.x * source.width);
            int y = Mathf.RoundToInt(normalizedRect.y * source.height);
            int w = Mathf.RoundToInt(normalizedRect.width * source.width);
            int h = Mathf.RoundToInt(normalizedRect.height * source.height);

            x = Mathf.Clamp(x, 0, source.width - 1);
            y = Mathf.Clamp(y, 0, source.height - 1);
            w = Mathf.Clamp(w, 1, source.width - x);
            h = Mathf.Clamp(h, 1, source.height - y);

            Color[] pixels = source.GetPixels(x, y, w, h);

            Texture2D cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cropped.SetPixels(pixels);
            cropped.Apply();

            return cropped;
        }

        public static void ClearSmoothnessAlpha(Texture2D texture)
        {
            if (texture == null || !texture.isReadable)
                return;

            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i].a = 0f;

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private void ApplyMapTexture(TerrainData td, Texture2D croppedMap)
        {
            ClearSmoothnessAlpha(croppedMap);

            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = croppedMap;
            layer.tileSize = new Vector2(td.size.x, td.size.z);
            layer.metallic = 0f;
            layer.smoothness = 0f;

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

        private void PositionCamera(Terrain terrain)
        {
            Camera cam = Camera.main;
            if (cam == null)
                cam = Object.FindFirstObjectByType<Camera>();

            if (cam == null)
                return;

            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);

            cam.transform.position = center + new Vector3(0f, size.y * 1.6f, -size.z * 0.9f);
            cam.transform.LookAt(center + new Vector3(0f, size.y * 0.25f, 0f));
        }

        private float[,] Blur(float[,] source)
        {
            int h = source.GetLength(0);
            int w = source.GetLength(1);

            float[,] result = new float[h, w];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    float weightSum = 0f;

                    for (int oy = -2; oy <= 2; oy++)
                    {
                        for (int ox = -2; ox <= 2; ox++)
                        {
                            int nx = Mathf.Clamp(x + ox, 0, w - 1);
                            int ny = Mathf.Clamp(y + oy, 0, h - 1);

                            float dist = Mathf.Sqrt(ox * ox + oy * oy);
                            float weight = 1f / (1f + dist);

                            sum += source[ny, nx] * weight;
                            weightSum += weight;
                        }
                    }

                    result[y, x] = sum / weightSum;
                }
            }

            return result;
        }

        private void FindMinMax(float[,] values, out float min, out float max)
        {
            int h = values.GetLength(0);
            int w = values.GetLength(1);

            min = float.MaxValue;
            max = float.MinValue;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float v = values[y, x];

                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
        }

        private float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;

            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static Color From255(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}