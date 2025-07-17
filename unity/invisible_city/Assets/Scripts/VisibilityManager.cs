// Assets/Scripts/PointCloudVisibilityManager.cs
using UnityEngine;

public class PointCloudVisibilityManager : MonoBehaviour
{
    [Header("Compute shader (frustum + occlusion)")]
    public ComputeShader visibilityCS;

    [Header("Managed point-cloud renderers")]
    public BuildingPointCloudRenderer[] renderers;

    [Header("Memory dynamics")]
    [Range(0,1)] public float learnRate  = 0.4f;   // higher = remember faster
    [Range(0,1)] public float forgetRate = 0.2f;   // higher = forget faster

    static readonly int ID_Count        = Shader.PropertyToID("Count");
    static readonly int ID_CameraVP     = Shader.PropertyToID("CameraVP");
    static readonly int ID_LocalToWorld = Shader.PropertyToID("LocalToWorld");
    static readonly int ID_DeltaTime    = Shader.PropertyToID("DeltaTime");
    static readonly int ID_LearnRate    = Shader.PropertyToID("LearnRate");
    static readonly int ID_ForgetRate   = Shader.PropertyToID("ForgetRate");
    static readonly int ID_ScreenSize   = Shader.PropertyToID("ScreenSize");

    int    kernel;
    Camera cam;

    void Awake()
    {
        cam    = Camera.main;
        kernel = visibilityCS.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        if (visibilityCS == null || kernel < 0) return;

        Matrix4x4 vp   = cam.projectionMatrix * cam.worldToCameraMatrix;
        Vector2   size = new Vector2(Screen.width, Screen.height);
        float     dt   = Time.deltaTime;

        var depthTex = Shader.GetGlobalTexture("_CustomDepthTexture");
        if (depthTex == null) return;                               // depth not ready first frame or two

        visibilityCS.SetTexture(kernel, "DepthGraphTexture", depthTex);
        visibilityCS.SetVector (ID_ScreenSize, size);
        visibilityCS.SetFloat  (ID_DeltaTime,  dt);
        visibilityCS.SetFloat  (ID_LearnRate,  learnRate);
        visibilityCS.SetFloat  (ID_ForgetRate, forgetRate);
        visibilityCS.SetMatrix (ID_CameraVP,   vp);

        foreach (var r in renderers)
        {
            int cnt    = r.PointCount;
            if (cnt == 0) continue;
            int groups = Mathf.CeilToInt(cnt / 64f);

            visibilityCS.SetInt   (ID_Count,        cnt);
            visibilityCS.SetMatrix(ID_LocalToWorld, r.transform.localToWorldMatrix);

            visibilityCS.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            visibilityCS.SetBuffer(kernel, "MemoryBuffer",   r.MemoryBuffer);

            visibilityCS.Dispatch(kernel, groups, 1, 1);
        }
    }
}
