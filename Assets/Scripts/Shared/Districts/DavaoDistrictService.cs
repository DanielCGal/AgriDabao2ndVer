using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    [Serializable]
    public class DistrictColorEntry
    {
        public string districtName;
        public Color color;
    }

    public enum AreaValidationStatus
    {
        Valid,
        OutsideCity,
        PartiallyOutsideCity,
        NonProductiveArea,
        ServiceNotReady
    }

    [Serializable]
    public class AreaValidationResult
    {
        public AreaValidationStatus status;
        public string message;
        public string primaryDistrictName;
    }

    public class DavaoDistrictService : MonoBehaviour
    {
        [Header("District Border Sprite")]
        public Sprite districtBorderSprite;

        [Header("Outside City")]
        public Color outsideCityColor = Color.white;

        [Header("Color Matching")]
        [Range(0.005f, 0.25f)] public float colorTolerance = 0.10f;

        [Header("District Colors")]
        public List<DistrictColorEntry> districtColors = new List<DistrictColorEntry>();

        private Texture2D districtTexture;

        private static readonly HashSet<string> ProductiveDistricts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CALINAN",
            "TORIL",
            "BAGUIO",
            "PAQUIBATO",
            "MARILOG",
            "BUHANGIN",
            "TUGBOK",
            "CALINAN DISTRICT",
            "TORIL DISTRICT",
            "BAGUIO DISTRICT",
            "PAQUIBATO DISTRICT",
            "MARILOG DISTRICT",
            "BUHANGIN DISTRICT",
            "TUGBOK DISTRICT"
        };

        public static readonly string[] ProductiveDistrictOrder =
        {
            "Calinan",
            "Toril",
            "Baguio",
            "Paquibato",
            "Marilog",
            "Buhangin",
            "Tugbok"
        };

        private class DistrictBoundsAccumulator
        {
            public float minU = 1f;
            public float minV = 1f;
            public float maxU = 0f;
            public float maxV = 0f;
            public double sumU;
            public double sumV;
            public long count;
        }

        private Dictionary<string, DistrictBoundsAccumulator> districtBoundsByName;

        private struct DistrictFrame
        {
            public Rect bounds;
            public Vector2 center;
        }

        private static readonly Dictionary<string, DistrictFrame> FallbackFrames =
            new Dictionary<string, DistrictFrame>(StringComparer.OrdinalIgnoreCase)
            {
                { "Calinan",   Frame(0.0082f, 0.4117f, 0.4280f, 0.4542f, 0.2315f, 0.6623f) },
                { "Toril",     Frame(0.0002f, 0.2253f, 0.3900f, 0.2372f, 0.1823f, 0.3426f) },
                { "Baguio",    Frame(0.0002f, 0.4625f, 0.2340f, 0.2678f, 0.0966f, 0.6042f) },
                { "Paquibato", Frame(0.0262f, 0.6388f, 0.4500f, 0.3610f, 0.3079f, 0.8875f) },
                { "Marilog",   Frame(0.0002f, 0.6388f, 0.1480f, 0.3610f, 0.0516f, 0.8193f) },
                { "Buhangin",  Frame(0.4162f, 0.4676f, 0.2780f, 0.2356f, 0.5699f, 0.5654f) }
            };

        private static DistrictFrame Frame(float minU, float minV, float width, float height, float cx, float cy)
        {
            return new DistrictFrame
            {
                bounds = new Rect(minU, minV, width, height),
                center = new Vector2(cx, cy)
            };
        }

        private void Reset()
        {
            BuildDefaultDistrictColors();
            RefreshTextureReference();
        }

        private void Awake()
        {
            if (districtColors == null || districtColors.Count == 0)
                BuildDefaultDistrictColors();

            RefreshTextureReference();
        }

        private void OnValidate()
        {
            if (districtColors == null || districtColors.Count == 0)
                BuildDefaultDistrictColors();

            RefreshTextureReference();
        }

        public void SetDistrictBorderSprite(Sprite sprite)
        {
            districtBorderSprite = sprite;
            RefreshTextureReference();
        }

        private void RefreshTextureReference()
        {
            districtTexture = districtBorderSprite != null ? districtBorderSprite.texture : null;
        }

        private void BuildDefaultDistrictColors()
        {
            districtColors = new List<DistrictColorEntry>
            {
                new DistrictColorEntry { districtName = "Agdao",     color = From255(215,  25,  28) },
                new DistrictColorEntry { districtName = "Baguio",    color = From255(231,  84,  55) },
                new DistrictColorEntry { districtName = "Buhangin",  color = From255(246, 144,  83) },
                new DistrictColorEntry { districtName = "Bunawan",   color = From255(254, 190, 116) },
                new DistrictColorEntry { districtName = "Calinan",   color = From255(255, 223, 154) },
                new DistrictColorEntry { districtName = "Marilog",   color = From255(255, 255, 191) },
                new DistrictColorEntry { districtName = "Paquibato", color = From255(222, 242, 180) },
                new DistrictColorEntry { districtName = "Poblacion", color = From255(188, 228, 170) },
                new DistrictColorEntry { districtName = "Talomo",    color = From255(145, 203, 169) },
                new DistrictColorEntry { districtName = "Toril",     color = From255( 94, 167, 177) },
                new DistrictColorEntry { districtName = "Tugbok",    color = From255( 43, 131, 186) }
            };
        }

        private static Color From255(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        public bool IsReady()
        {
            if (districtTexture == null && districtBorderSprite != null)
                RefreshTextureReference();

            return districtBorderSprite != null &&
                   districtTexture != null &&
                   districtTexture.isReadable &&
                   districtColors != null &&
                   districtColors.Count > 0;

            Debug.Log($"District ready? sprite={districtBorderSprite != null}, tex={districtTexture != null}, readable={(districtTexture != null && districtTexture.isReadable)}, colors={(districtColors != null ? districtColors.Count : 0)}");
        }

        public bool TryGetDistrictAtNormalized(float u, float v, out string districtName, out bool isInsideCity, out bool isProductive)
        {
            districtName = "";
            isInsideCity = false;
            isProductive = false;

            if (!IsReady())
                return false;

            Color pixel = districtTexture.GetPixelBilinear(u, v);

            if (IsSameColor(pixel, outsideCityColor))
            {
                isInsideCity = false;
                return true;
            }

            isInsideCity = true;

            float bestDistance = float.MaxValue;
            DistrictColorEntry bestEntry = null;

            foreach (DistrictColorEntry entry in districtColors)
            {
                float distance = ColorDistance(pixel, entry.color);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestEntry = entry;
                }
            }

            if (bestEntry == null || bestDistance > colorTolerance)
            {
                districtName = "Unknown District";
                isProductive = false;
                return true;
            }

            districtName = bestEntry.districtName;
            isProductive = IsProductiveDistrict(bestEntry.districtName);
            return true;
        }

        public AreaValidationResult ValidateSelection(Rect normalizedRect, int sampleResolution = 7)
        {
            AreaValidationResult result = new AreaValidationResult();

            if (!IsReady())
            {
                result.status = AreaValidationStatus.ServiceNotReady;
                result.message = "District validation is not ready. Check the sprite assignment and Read/Write setting.";
                return result;
            }

            bool foundAnyInsideCity = false;
            bool foundAnyOutsideCity = false;
            bool foundAnyNonProductive = false;
            string firstDistrict = "";

            for (int y = 0; y < sampleResolution; y++)
            {
                for (int x = 0; x < sampleResolution; x++)
                {
                    float u = Mathf.Lerp(normalizedRect.xMin, normalizedRect.xMax, (x + 0.5f) / sampleResolution);
                    float v = Mathf.Lerp(normalizedRect.yMin, normalizedRect.yMax, (y + 0.5f) / sampleResolution);

                    if (!TryGetDistrictAtNormalized(u, v, out string district, out bool insideCity, out bool productive))
                    {
                        result.status = AreaValidationStatus.ServiceNotReady;
                        result.message = "Failed to read district border map.";
                        return result;
                    }

                    if (!insideCity)
                    {
                        foundAnyOutsideCity = true;
                        continue;
                    }

                    foundAnyInsideCity = true;

                    if (string.IsNullOrEmpty(firstDistrict) && !string.IsNullOrEmpty(district))
                        firstDistrict = district;

                    if (!productive)
                        foundAnyNonProductive = true;
                }
            }

            if (!foundAnyInsideCity)
            {
                result.status = AreaValidationStatus.OutsideCity;
                result.message = "This area is outside of the Davao City Area, please select an area IN Davao City";
                return result;
            }

            if (foundAnyOutsideCity)
            {
                result.status = AreaValidationStatus.PartiallyOutsideCity;
                result.message = "This area is outside of the Davao City Area, please select an area IN Davao City";
                return result;
            }

            if (foundAnyNonProductive)
            {
                result.status = AreaValidationStatus.NonProductiveArea;
                result.message = "This part of Davao City is not a PRODUCTION AREA, please choose another area";
                return result;
            }

            result.status = AreaValidationStatus.Valid;
            result.message = "Valid production area";
            result.primaryDistrictName = firstDistrict;
            return result;
        }

        public AreaValidationResult ValidateSelectionForDistrict(
            Rect normalizedRect, string requiredDistrict, int sampleResolution = 7)
        {
            AreaValidationResult result = new AreaValidationResult();

            if (!IsReady())
            {
                result.status = AreaValidationStatus.ServiceNotReady;
                result.message = "District validation is not ready. Check the sprite assignment and Read/Write setting.";
                return result;
            }

            string required = requiredDistrict != null ? requiredDistrict.Trim() : "";
            string outsideMessage =
                "You selected outside the " + required +
                " district. Please select an area inside the district.";

            for (int y = 0; y < sampleResolution; y++)
            {
                for (int x = 0; x < sampleResolution; x++)
                {
                    float u = Mathf.Lerp(normalizedRect.xMin, normalizedRect.xMax, (x + 0.5f) / sampleResolution);
                    float v = Mathf.Lerp(normalizedRect.yMin, normalizedRect.yMax, (y + 0.5f) / sampleResolution);

                    if (!TryGetDistrictAtNormalized(u, v, out string district, out bool insideCity, out _))
                    {
                        result.status = AreaValidationStatus.ServiceNotReady;
                        result.message = "Failed to read district border map.";
                        return result;
                    }

                    if (!insideCity ||
                        !string.Equals(district, required, StringComparison.OrdinalIgnoreCase))
                    {
                        result.status = AreaValidationStatus.OutsideCity;
                        result.message = outsideMessage;
                        result.primaryDistrictName = required;
                        return result;
                    }
                }
            }

            result.status = AreaValidationStatus.Valid;
            result.message = "Valid production area";
            result.primaryDistrictName = required;
            return result;
        }

        public bool TryGetDistrictNormalizedBounds(string districtName, out Rect bounds, out Vector2 center)
        {
            bounds = new Rect(0f, 0f, 1f, 1f);
            center = new Vector2(0.5f, 0.5f);

            if (string.IsNullOrWhiteSpace(districtName))
                return false;

            EnsureDistrictBoundsComputed();

            if (districtBoundsByName != null &&
                districtBoundsByName.TryGetValue(districtName.Trim(), out DistrictBoundsAccumulator b) &&
                b.count > 0)
            {
                bounds = new Rect(
                    b.minU,
                    b.minV,
                    Mathf.Max(0.0001f, b.maxU - b.minU),
                    Mathf.Max(0.0001f, b.maxV - b.minV));
                center = new Vector2((float)(b.sumU / b.count), (float)(b.sumV / b.count));
                return true;
            }

            if (FallbackFrames.TryGetValue(districtName.Trim(), out DistrictFrame fallback))
            {
                bounds = fallback.bounds;
                center = fallback.center;
                return true;
            }

            return false;

        }

        private void EnsureDistrictBoundsComputed()
        {
            if (districtBoundsByName != null)
                return;

            if (!IsReady())
                return;

            Color32[] pixels;
            try
            {
                pixels = districtTexture.GetPixels32();
            }
            catch (UnityException)
            {
                return;
            }

            districtBoundsByName = new Dictionary<string, DistrictBoundsAccumulator>(StringComparer.OrdinalIgnoreCase);

            int width = districtTexture.width;
            int height = districtTexture.height;

            int stride = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(width, height) / 512f));

            for (int y = 0; y < height; y += stride)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x += stride)
                {
                    Color32 c32 = pixels[rowOffset + x];
                    Color color = new Color(c32.r / 255f, c32.g / 255f, c32.b / 255f, 1f);

                    if (IsSameColor(color, outsideCityColor))
                        continue;

                    if (!TryClassifyColor(color, out string name))
                        continue;

                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;

                    if (!districtBoundsByName.TryGetValue(name, out DistrictBoundsAccumulator acc))
                    {
                        acc = new DistrictBoundsAccumulator();
                        districtBoundsByName[name] = acc;
                    }

                    if (u < acc.minU) acc.minU = u;
                    if (u > acc.maxU) acc.maxU = u;
                    if (v < acc.minV) acc.minV = v;
                    if (v > acc.maxV) acc.maxV = v;
                    acc.sumU += u;
                    acc.sumV += v;
                    acc.count++;
                }
            }
        }

        private bool TryClassifyColor(Color pixel, out string districtName)
        {
            districtName = null;

            float bestDistance = float.MaxValue;
            DistrictColorEntry bestEntry = null;

            foreach (DistrictColorEntry entry in districtColors)
            {
                float distance = ColorDistance(pixel, entry.color);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestEntry = entry;
                }
            }

            if (bestEntry == null || bestDistance > colorTolerance)
                return false;

            districtName = bestEntry.districtName;
            return true;
        }

        private bool IsProductiveDistrict(string districtName)
        {
            if (string.IsNullOrWhiteSpace(districtName))
                return false;

            return ProductiveDistricts.Contains(districtName.Trim());
        }

        private bool IsSameColor(Color a, Color b)
        {
            return ColorDistance(a, b) <= colorTolerance;
        }

        private float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
