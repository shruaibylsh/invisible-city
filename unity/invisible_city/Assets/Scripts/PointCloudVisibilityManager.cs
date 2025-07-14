using UnityEngine;
using UnityEngine.VFX;


public class PointCloudVisibilityManager : MonoBehaviour
{
    [Tooltip("Compute shader asset")]
    public ComputeShader frustumShader;
    [Tooltip("Renderer components to update")]
    public BuildingPointCloudRenderer[] renderers;

    int kernel;
    Camera cam;

    void Start()
    {
        cam    = Camera.main;
        kernel = frustumShader.FindKernel("CSMain");
    }

    void LateUpdate()
{
    // 1) Are we even here?
    Debug.Log("[VisibilityManager] LateUpdate()");

    // 2) Compute the camera VP matrix
    Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
    Debug.Log($"[VisibilityManager] CameraVP m00 = {vp.m00}  m11 = {vp.m11}");

    foreach (var r in renderers)
    {
        int count  = r.PointCount;
        int groups = Mathf.CeilToInt(count / 64f);

        Debug.Log($"[VisibilityManager] Dispatching on '{r.name}': count={count}, groups={groups}, kernel={kernel}");

        // Bind inputs
        frustumShader.SetMatrix("CameraVP", vp);
        frustumShader.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
        frustumShader.SetBuffer(kernel, "VisibleBuffer",  r.VisibleBuffer);

        // Dispatch
        frustumShader.Dispatch(kernel, groups, 1, 1);

        // 3) Sample & log first 5 flags
        uint[] sample = new uint[Mathf.Min(5, count)];
        r.VisibleBuffer.GetData(sample, 0, 0, sample.Length);
        Debug.Log($"[{r.name}] VisibleBuffer[0..{sample.Length-1}] = {string.Join(", ", sample)}");

        // 4) Log alive-particle count
        var vfx = r.GetComponent<VisualEffect>();
        Debug.Log($"[{r.name}] aliveParticleCount = {vfx.aliveParticleCount}");
    }
}

}
