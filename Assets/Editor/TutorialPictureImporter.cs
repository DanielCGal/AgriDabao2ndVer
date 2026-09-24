using System;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public class TutorialPictureImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Sprites/UI/AreaSelectionTutorial";

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;

            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }

        [MenuItem("AgriDabao/Reimport Tutorial Pictures")]
        private static void ReimportAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning("There is no " + Folder + " folder.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });

            for (int i = 0; i < guids.Length; i++)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guids[i]), ImportAssetOptions.ForceUpdate);

            Debug.Log("Reimported " + guids.Length + " tutorial pictures under " + Folder + ".");
        }
    }
}
