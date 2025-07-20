using UnityEngine;

public class PointCloudVisibilityManager : MonoBehaviour
{
    public ComputeShader visibilityCS;
    public BuildingPointCloudRenderer[] renderers;
    public Camera activeCamera;
    [Range(0,1)] public float baseLearnRate = 0.4f;
    [Range(0,1)] public float baseForgetRate = 0.2f;
    public float cullRadius = 50f;
    public float driftAmplitude = 0.15f;

    int kernel;

    void Awake()
    {
        kernel = visibilityCS.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        if (visibilityCS == null || activeCamera == null) return;

        Matrix4x4 vp = activeCamera.projectionMatrix * activeCamera.worldToCameraMatrix;
        Vector3 camPos = activeCamera.transform.position;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        float radiusSqr = cullRadius * cullRadius;

        visibilityCS.SetVector("ScreenSize", screenSize);
        visibilityCS.SetFloat("DeltaTime", Time.deltaTime);
        visibilityCS.SetFloat("LearnRate", baseLearnRate);
        visibilityCS.SetFloat("ForgetRate", baseForgetRate);
        visibilityCS.SetMatrix("CameraVP", vp);
        visibilityCS.SetVector("CameraPosition", camPos);
        visibilityCS.SetFloat("CullRadiusSqr", radiusSqr);
        visibilityCS.SetFloat("Amplitude", driftAmplitude);
        visibilityCS.SetFloat("GlobalTime", Time.time);

        foreach (var r in renderers)
        {
            int cnt = r.PointCount;
            if (cnt == 0) continue;

            visibilityCS.SetInt("Count", cnt);
            visibilityCS.SetMatrix("LocalToWorld", r.transform.localToWorldMatrix);
            visibilityCS.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            visibilityCS.SetBuffer(kernel, "MemoryBuffer", r.MemoryBuffer);
            visibilityCS.SetBuffer(kernel, "VisibilityBuffer", r.VisibilityBuffer);
            visibilityCS.SetBuffer(kernel, "FinalPositionBuffer", r.FinalPositionBuffer);

            int groups = Mathf.CeilToInt(cnt / 64f);
            visibilityCS.Dispatch(kernel, groups, 1, 1);

            Debug.Log($"[VisibilityManager] Processed {cnt} points for renderer {r.name}.");
        }
    }
}
