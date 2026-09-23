using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The pictures the farm map draws instead of dots: a crop's fruit, and a
    /// placed mitigation item's kit.
    ///
    /// None of the art is assigned twice. The inventory already holds every one of
    /// these for the hotbar and backpack, so the map asks it by item type, and a
    /// sprite swapped there changes on the map as well.
    ///
    /// Each picture is shrunk once before the map uses it. The source files are
    /// 1254 pixels square with no mipmaps and a map icon is about thirty, so drawn
    /// straight from the original, every screen pixel lands on a different few
    /// texels each time the small map scrolls or turns, and the icons shimmer.
    /// Halving the picture over and over averages that detail away first. Where
    /// that cannot be done, the original sprite is drawn as it is.
    /// </summary>
    public static class FarmMapIcons
    {
        /// <summary>
        /// Longest side of the shrunk copy: about twice the largest an icon is
        /// drawn, zoomed all the way in, so it stays sharp on a dense phone screen.
        /// </summary>
        private const int IconPixels = 128;

        private static readonly Dictionary<InventoryItemType, Sprite> Cache =
            new Dictionary<InventoryItemType, Sprite>();

        private static InventoryUIBuilder inventory;
        private static float nextInventorySearchTime;

        /// <summary>The crop's fruit, or null when there is no picture for it.</summary>
        public static Sprite ForCrop(FarmCropType crop)
        {
            return ForItem(CropItem(crop));
        }

        /// <summary>The kit a placed climate structure was built from.</summary>
        public static Sprite ForClimateObject(ClimateWorldMitigationType type)
        {
            return ForItem(ClimateItem(type));
        }

        /// <summary>A pheromone trap or a termite bait station.</summary>
        public static Sprite ForAreaTrap(PestDiseaseMitigation mitigation)
        {
            return ForItem(TrapItem(mitigation));
        }

        public static Sprite ForAphidTrap()
        {
            return ForItem(InventoryItemType.AphidTrap);
        }

        private static Sprite ForItem(InventoryItemType item)
        {
            if (item == InventoryItemType.None)
                return null;

            // A cached sprite reads as null once Unity has destroyed it - leaving
            // Play Mode does that to anything made at runtime - and is made again.
            if (Cache.TryGetValue(item, out Sprite cached) && cached != null)
                return cached;

            // Throttled, because the map asks once per crop on every rescan and a
            // scene without an inventory would otherwise search for one hundreds
            // of times a second.
            if (inventory == null && Time.unscaledTime >= nextInventorySearchTime)
            {
                nextInventorySearchTime = Time.unscaledTime + 1f;
                inventory = Object.FindFirstObjectByType<InventoryUIBuilder>();
            }

            Sprite source = inventory != null ? inventory.GetSpriteFor(item) : null;
            if (source == null)
                return null;

            Sprite icon = Shrink(source);
            if (icon == null)
                icon = source;

            Cache[item] = icon;
            return icon;
        }

        private static Sprite Shrink(Sprite source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture current = null;

            try
            {
                Texture texture = source.texture;
                Rect area = source.textureRect;
                float longest = Mathf.Max(area.width, area.height);

                if (texture == null || longest <= IconPixels)
                    return null;

                int targetWidth = Mathf.Max(1, Mathf.RoundToInt(area.width * IconPixels / longest));
                int targetHeight = Mathf.Max(1, Mathf.RoundToInt(area.height * IconPixels / longest));

                // The first pass reads only this sprite's part of its texture.
                int width = Mathf.Max(targetWidth, Mathf.CeilToInt(area.width * 0.5f));
                int height = Mathf.Max(targetHeight, Mathf.CeilToInt(area.height * 0.5f));
                current = Temporary(width, height);
                Graphics.Blit(texture, current,
                    new Vector2(area.width / texture.width, area.height / texture.height),
                    new Vector2(area.x / texture.width, area.y / texture.height));

                // Each halving samples midway between four texels, so every pixel
                // is an even average of them rather than a pick of one.
                while (width > targetWidth || height > targetHeight)
                {
                    width = Mathf.Max(targetWidth, Mathf.CeilToInt(width * 0.5f));
                    height = Mathf.Max(targetHeight, Mathf.CeilToInt(height * 0.5f));

                    RenderTexture next = Temporary(width, height);
                    Graphics.Blit(current, next);
                    RenderTexture.ReleaseTemporary(current);
                    current = next;
                }

                RenderTexture.active = current;

                Texture2D shrunk = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, false)
                {
                    name = source.name + " (map icon)",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                shrunk.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0, false);
                shrunk.Apply(false, true);

                return Sprite.Create(shrunk, new Rect(0f, 0f, targetWidth, targetHeight),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[FarmMap] Drawing " + source.name +
                                 " at full size, because it could not be shrunk: " + exception.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (current != null)
                    RenderTexture.ReleaseTemporary(current);
            }
        }

        private static RenderTexture Temporary(int width, int height)
        {
            // sRGB both ways, so a linear-colour project averages real brightness
            // and a gamma one is left alone.
            RenderTexture texture = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static InventoryItemType CropItem(FarmCropType crop)
        {
            switch (crop)
            {
                case FarmCropType.Coconut: return InventoryItemType.Coconut;
                case FarmCropType.Banana: return InventoryItemType.Banana;
                case FarmCropType.Durian: return InventoryItemType.Durian;
                case FarmCropType.Pomelo: return InventoryItemType.Pomelo;
                case FarmCropType.Cacao: return InventoryItemType.Cacao;
                case FarmCropType.Pineapple: return InventoryItemType.Pineapple;
                case FarmCropType.Mangosteen: return InventoryItemType.Mangosteen;
                case FarmCropType.Mango: return InventoryItemType.Mango;
                case FarmCropType.Corn: return InventoryItemType.Corn;
                case FarmCropType.Eggplant: return InventoryItemType.Eggplant;
                case FarmCropType.Squash: return InventoryItemType.Squash;
                case FarmCropType.Strawberry: return InventoryItemType.Strawberry;
                case FarmCropType.Tomato: return InventoryItemType.Tomato;
                default: return InventoryItemType.None;
            }
        }

        private static InventoryItemType ClimateItem(ClimateWorldMitigationType type)
        {
            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem: return InventoryItemType.IrrigationSystemKit;
                case ClimateWorldMitigationType.WaterStorageTank: return InventoryItemType.WaterStorageTankKit;
                case ClimateWorldMitigationType.ShadeNet: return InventoryItemType.ShadeNetKit;
                case ClimateWorldMitigationType.Windbreak: return InventoryItemType.WindbreakKit;
                case ClimateWorldMitigationType.Greenhouse: return InventoryItemType.GreenhouseKit;
                case ClimateWorldMitigationType.DrainageCanal: return InventoryItemType.DrainageCanalKit;
                default: return InventoryItemType.None;
            }
        }

        private static InventoryItemType TrapItem(PestDiseaseMitigation mitigation)
        {
            switch (mitigation)
            {
                case PestDiseaseMitigation.PheromoneTrap: return InventoryItemType.PheromoneTrap;
                case PestDiseaseMitigation.TermiteBait: return InventoryItemType.TermiteBaitStation;
                case PestDiseaseMitigation.AphidTrap: return InventoryItemType.AphidTrap;
                default: return InventoryItemType.None;
            }
        }
    }
}
