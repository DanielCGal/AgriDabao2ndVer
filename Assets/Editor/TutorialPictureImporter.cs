using System;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Imports the area selection tutorial's screenshots as sprites.
    ///
    /// Each tutorial page takes a Sprite, but this project was set up in 3D mode,
    /// so a freshly dropped PNG arrives as a plain texture and the page's Picture
    /// slot refuses it until its Texture Type is changed by hand. Anything placed
    /// in this folder skips that step.
    ///
    /// The settings only apply as a picture is imported, so drop new screenshots
    /// straight into the folder. A picture moved in from somewhere else keeps its
    /// old settings until AgriDabao -> Reimport Tutorial Pictures is run.
    /// </summary>
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

            // Screenshots are only ever shown, never read, so ordinary compression
            // is fine; 2048 keeps a full-HD capture sharp at the size it is drawn.
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
