using System;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public class DistrictMapArtPostprocessor : AssetPostprocessor
    {
        public const string MapFolder = "Assets/Sprites/DavaoCityMap";
        public const string ArtFolder = MapFolder + "/Districts";
        public const string SpotFolder = ArtFolder + "/AreaSpots";

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
