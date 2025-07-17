using UnityEngine;

public class PointCloudVisibilityManager : MonoBehaviour
{
    public ComputeShader frustumShader;
    public BuildingPointCloudRenderer[] renderers;

    [Header("Memory dynamics")]
    public float learnRate = 0.35f;
    public float forgetRate = 0.175f;

    int kernel;
    Camera cam;

    static readonly int ID_Count = Shader.PropertyToID("Count");
    static readonly int ID_CameraVP = Shader.PropertyToID("CameraVP");
    static readonly int ID_LocalToWorld = Shader.PropertyToID("LocalToWorld");
    static readonly int ID_DeltaTime = Shader.PropertyToID("DeltaTime");
    static readonly int ID_LearnRate = Shader.PropertyToID("LearnRate");
    static readonly int ID_ForgetRate = Shader.PropertyToID("ForgetRate");

    void Start()
    {
        cam = Camera.main;
        kernel = frustumShader.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
        float dt = Time.deltaTime;

        foreach (var r in renderers)
        {
            int count = r.PointCount;
            int groups = Mathf.CeilToInt(count / 64f);

            frustumShader.SetInt(ID_Count, count);
            frustumShader.SetMatrix(ID_CameraVP, vp);
            frustumShader.SetMatrix(ID_LocalToWorld, r.transform.localToWorldMatrix);
            frustumShader.SetFloat(ID_DeltaTime, dt);
            frustumShader.SetFloat(ID_LearnRate, learnRate);
            frustumShader.SetFloat(ID_ForgetRate, forgetRate);

            frustumShader.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            frustumShader.SetBuffer(kernel, "MemoryBuffer", r.MemoryBuffer);
            frustumShader.SetBuffer(kernel, "VisibleBuffer", r.VisibleBuffer);

            frustumShader.Dispatch(kernel, groups, 1, 1);
        }
    }
}