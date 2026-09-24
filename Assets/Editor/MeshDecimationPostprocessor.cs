#if UNITY_MESH_SIMPLIFIER
using UnityMeshSimplifier;
#endif

using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public class MeshDecimationPostprocessor : AssetPostprocessor
    {
        private static readonly bool Enabled = true;

        private const float FoliageQuality = 0.4f;

        private const float WispyQuality = 0.6f;

        private const float SolidQuality = 0.4f;

        private const float GroundQuality = 0.3f;

        private static readonly Target[] Targets =
        {
            new Target("Assets/Models/BananaTree/", FoliageQuality),
            new Target("Assets/Models/CacaoTree/", FoliageQuality),
            new Target("Assets/Models/CoconutTree/", FoliageQuality),
            new Target("Assets/Models/CornTree/", FoliageQuality),
            new Target("Assets/Models/DurianTree/", FoliageQuality),
            new Target("Assets/Models/EggplantTree/", FoliageQuality),
            new Target("Assets/Models/MangoTree/", FoliageQuality),
            new Target("Assets/Models/MangosteenTree/", FoliageQuality),
            new Target("Assets/Models/PineappleTree/", FoliageQuality),
            new Target("Assets/Models/PomeloTree/", FoliageQuality),
            new Target("Assets/Models/SquashTree/", FoliageQuality),
            new Target("Assets/Models/StrawberryTree/", FoliageQuality),
            new Target("Assets/Models/TomatoTree/", FoliageQuality),

            new Target("Assets/Models/DrainageCanal/", SolidQuality),
            new Target("Assets/Models/Greenhouse/", SolidQuality),
            new Target("Assets/Models/IrrigationSprinkler/", SolidQuality),
            new Target("Assets/Models/WaterStorageTank/", SolidQuality),
            new Target("Assets/Models/ShadeNet/", WispyQuality),
            new Target("Assets/Models/WindBreakNet/", WispyQuality),

            new Target("Assets/Models/MulchPatch/", SolidQuality),
            new Target("Assets/Models/SupportStake/", SolidQuality),
            new Target("Assets/Models/Trellis/", WispyQuality),

            new Target("Assets/Models/AphidTrap/", SolidQuality),
            new Target("Assets/Models/PhermoneTrap/", SolidQuality),
            new Target("Assets/Models/TermiteBait/", SolidQuality),

            new Target("Assets/Models/DirtSpot/", GroundQuality),
            new Target("Assets/Models/ShippingBin/", SolidQuality),

            new Target("Assets/Models/GrasspatchLarge/", WispyQuality),
            new Target("Assets/Models/GrasspatchSmall/", WispyQuality),

            new Target("Assets/Models/SeedlingTent/", SolidQuality),
            new Target("Assets/Models/NurseryRack/", SolidQuality),
            new Target("Assets/Models/SeedlingBag_Empty/", SolidQuality),
            new Target("Assets/Models/SeedlingBag_Filled/", SolidQuality),
            new Target("Assets/Models/SproutBanana/", FoliageQuality),
            new Target("Assets/Models/SproutCacao/", FoliageQuality),
            new Target("Assets/Models/SproutCorn/", FoliageQuality),
            new Target("Assets/Models/SproutDurian/", FoliageQuality),
            new Target("Assets/Models/SproutEggplant/", FoliageQuality),
            new Target("Assets/Models/SproutMango/", FoliageQuality),
            new Target("Assets/Models/SproutMangosteen/", FoliageQuality),
            new Target("Assets/Models/SproutPomelo/", FoliageQuality),
            new Target("Assets/Models/SproutSquash/", FoliageQuality),
            new Target("Assets/Models/SproutTomato/", FoliageQuality),
            new Target("Assets/Models/Sucker_Banana/", FoliageQuality),
            new Target("Assets/Models/Sucker_Pineapple/", FoliageQuality),
            new Target("Assets/Models/Runner_Strawberry/", FoliageQuality),
            new Target("Assets/Models/Seednut_Coconut/", SolidQuality),
        };

        private readonly struct Target
        {
            public readonly string Folder;
            public readonly float Quality;

            public Target(string folder, float quality)
            {
                Folder = folder;
                Quality = quality;
            }
        }

        private static bool TryGetQuality(string path, out float quality)
        {
            foreach (Target target in Targets)
            {
                if (path.StartsWith(target.Folder, System.StringComparison.OrdinalIgnoreCase))
                {
                    quality = target.Quality;
                    return true;
                }
            }

            quality = 0f;
            return false;
        }

        private const int SkipBelowTriangles = 3000;

        private void OnPreprocessModel()
        {
            if (!Enabled)
                return;

            if (!TryGetQuality(assetPath, out _))
                return;

            if (assetImporter is ModelImporter importer && !importer.isReadable)
                importer.isReadable = true;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!Enabled)
                return;

            if (!TryGetQuality(assetPath, out float quality))
                return;

#if UNITY_MESH_SIMPLIFIER
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                int before = mesh.triangles.Length / 3;
                if (before <= SkipBelowTriangles)
                    continue;

                MeshSimplifier simplifier = new MeshSimplifier();
                simplifier.Initialize(mesh);
                simplifier.SimplifyMesh(quality);

                Mesh simplified = simplifier.ToMesh();

                if (simplified == null ||
                    simplified.vertexCount == 0 ||
                    simplified.subMeshCount == 0)
                {
                    Debug.LogWarning(
                        $"[MeshDecimation] {mesh.name}: simplification returned an empty " +
                        "mesh, so the original was kept.");

                    if (simplified != null)
                        UnityEngine.Object.DestroyImmediate(simplified);

                    continue;
                }

                CopyInto(simplified, mesh);
                UnityEngine.Object.DestroyImmediate(simplified);

                int after = mesh.triangles.Length / 3;
                Debug.Log(
                    $"[MeshDecimation] {mesh.name}: {before:N0} -> {after:N0} tris " +
                    $"({(1f - (float)after / Mathf.Max(1, before)) * 100f:F0}% reduction)");
            }
#endif
        }

#if UNITY_MESH_SIMPLIFIER
        private static void CopyInto(Mesh source, Mesh destination)
        {
            destination.Clear();

            destination.indexFormat = source.indexFormat;
            destination.vertices = source.vertices;
            destination.normals = source.normals;
            destination.tangents = source.tangents;
            destination.uv = source.uv;
            destination.uv2 = source.uv2;
            destination.uv3 = source.uv3;
            destination.uv4 = source.uv4;
            destination.colors = source.colors;

            destination.boneWeights = source.boneWeights;
            destination.bindposes = source.bindposes;

            destination.subMeshCount = source.subMeshCount;
            for (int i = 0; i < source.subMeshCount; i++)
                destination.SetTriangles(source.GetTriangles(i), i, false);

            destination.RecalculateBounds();
        }
#endif
    }
}
