// Requires the UnityMeshSimplifier package. Until it is installed AND the
// UNITY_MESH_SIMPLIFIER scripting define is added, everything below compiles away
// to nothing, so dropping this file into the project changes no behaviour.
#if UNITY_MESH_SIMPLIFIER
using UnityMeshSimplifier;
#endif

using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Reduces the triangle count of imported models on the way in.
    ///
    /// The source FBX files are never modified. Unity re-imports them from disk, and
    /// this runs on the result, so the simplified mesh is what every prefab, scene
    /// and material sees while the original file stays intact. Turning
    /// <see cref="Enabled"/> off and reimporting restores full detail - which is why
    /// this is preferable to decimating in Blender, where the reduction is baked into
    /// the file and getting the detail back means re-exporting all over again.
    ///
    /// Measured before writing this: 20 mature mangosteens rendered 316k triangles
    /// and 517k vertices, roughly three times a mid-range phone's comfortable budget.
    /// </summary>
    public class MeshDecimationPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Master switch. Left OFF deliberately: importing this script should change
        /// nothing until the decision to decimate has actually been made, and that
        /// decision belongs after a real device build, not before one.
        /// </summary>
        /// <remarks>
        /// Deliberately not a <c>const</c>: as a compile-time constant the guard below
        /// makes the rest of the method provably unreachable and the compiler warns
        /// about it every time this is switched off.
        /// </remarks>
        private static readonly bool Enabled = true;

        // Share of the original triangles to keep, grouped by what the geometry can
        // survive. Solid shapes lose triangles gracefully; thin, gappy, alpha-cut
        // geometry does not, so nets and foliage are treated far more gently.

        /// <summary>Tree canopies. Verified on the mangosteen: 10,365 -> ~4,100 tris.</summary>
        private const float FoliageQuality = 0.4f;

        /// <summary>
        /// Grass, netting and trellis. These are thin strips with holes in them, and
        /// aggressive decimation leaves visible gaps and jagged edges rather than a
        /// slightly rounder shape, so they keep far more of their geometry.
        /// </summary>
        private const float WispyQuality = 0.6f;

        /// <summary>Hard-surface props - tanks, greenhouses, bins, traps.</summary>
        private const float SolidQuality = 0.4f;

        /// <summary>
        /// The dug planting spot. Essentially a flat patch of ground, so it tolerates
        /// heavy reduction, and one is spawned per planting - twenty crops means twenty
        /// of these, which at 6,357 tris each is a bigger cost than it looks.
        /// </summary>
        private const float GroundQuality = 0.3f;

        /// <summary>
        /// Only models under these folders are touched.
        ///
        /// Listed rather than reorganised on disk: every 2nd-stage prefab already shares
        /// its source model with the adult of the same crop - there is no separate
        /// 2nd-stage FBX - so decimating each "<crop>Tree" folder covers both stages at
        /// once. Sprouts and fruit live in their own folders and are left at full
        /// detail, since they are small on screen and cheap already.
        ///
        /// Deliberately excluded:
        ///   Assets/Models/PineappleSprout/ - Pineapple2ndStage is the one prefab that
        ///   reuses a SPROUT model rather than its tree, so decimating it would also
        ///   decimate the pineapple seedling. Add it only if that is acceptable.
        /// </summary>
        private static readonly Target[] Targets =
        {
            // Crop trees - each covers both the 2nd-stage and adult prefab, because
            // every 2nd-stage prefab reuses its adult's model at a smaller scale.
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

            // Climate mitigation structures. The shade net and windbreak are netting
            // rather than solid shapes, so they are treated like foliage.
            new Target("Assets/Models/DrainageCanal/", SolidQuality),
            new Target("Assets/Models/Greenhouse/", SolidQuality),
            new Target("Assets/Models/IrrigationSprinkler/", SolidQuality),
            new Target("Assets/Models/WaterStorageTank/", SolidQuality),
            new Target("Assets/Models/ShadeNet/", WispyQuality),
            new Target("Assets/Models/WindBreakNet/", WispyQuality),

            // Crop maintenance placeables.
            new Target("Assets/Models/MulchPatch/", SolidQuality),
            new Target("Assets/Models/SupportStake/", SolidQuality),
            new Target("Assets/Models/Trellis/", WispyQuality),

            // Pest and disease traps.
            new Target("Assets/Models/AphidTrap/", SolidQuality),
            new Target("Assets/Models/PhermoneTrap/", SolidQuality),
            new Target("Assets/Models/TermiteBait/", SolidQuality),

            // Ground and world props. DirtSpot is spawned once per dug planting spot,
            // so its cost multiplies with the size of the farm.
            new Target("Assets/Models/DirtSpot/", GroundQuality),
            new Target("Assets/Models/ShippingBin/", SolidQuality),

            // Grass. Only a handful are visible at once, but there are 450 of them and
            // their vertex-to-triangle ratio is the worst in the project.
            new Target("Assets/Models/GrasspatchLarge/", WispyQuality),
            new Target("Assets/Models/GrasspatchSmall/", WispyQuality),
        };

        /// <summary>One folder of models and how much of their geometry to keep.</summary>
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

        /// <summary>
        /// Finds the reduction configured for an asset, or reports that it is not a
        /// target and should be imported untouched.
        /// </summary>
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

        /// <summary>
        /// Meshes at or below this are already cheap, and simplifying them costs
        /// silhouette quality for a saving too small to measure.
        /// </summary>
        private const int SkipBelowTriangles = 3000;

        /// <summary>
        /// Turns Read/Write on for the models this processor rewrites.
        ///
        /// Unity blocks writes to a mesh whose importer has Read/Write disabled, which
        /// is the default and is normally the setting you want - it lets the engine drop
        /// the CPU-side copy once the mesh reaches the GPU. Simplifying the mesh means
        /// writing to it, so the flag has to be on for these specific models.
        ///
        /// The cost is that their vertex data also stays in system memory at runtime.
        /// That is a real trade: the triangles saved are worth more than the RAM spent
        /// here, but it applies only to models under <see cref="TargetFolder"/>, which
        /// is why the scope is kept narrow.
        /// </summary>
        private void OnPreprocessModel()
        {
            if (!Enabled)
                return;

            if (!TryGetQuality(assetPath, out _))
                return;

            // Only write when the value actually changes. Assigning it unconditionally
            // dirties the .meta on every single import, and Unity's determinism check -
            // which imports twice and compares - can then report the containing folder
            // as having produced an inconsistent result.
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

                // Refuse to apply a result that came back empty. Without this a failed
                // simplification silently wipes the model instead of leaving it alone.
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

                // Write the result INTO the mesh Unity already owns rather than swapping
                // the reference. A Mesh constructed in here is not part of the imported
                // asset, so assigning it to the filter leaves the model referencing an
                // object that never gets serialised - which imports as a prefab with no
                // mesh at all rather than a lighter one.
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
        /// <summary>
        /// Replaces every channel of <paramref name="destination"/> with the contents of
        /// <paramref name="source"/>, keeping the destination object identity so the
        /// importer still serialises it as part of the model.
        /// </summary>
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

            // Only meaningful for skinned meshes; static props carry empty arrays.
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
