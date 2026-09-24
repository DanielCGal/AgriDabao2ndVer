using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    [Serializable]
    public class DistrictMapArt
    {
        [Tooltip("Must match the district exactly - Calinan, Toril, Baguio, " +
                 "Paquibato, Marilog, Buhangin, Tugbok. This is the name shown on the sign, " +
                 "looked up for the description board, and saved with the farm.")]
        public string districtName;

        [Tooltip("Whole-city picture with only this district tinted. Shown while the " +
                 "player is still stepping through districts with << and >>.")]
        public Sprite districtHighlight;

        [Tooltip("Whole-city picture with this district's barangay borders drawn in, " +
                 "filled green where farming is allowed and red where it is not. " +
                 "Shown once the player presses Next and locks the district in.")]
        public Sprite barangayHighlight;

        [Tooltip("The same green and red barangays as flat shapes on a white " +
                 "background, with no satellite behind them. Never drawn on screen - " +
                 "this is the picture the game READS to decide whether the box the " +
                 "player dragged is on agricultural land. Needs Read/Write Enabled.")]
        public Sprite agriculturalAreaSpot;

        public bool IsComplete =>
            districtHighlight != null && barangayHighlight != null && agriculturalAreaSpot != null;
    }

    public enum AreaSelectionVerdict
    {
        Valid,

        OutsideDistrict,

        NonAgricultural,

        SpotMapMissing,

        SpotMapUnreadable
    }

    public static class DistrictAreaSpot
    {
        private enum Cell : byte
        {
            Outside = 0,
            Agricultural = 1,
            NonAgricultural = 2,
            Undecided = 3
        }

        private class Decoded
        {
            public int width;
            public int height;
            public Cell[] cells;
            public bool hasFootprint;
            public Rect bounds;
            public Vector2 center;
        }

        private static readonly Dictionary<Sprite, Decoded> cache = new Dictionary<Sprite, Decoded>();
        private static readonly HashSet<Sprite> unreadable = new HashSet<Sprite>();

        public static void Prewarm(Sprite spot)
        {
            Get(spot);
        }

        public static void ClearCache()
        {
            cache.Clear();
            unreadable.Clear();
        }

        public static bool TryGetFootprint(Sprite spot, out Rect bounds, out Vector2 center)
        {
            bounds = new Rect(0f, 0f, 1f, 1f);
            center = new Vector2(0.5f, 0.5f);

            Decoded decoded = Get(spot);
            if (decoded == null || !decoded.hasFootprint)
                return false;

            bounds = decoded.bounds;
            center = decoded.center;
            return true;
        }

        public static AreaSelectionVerdict Validate(Sprite spot, Rect normalizedRect, int resolution)
        {
            if (spot == null)
                return AreaSelectionVerdict.SpotMapMissing;

            Decoded decoded = Get(spot);
            if (decoded == null)
                return AreaSelectionVerdict.SpotMapUnreadable;

            int steps = Mathf.Max(2, resolution);

            bool anyOutside = false;
            bool anyNonAgricultural = false;
            bool anyAgricultural = false;

            for (int y = 0; y < steps; y++)
            {
                float v = Mathf.Lerp(normalizedRect.yMin, normalizedRect.yMax, (y + 0.5f) / steps);

                for (int x = 0; x < steps; x++)
                {
                    float u = Mathf.Lerp(normalizedRect.xMin, normalizedRect.xMax, (x + 0.5f) / steps);

                    switch (Sample(decoded, u, v))
                    {
                        case Cell.Outside:
                            anyOutside = true;
                            break;
                        case Cell.NonAgricultural:
                            anyNonAgricultural = true;
                            break;
                        case Cell.Agricultural:
                            anyAgricultural = true;
                            break;
                    }
                }
            }

            if (anyOutside || (!anyAgricultural && !anyNonAgricultural))
                return AreaSelectionVerdict.OutsideDistrict;

            if (anyNonAgricultural)
                return AreaSelectionVerdict.NonAgricultural;

            return AreaSelectionVerdict.Valid;
        }

        private static Cell Sample(Decoded decoded, float u, float v)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt(u * decoded.width), 0, decoded.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(v * decoded.height), 0, decoded.height - 1);
            return decoded.cells[y * decoded.width + x];
        }

        private static Decoded Get(Sprite spot)
        {
            if (spot == null)
                return null;

            if (cache.TryGetValue(spot, out Decoded cached))
                return cached;

            if (unreadable.Contains(spot))
                return null;

            Texture2D texture = spot.texture;
            if (texture == null)
            {
                unreadable.Add(spot);
                return null;
            }

            Color32[] pixels;
            try
            {
                pixels = texture.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogError(
                    "Area selection: the agricultural area spot picture \"" + spot.name +
                    "\" cannot be read. Select it in the Project window and turn on " +
                    "Read/Write Enabled, or the game cannot tell green land from red.");
                unreadable.Add(spot);
                return null;
            }

            Decoded decoded = Decode(spot, texture, pixels);
            cache[spot] = decoded;
            return decoded;
        }

        private static Decoded Decode(Sprite spot, Texture2D texture, Color32[] pixels)
        {
            Rect region = spot.textureRect;

            int originX = Mathf.Clamp(Mathf.RoundToInt(region.x), 0, Mathf.Max(0, texture.width - 1));
            int originY = Mathf.Clamp(Mathf.RoundToInt(region.y), 0, Mathf.Max(0, texture.height - 1));
            int width = Mathf.Clamp(Mathf.RoundToInt(region.width), 1, texture.width - originX);
            int height = Mathf.Clamp(Mathf.RoundToInt(region.height), 1, texture.height - originY);

            Decoded decoded = new Decoded
            {
                width = width,
                height = height,
                cells = new Cell[width * height]
            };

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            double sumX = 0d;
            double sumY = 0d;
            long count = 0L;

            for (int y = 0; y < height; y++)
            {
                int sourceRow = (originY + y) * texture.width + originX;
                int targetRow = y * width;

                for (int x = 0; x < width; x++)
                {
                    Cell cell = Classify(pixels[sourceRow + x]);
                    decoded.cells[targetRow + x] = cell;

                    if (cell != Cell.Agricultural && cell != Cell.NonAgricultural)
                        continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    sumX += x;
                    sumY += y;
                    count++;
                }
            }

            if (count > width * (long)height * 6L / 10L)
            {
                Debug.LogWarning(
                    "Area selection: \"" + spot.name + "\" is coloured in over most of the " +
                    "picture. An agricultural area spot map should be one district on a " +
                    "white background - check that a satellite or highlight picture has " +
                    "not been put in that slot by mistake.");
            }

            if (count > 0L && maxX >= minX && maxY >= minY)
            {
                decoded.hasFootprint = true;
                decoded.bounds = new Rect(
                    minX / (float)width,
                    minY / (float)height,
                    Mathf.Max(1f, maxX - minX + 1f) / width,
                    Mathf.Max(1f, maxY - minY + 1f) / height);
                decoded.center = new Vector2(
                    (float)(sumX / count + 0.5d) / width,
                    (float)(sumY / count + 0.5d) / height);
            }
            else
            {
                Debug.LogWarning(
                    "Area selection: \"" + spot.name + "\" has no green or red areas on it. " +
                    "The camera will frame the whole map for that district instead.");
            }

            return decoded;
        }

        private static Cell Classify(Color32 colour)
        {
            if (colour.a < 32)
                return Cell.Outside;

            int r = colour.r;
            int g = colour.g;
            int b = colour.b;

            int max = Mathf.Max(r, Mathf.Max(g, b));
            int min = Mathf.Min(r, Mathf.Min(g, b));

            if (min >= 200 && max - min <= 30)
                return Cell.Outside;

            if (max <= 60)
                return Cell.Undecided;

            const int Lead = 24;

            if (g >= r + Lead && g >= b + Lead)
                return Cell.Agricultural;

            if (r >= g + Lead && r >= b + Lead)
                return Cell.NonAgricultural;

            return Cell.Undecided;
        }
    }
}
