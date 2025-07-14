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
        // 1) Confirm we’re running each frame
        Debug.Log("[VisibilityManager] LateUpdate()");

        // 2) Compute and log the camera’s view-proj matrix
        Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
        Debug.Log($"[VisibilityManager] CameraVP m00 = {vp.m00}  m11 = {vp.m11}");

        foreach (var r in renderers)
        {
            int count  = r.PointCount;
            int groups = Mathf.CeilToInt(count / 64f);

            Debug.Log($"[VisibilityManager] Dispatching on '{r.name}': count={count}, groups={groups}, kernel={kernel}");

            // 3) Bind uniforms and buffers, including each renderer’s transform
            frustumShader.SetInt   (ID_Count,        count);
            frustumShader.SetMatrix(ID_CameraVP,     vp);
            frustumShader.SetMatrix(ID_LocalToWorld, r.transform.localToWorldMatrix);
            frustumShader.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            frustumShader.SetBuffer(kernel, "VisibleBuffer",  r.VisibleBuffer);

            // 4) Dispatch the frustum-test kernel
            frustumShader.Dispatch(kernel, groups, 1, 1);

            // 5) Read back a small sample of flags for debugging
            uint[] sample = new uint[Mathf.Min(5, count)];
            r.VisibleBuffer.GetData(sample, 0, 0, sample.Length);
            Debug.Log($"[{r.name}] VisibleBuffer[0..{sample.Length-1}] = {string.Join(", ", sample)}");

            // 6) Log how many particles remain alive in the VFX
            var vfx = r.GetComponent<VisualEffect>();
            Debug.Log($"[{r.name}] aliveParticleCount = {vfx.aliveParticleCount}");
        }
    }
}
