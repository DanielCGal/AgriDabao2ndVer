using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Lists the triangle count of every imported model in one pass.
    ///
    /// The importer shows this per file in its preview pane, but the project has
    /// 54 of them, and the number that matters is how they compare to each other
    /// and to a mobile budget - which is impossible to judge one file at a time.
    /// This prints them sorted heaviest first so the worst offenders are obvious.
    /// </summary>
    public static class MeshStatsReporter
    {
        /// <summary>
        /// Rough per-model ceiling for a phone. A mid-range Android device can push
        /// somewhere around 100k triangles per frame in total, so any single crop
        /// costing more than this leaves almost nothing for the rest of the farm.
        /// </summary>
        private const int HeavyModelTriangles = 20000;

        /// <summary>Above this a single model can blow the whole frame budget on its own.</summary>
        private const int SevereModelTriangles = 100000;

        private sealed class ModelStats
        {
            public string Path;
            public string Name;
            public int Triangles;
            public int Vertices;
            public int SubMeshes;
            public int Meshes;
        }

        [MenuItem("Tools/AgriDabao/Report Mesh Triangle Counts")]
        public static void Report()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
            List<ModelStats> stats = new List<ModelStats>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(
                    "Counting triangles",
                    path,
                    (float)i / Mathf.Max(1, guids.Length));

                ModelStats entry = new ModelStats
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path)
                };

                // A single model file can contain several meshes; the cost of placing
                // it in the scene is the sum of all of them.
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub is not Mesh mesh)
                        continue;

                    entry.Meshes++;
                    entry.Vertices += mesh.vertexCount;
                    entry.SubMeshes += mesh.subMeshCount;

                    for (int s = 0; s < mesh.subMeshCount; s++)
                        entry.Triangles += (int)(mesh.GetIndexCount(s) / 3);
                }

                if (entry.Meshes > 0)
                    stats.Add(entry);
            }

            EditorUtility.ClearProgressBar();

            if (stats.Count == 0)
            {
                Debug.LogWarning("[MeshStats] No models found under Assets.");
                return;
            }

            stats = stats.OrderByDescending(s => s.Triangles).ToList();

            long total = stats.Sum(s => (long)s.Triangles);
            int heavy = stats.Count(s => s.Triangles >= HeavyModelTriangles);
            int severe = stats.Count(s => s.Triangles >= SevereModelTriangles);

            StringBuilder report = new StringBuilder();
            report.AppendLine($"[MeshStats] {stats.Count} models, {total:N0} triangles total.");
            report.AppendLine($"  {severe} model(s) at or above {SevereModelTriangles:N0} tris (severe)");
            report.AppendLine($"  {heavy} model(s) at or above {HeavyModelTriangles:N0} tris (heavy for mobile)");
            report.AppendLine();
            report.AppendLine($"{"TRIANGLES",12}  {"VERTS",10}  {"MESHES",7}  MODEL");

            foreach (ModelStats s in stats)
            {
                string flag = s.Triangles >= SevereModelTriangles ? "  <-- SEVERE"
                            : s.Triangles >= HeavyModelTriangles ? "  <-- heavy"
                            : string.Empty;

                report.AppendLine(
                    $"{s.Triangles,12:N0}  {s.Vertices,10:N0}  {s.Meshes,7}  {s.Name}{flag}");
            }

            Debug.Log(report.ToString());

            // The console truncates long messages, so the full table also goes to a
            // file that can be opened and sorted outside Unity.
            string outPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".",
                "MeshTriangleReport.csv");

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Triangles,Vertices,Meshes,SubMeshes,Model,Path");
            foreach (ModelStats s in stats)
                csv.AppendLine($"{s.Triangles},{s.Vertices},{s.Meshes},{s.SubMeshes},{s.Name},{s.Path}");

            System.IO.File.WriteAllText(outPath, csv.ToString());
            Debug.Log($"[MeshStats] Full table written to {outPath}");
        }
    }
}
