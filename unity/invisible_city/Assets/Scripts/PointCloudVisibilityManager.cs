// Assets/Scripts/PointCloudVisibilityManager.cs
using UnityEngine;
using UnityEngine.VFX;

public class PointCloudVisibilityManager : MonoBehaviour
{
    [Tooltip("Compute shader asset")]
    public ComputeShader frustumShader;
    [Tooltip("Renderer components to update")]
    public BuildingPointCloudRenderer[] renderers;

    int    kernel;
    Camera cam;

    static readonly int ID_Count        = Shader.PropertyToID("Count");
    static readonly int ID_CameraVP     = Shader.PropertyToID("CameraVP");
    static readonly int ID_LocalToWorld = Shader.PropertyToID("LocalToWorld");

    void Start()
    {
        cam    = Camera.main;
        kernel = frustumShader.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        // 1) Compute view-projection
        Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;

        foreach (var r in renderers)
        {
            int count  = r.PointCount;
            int groups = Mathf.CeilToInt(count / 64f);

            // 2) Bind
            frustumShader.SetInt   (ID_Count,        count);
            frustumShader.SetMatrix(ID_CameraVP,     vp);
            frustumShader.SetMatrix(ID_LocalToWorld, r.transform.localToWorldMatrix);
            frustumShader.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            frustumShader.SetBuffer(kernel, "MemoryBuffer",   r.MemoryBuffer);

            // 3) Dispatch
            frustumShader.Dispatch(kernel, groups, 1, 1);

            // 4) Debug sample
            var sample = new float[Mathf.Min(5, count)];
            r.MemoryBuffer.GetData(sample, 0, 0, sample.Length);
            Debug.Log($"[{r.name}] MemoryBuffer[0..{sample.Length-1}] = {string.Join(", ", sample)}");

            // 5) Debug alive count
            var vfx = r.GetComponent<VisualEffect>();
            Debug.Log($"[{r.name}] aliveParticleCount = {vfx.aliveParticleCount}");
        }
    }
}
