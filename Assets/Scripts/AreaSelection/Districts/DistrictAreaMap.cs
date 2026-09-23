using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The three pictures one district needs on the area selection screen.
    ///
    /// All three are drawn on the same canvas as the base satellite map and must
    /// be exported at the same pixel size, because the game lines them up by
    /// stretching each one over the same rectangle. If one of them is a different
    /// shape, its barangays will not sit where the satellite says they are.
    /// </summary>
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
        /// <summary>Every sampled point of the box is on green land.</summary>
        Valid,

        /// <summary>Some part of the box is off the district altogether.</summary>
        OutsideDistrict,

        /// <summary>The box is inside the district but touches red land.</summary>
        NonAgricultural,

        /// <summary>No agricultural area spot picture has been assigned yet.</summary>
        SpotMapMissing,

        /// <summary>The picture is assigned but the game is not allowed to read it.</summary>
        SpotMapUnreadable
    }

    /// <summary>
    /// Reads a district's "barangay agricultural area spot" picture.
    ///
    /// The picture is a flat map: green where the barangay is an agricultural
    /// area, red where it is not, and white everywhere outside the district. That
    /// is the whole rule - the game asks this picture what colour is under the
    /// player's box and answers accordingly, the same way the old screen asked the
    /// district border map what colour a point was.
    ///
    /// Colours are classified by which channel leads rather than by matching an
    /// exact swatch, so a slightly different green, a resized import or a little
    /// JPEG-ish softening around the shapes all still read correctly. Pixels that
    /// lead with neither - the blended ring one pixel wide where green meets red -
    /// are reported as undecided and simply ignored.
    ///
    /// Each picture is decoded once and kept, because the player steps back and
    /// forth between districts and re-reading a few million pixels on every
    /// Generate would stutter.
    /// </summary>
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

        /// <summary>
        /// Decodes a picture ahead of time. Reading one costs a few hundred
        /// thousand pixels, which is nothing spread over the frames after the
        /// screen opens but is a visible hitch if it lands on the frame the player
        /// presses >> on.
        /// </summary>
        public static void Prewarm(Sprite spot)
        {
            Get(spot);
        }

        /// <summary>
        /// Drops every decoded picture. Called when the area selection screen goes
        /// away, so the farm does not carry a few megabytes of map around with it.
        /// </summary>
        public static void ClearCache()
        {
            cache.Clear();
            unreadable.Clear();
        }

        /// <summary>
        /// The district's own extent in the picture, as a normalized rect and
        /// centroid (u = 0 left to 1 right, v = 0 bottom to 1 top). Green and red
        /// together make up the district, so both count toward the footprint.
        /// Used to aim the camera at the district and to park the selection box.
        /// </summary>
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

        /// <summary>
        /// Checks the box the player dragged against the district's spot picture.
        /// Every sampled point has to be on green land; one point off the district
        /// or on red land is enough to refuse, which is what keeps a plot from
        /// straddling a boundary it should not cross.
        /// </summary>
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

            // Leaving the district is the more basic mistake, so it is reported
            // first even when the box also clips a red barangay. A box that landed
            // entirely on blended edge pixels counts as off the district too -
            // there is no evidence it is on farmland.
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
            // textureRect rather than the whole texture, so a sprite packed into an
            // atlas or trimmed on import still reads its own region.
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

            // One district on a whole-city canvas is a small part of it. Much more
            // than this and the wrong picture is almost certainly in the slot - a
            // satellite photo reads as green almost everywhere, and would quietly
            // approve every area the player picked.
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

            // Paper white is the land that does not belong to this district.
            if (min >= 200 && max - min <= 30)
                return Cell.Outside;

            // Dark ink is undecided rather than outside, which matters if the spot
            // map is ever exported with barangay outlines on it: a box sitting on
            // the line between two green barangays would otherwise be refused as
            // off the district. Undecided points are ignored, so the green either
            // side of the line still decides it. A box that finds nothing but ink
            // is refused anyway, so a black background behaves like a white one.
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
