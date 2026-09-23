using System;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Import settings for every picture under Sprites/DavaoCityMap, applied
    /// automatically so none of them can arrive in a state the game cannot use.
    ///
    /// Three jobs, three rules:
    ///
    ///   DavaoCityMap/                    the TERRAIN SOURCES - satellite,
    ///                                    elevation and slope. The generator reads
    ///                                    all three pixel by pixel, so they have to
    ///                                    be readable. Left uncompressed at 2048,
    ///                                    which is what the existing maps already
    ///                                    use, because the elevation and slope
    ///                                    readings are colour matches against a
    ///                                    legend and compression moves colours.
    ///
    ///   DavaoCityMap/Districts/          the area selection pictures that get
    ///                                    DRAWN - district highlight and barangay
    ///                                    highlight. Ordinary compressed sprites;
    ///                                    nothing reads them.
    ///
    ///   DavaoCityMap/Districts/AreaSpots/  the picture that gets READ to decide
    ///                                    whether a plot is allowed. Readable,
    ///                                    uncompressed and point filtered: block
    ///                                    compression smears green into red along
    ///                                    every boundary, and a bilinear filter
    ///                                    invents colours that are neither.
    ///
    /// Dropping a file into any of them is all that is needed. Existing files can
    /// be brought in line with AgriDabao -> Reimport Davao Map Art.
    /// </summary>
    public class DistrictMapArtPostprocessor : AssetPostprocessor
    {
        public const string MapFolder = "Assets/Sprites/DavaoCityMap";
        public const string ArtFolder = MapFolder + "/Districts";
        public const string SpotFolder = ArtFolder + "/AreaSpots";

        /// <summary>
        /// Sampling resolution of the area spot maps. The full map is roughly 63 km
        /// across, so 512 works out near 120 m per pixel - far finer than a barangay
        /// boundary needs, and small enough that the six of them together cost a
        /// couple of megabytes rather than a couple of hundred. Raise it if a thin
        /// sliver of a barangay is being misjudged.
        /// </summary>
        private const int SpotMapResolution = 512;

        private const int MapResolution = 2048;

        private enum MapRole
        {
            None,
            TerrainSource,
            Highlight,
            AreaSpot
        }

        private void OnPreprocessTexture()
        {
            MapRole role = RoleOf(assetPath);
            if (role == MapRole.None)
                return;

            Apply((TextureImporter)assetImporter, role);
        }

        private static MapRole RoleOf(string assetPath)
        {
            string path = assetPath.Replace('\\', '/');

            if (StartsWith(path, SpotFolder + "/"))
                return MapRole.AreaSpot;

            if (StartsWith(path, ArtFolder + "/"))
                return MapRole.Highlight;

            // Only files sitting directly in the map folder, so an unrelated
            // subfolder added later is not silently forced uncompressed.
            if (StartsWith(path, MapFolder + "/") &&
                path.IndexOf('/', MapFolder.Length + 1) < 0)
            {
                return MapRole.TerrainSource;
            }

            return MapRole.None;
        }

        private static bool StartsWith(string path, string prefix)
        {
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void Apply(TextureImporter importer, MapRole role)
        {
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;

            switch (role)
            {
                case MapRole.AreaSpot:
                    importer.isReadable = true;
                    importer.filterMode = FilterMode.Point;
                    importer.maxTextureSize = SpotMapResolution;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    break;

                case MapRole.TerrainSource:
                    importer.isReadable = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = MapResolution;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    break;

                default:
                    importer.isReadable = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = MapResolution;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    break;
            }

            // Per-platform overrides win over everything set above, and the mobile
            // ones compress by default, so they are removed rather than fought.
            importer.ClearPlatformTextureSettings("Android");
            importer.ClearPlatformTextureSettings("iPhone");
            importer.ClearPlatformTextureSettings("Standalone");
            importer.ClearPlatformTextureSettings("WebGL");
        }

        [MenuItem("AgriDabao/Reimport Davao Map Art")]
        private static void ReimportAll()
        {
            if (!AssetDatabase.IsValidFolder(MapFolder))
            {
                Debug.LogWarning("There is no " + MapFolder + " folder.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { MapFolder });
            int touched = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (RoleOf(path) == MapRole.None)
                    continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                touched++;
            }

            Debug.Log("Reimported " + touched + " Davao map pictures under " + MapFolder + ".");
        }
    }
}
