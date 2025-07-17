using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class PointCloudSamplerEditor
{
    const float DENSITY  = 300f;
    const string SAVE_DIR = "Assets/PointCloudBakes";

    /* ---------- menu registration ---------- */
    [MenuItem("Assets/Bake Point Cloud (300 per m²)", true)]
    static bool ValidateBake() => GetMesh(Selection.activeObject) != null;

    [MenuItem("Assets/Bake Point Cloud (300 per m²)")]
    static void Bake()                                  // single entry point
    {
        Mesh mesh = GetMesh(Selection.activeObject);
        if (mesh == null) { Debug.LogError("No mesh found."); return; }

        var verts  = mesh.vertices;
        var tris   = mesh.triangles;
        var norms  = mesh.normals.Length == verts.Length ? mesh.normals : null;

        /* ----- build triangle-area CDF ----- */
        int triCount = tris.Length / 3;
        var cdf = new float[triCount];
        float totalArea = 0f;
        for (int i = 0; i < triCount; i++)
        {
            Vector3 a = verts[tris[i*3]];
            Vector3 b = verts[tris[i*3+1]];
            Vector3 c = verts[tris[i*3+2]];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            totalArea += area;
            cdf[i] = totalArea;
        }

        int targetPts = Mathf.CeilToInt(totalArea * DENSITY);
        var posList  = new List<Vector3>(targetPts);
        var normList = new List<Vector3>(targetPts);

        /* ----- sample barycentric points ----- */
        for (int p = 0; p < targetPts; p++)
        {
            float r = Random.value * totalArea;            // choose triangle
            int lo = 0, hi = triCount - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (r < cdf[mid]) hi = mid; else lo = mid + 1;
            }
            int t = lo;

            Vector3 v0 = verts[tris[t*3]];
            Vector3 v1 = verts[tris[t*3+1]];
            Vector3 v2 = verts[tris[t*3+2]];

            float u = Random.value, v = Random.value;
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }
            Vector3 pos = v0 + u*(v1 - v0) + v*(v2 - v0);
            posList.Add(pos);

            Vector3 n = norms != null
                        ? (norms[tris[t*3]] + norms[tris[t*3+1]] + norms[tris[t*3+2]]).normalized
                        : Vector3.up;
            normList.Add(n);
        }

        /* ----- save as ScriptableObject asset ----- */
        if (!Directory.Exists(SAVE_DIR)) Directory.CreateDirectory(SAVE_DIR);
        string meshName = mesh.name.Replace(" ", "_");
        string path = $"{SAVE_DIR}/{meshName}.asset";

        var asset = ScriptableObject.CreateInstance<PointCloudData>();
        asset.positions = posList.ToArray();
        asset.normals   = normList.ToArray();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Point cloud baked: {targetPts} pts  (300 per m²)  →  {path}");
    }

    /* ---------- helper ---------- */
    static Mesh GetMesh(Object obj) =>
        obj switch
        {
            Mesh m                                                  => m,
            GameObject go when go.TryGetComponent(out MeshFilter mf) => mf.sharedMesh,
            GameObject go when go.TryGetComponent(out SkinnedMeshRenderer smr)
                                                                     => smr.sharedMesh,
            _                                                       => null
        };
}
