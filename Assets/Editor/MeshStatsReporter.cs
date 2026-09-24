using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public static class MeshStatsReporter
    {
        private const int HeavyModelTriangles = 20000;

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
