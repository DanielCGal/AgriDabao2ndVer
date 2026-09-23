using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AgriDabao3D
{
    public static class TerrainModificationService
    {
        // Keyed by the Terrain object reference itself, not GetInstanceID().
        // Unity can recycle instance IDs within a session once the old Terrain
        // GameObject is destroyed (e.g. on scene reload), which previously made
        // this guard falsely think a patch was already applied to a brand-new
        // terrain and silently skip re-carving it - the bug behind raised beds /
        // drainage canals reverting after a farm save+reload. A ConditionalWeakTable
        // tracks patches per actual Terrain instance and cleans itself up once a
        // terrain is destroyed/GC'd, so there is no risk of ID collisions or leaks.
        private static readonly ConditionalWeakTable<Terrain, HashSet<string>> AppliedPatchesByTerrain =
            new ConditionalWeakTable<Terrain, HashSet<string>>();

        private static bool TryMarkApplied(Terrain terrain, string key)
        {
            HashSet<string> patches = AppliedPatchesByTerrain.GetOrCreateValue(terrain);
            return patches.Add(key);
        }

        /// <summary>
        /// Returns the world X/Z of the heightmap vertex nearest <paramref name="worldPosition"/>,
        /// leaving Y untouched.
        ///
        /// A terrain can only raise its vertices, so a mound's peak always lands on
        /// one - it cannot sit between them. On this terrain the vertices are about
        /// 2.7 m apart (700 m over 256 steps) while a raised bed is only 3.2 m across,
        /// so a crop left at an arbitrary position ends up on the side of its own bed.
        /// Snapping the crop to the vertex that becomes the peak puts it on top; the
        /// crop moves by at most half a vertex spacing, which is not noticeable.
        /// </summary>
        public static Vector3 SnapToHeightmapVertex(Terrain terrain, Vector3 worldPosition)
        {
            if (terrain == null || terrain.terrainData == null)
                return worldPosition;

            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            Vector3 local = worldPosition - terrain.transform.position;

            int vertexX = Mathf.RoundToInt(local.x / data.size.x * (resolution - 1));
            int vertexZ = Mathf.RoundToInt(local.z / data.size.z * (resolution - 1));
            vertexX = Mathf.Clamp(vertexX, 0, resolution - 1);
            vertexZ = Mathf.Clamp(vertexZ, 0, resolution - 1);

            return new Vector3(
                terrain.transform.position.x + vertexX * (data.size.x / (resolution - 1)),
                worldPosition.y,
                terrain.transform.position.z + vertexZ * (data.size.z / (resolution - 1)));
        }

        public static bool ApplyRaisedBed(Terrain terrain, Vector3 center, float radiusMeters, float heightMeters, string patchId)
        {
            if (terrain == null || terrain.terrainData == null) return false;
            string key = "raised:" + patchId;
            if (!TryMarkApplied(terrain, key)) return false;

            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            Vector3 local = center - terrain.transform.position;
            int centerX = Mathf.RoundToInt(local.x / data.size.x * (resolution - 1));
            int centerZ = Mathf.RoundToInt(local.z / data.size.z * (resolution - 1));
            int radiusX = Mathf.Max(1, Mathf.CeilToInt(radiusMeters / data.size.x * (resolution - 1)));
            int radiusZ = Mathf.Max(1, Mathf.CeilToInt(radiusMeters / data.size.z * (resolution - 1)));
            int x0 = Mathf.Clamp(centerX - radiusX, 0, resolution - 1);
            int z0 = Mathf.Clamp(centerZ - radiusZ, 0, resolution - 1);
            int width = Mathf.Clamp(centerX + radiusX - x0 + 1, 1, resolution - x0);
            int height = Mathf.Clamp(centerZ + radiusZ - z0 + 1, 1, resolution - z0);
            float[,] heights = data.GetHeights(x0, z0, width, height);
            float normalizedDelta = heightMeters / data.size.y;

            // Measure the falloff from the crop's true position, not from the rounded
            // sample index.
            //
            // centerX/centerZ are whole heightmap samples, and on this terrain one
            // sample spans about 2.7 m (700 m over 256 steps). Rounding the centre to
            // the nearest sample could therefore shift the peak up to ~1.4 m away from
            // the crop - close to half the bed's 3.2 m radius, which is why the crop
            // ended up on the side of the mound instead of on top of it. Using the
            // real world distance puts the peak exactly on the crop.
            float metresPerSampleX = data.size.x / (resolution - 1);
            float metresPerSampleZ = data.size.z / (resolution - 1);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sampleWorldX = (x0 + x) * metresPerSampleX;
                    float sampleWorldZ = (z0 + z) * metresPerSampleZ;
                    float nx = (sampleWorldX - local.x) / radiusMeters;
                    float nz = (sampleWorldZ - local.z) / radiusMeters;
                    float distance = Mathf.Sqrt(nx * nx + nz * nz);
                    if (distance > 1f) continue;
                    float falloff = Mathf.SmoothStep(0f, 1f, 1f - distance);
                    heights[z, x] = Mathf.Clamp01(heights[z, x] + normalizedDelta * falloff);
                }
            }
            data.SetHeights(x0, z0, heights);
            terrain.Flush();
            return true;
        }

        public static bool ApplyDrainageCanal(
            Terrain terrain,
            Vector3 center,
            Quaternion rotation,
            float lengthMeters,
            float widthMeters,
            float depthMeters,
            string patchId)
        {
            if (terrain == null || terrain.terrainData == null) return false;
            string key = "canal:" + patchId;
            if (!TryMarkApplied(terrain, key)) return false;

            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            float boundingRadius = Mathf.Sqrt(lengthMeters * lengthMeters + widthMeters * widthMeters) * 0.55f;
            Vector3 localCenter = center - terrain.transform.position;
            int centerX = Mathf.RoundToInt(localCenter.x / data.size.x * (resolution - 1));
            int centerZ = Mathf.RoundToInt(localCenter.z / data.size.z * (resolution - 1));
            int radiusX = Mathf.Max(1, Mathf.CeilToInt(boundingRadius / data.size.x * (resolution - 1)));
            int radiusZ = Mathf.Max(1, Mathf.CeilToInt(boundingRadius / data.size.z * (resolution - 1)));
            int x0 = Mathf.Clamp(centerX - radiusX, 0, resolution - 1);
            int z0 = Mathf.Clamp(centerZ - radiusZ, 0, resolution - 1);
            int sampleWidth = Mathf.Clamp(centerX + radiusX - x0 + 1, 1, resolution - x0);
            int sampleHeight = Mathf.Clamp(centerZ + radiusZ - z0 + 1, 1, resolution - z0);
            float[,] heights = data.GetHeights(x0, z0, sampleWidth, sampleHeight);
            Quaternion inverse = Quaternion.Inverse(rotation);
            float normalizedDepth = Mathf.Abs(depthMeters) / data.size.y;

            for (int z = 0; z < sampleHeight; z++)
            {
                for (int x = 0; x < sampleWidth; x++)
                {
                    float worldX = terrain.transform.position.x + (x0 + x) / (float)(resolution - 1) * data.size.x;
                    float worldZ = terrain.transform.position.z + (z0 + z) / (float)(resolution - 1) * data.size.z;
                    Vector3 relative = inverse * new Vector3(worldX - center.x, 0f, worldZ - center.z);
                    float along = Mathf.Abs(relative.z) / Mathf.Max(0.01f, lengthMeters * 0.5f);
                    float across = Mathf.Abs(relative.x) / Mathf.Max(0.01f, widthMeters * 0.5f);
                    if (along > 1f || across > 1f) continue;
                    float edge = Mathf.Clamp01(1f - Mathf.Max(along, across));
                    float falloff = Mathf.SmoothStep(0f, 1f, edge);
                    heights[z, x] = Mathf.Clamp01(heights[z, x] - normalizedDepth * falloff);
                }
            }
            data.SetHeights(x0, z0, heights);
            terrain.Flush();
            return true;
        }
    }
}
