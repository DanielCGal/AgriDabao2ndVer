#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public static class ClimateMaintenancePrefabGenerator
    {
        private const string Folder = "Assets/Resources/ClimateMaintenance/Prefabs";

        [MenuItem("Tools/AgriDabao/Climate Maintenance/Generate Placeholder Prefabs")]
        public static void Generate()
        {
            ConfigureIcons();
            Directory.CreateDirectory(Folder);
            SaveWorld("IrrigationSystem", BuildIrrigation());
            SaveWorld("WaterStorageTank", BuildTank());
            SaveWorld("ShadeNet", BuildShadeNet());
            SaveWorld("Windbreak", BuildWindbreak());
            SaveWorld("Greenhouse", BuildGreenhouse());
            SaveWorld("DrainageCanal", BuildCanal());
            SaveWorld("Crop_Mulch", BuildMulch());
            SaveWorld("Crop_SupportStake", BuildSupportStake());
            SaveWorld("Crop_Trellis", BuildTrellis());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Climate Maintenance",
                "Placeholder prefabs were generated under Assets/Resources/ClimateMaintenance/Prefabs. Replace their meshes later without changing the prefab names.",
                "OK");
        }


        [MenuItem("Tools/AgriDabao/Climate Maintenance/Configure Item Icons")]
        public static void ConfigureIcons()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Resources/ClimateMaintenance/Icons" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                if (changed) importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
        }

        private static void SaveWorld(string name, GameObject root)
        {
            root.name = name;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject BuildIrrigation()
        {
            GameObject root = NewRoot();
            AddCylinder(root, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.6f, 0.35f));
            AddCube(root, new Vector3(0f, 1.35f, 0f), new Vector3(0.18f, 0.18f, 2.8f));
            AddCube(root, new Vector3(0f, 1.35f, 0f), new Vector3(2.8f, 0.18f, 0.18f));
            return root;
        }

        private static GameObject BuildTank()
        {
            GameObject root = NewRoot();
            AddCylinder(root, new Vector3(0f, 1.4f, 0f), new Vector3(1.25f, 1.4f, 1.25f));
            AddCube(root, new Vector3(-0.9f, 0.5f, 0f), new Vector3(0.2f, 1f, 0.2f));
            AddCube(root, new Vector3(0.9f, 0.5f, 0f), new Vector3(0.2f, 1f, 0.2f));
            return root;
        }

        private static GameObject BuildShadeNet()
        {
            GameObject root = NewRoot();
            AddCube(root, new Vector3(-2f, 1.5f, -2f), new Vector3(0.15f, 3f, 0.15f));
            AddCube(root, new Vector3(2f, 1.5f, -2f), new Vector3(0.15f, 3f, 0.15f));
            AddCube(root, new Vector3(-2f, 1.5f, 2f), new Vector3(0.15f, 3f, 0.15f));
            AddCube(root, new Vector3(2f, 1.5f, 2f), new Vector3(0.15f, 3f, 0.15f));
            AddCube(root, new Vector3(0f, 3f, 0f), new Vector3(4.2f, 0.08f, 4.2f));
            return root;
        }

        private static GameObject BuildWindbreak()
        {
            GameObject root = NewRoot();
            for (int i = -2; i <= 2; i++)
            {
                AddCylinder(root, new Vector3(i * 1.4f, 1.3f, 0f), new Vector3(0.25f, 1.3f, 0.25f));
                AddSphere(root, new Vector3(i * 1.4f, 3f, 0f), new Vector3(1.1f, 1.5f, 1.1f));
            }
            return root;
        }

        private static GameObject BuildGreenhouse()
        {
            GameObject root = NewRoot();
            AddCube(root, new Vector3(0f, 1.5f, 0f), new Vector3(5f, 0.12f, 7f));
            AddCube(root, new Vector3(-2.4f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 7f));
            AddCube(root, new Vector3(2.4f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 7f));
            return root;
        }

        private static GameObject BuildCanal()
        {
            GameObject root = NewRoot();
            AddCube(root, new Vector3(0f, 0.06f, 0f), new Vector3(2f, 0.12f, 16f));
            return root;
        }

        private static GameObject BuildMulch()
        {
            GameObject root = NewRoot();
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = new Vector3(1.8f, 0.03f, 1.8f);
            return root;
        }

        private static GameObject BuildSupportStake()
        {
            GameObject root = NewRoot();
            AddCylinder(root, new Vector3(0.7f, 1.5f, 0f), new Vector3(0.08f, 1.5f, 0.08f));
            AddCube(root, new Vector3(0.35f, 1.6f, 0f), new Vector3(0.7f, 0.05f, 0.05f));
            return root;
        }

        private static GameObject BuildTrellis()
        {
            GameObject root = NewRoot();
            AddCube(root, new Vector3(-1.2f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 0.1f));
            AddCube(root, new Vector3(1.2f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 0.1f));
            for (int i = 0; i < 4; i++)
                AddCube(root, new Vector3(0f, 0.45f + i * 0.55f, 0f), new Vector3(2.4f, 0.06f, 0.06f));
            return root;
        }

        private static GameObject NewRoot()
        {
            return new GameObject("ClimatePrefabRoot");
        }

        private static void AddCube(GameObject root, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
        }

        private static void AddCylinder(GameObject root, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
        }

        private static void AddSphere(GameObject root, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
        }
    }
}
#endif
