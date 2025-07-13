using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Point-cloud data (baked asset)")]
    [SerializeField] PointCloudData pointCloudData;

    [Header("Visual settings")]
    [SerializeField] float baseSize = 0.02f;
    [SerializeField] Color pointColor = Color.white;

    GraphicsBuffer positionBuffer;
    VisualEffect vfx;

    // Cache property IDs once (avoids string hashing every frame)
    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_SpawnCount = Shader.PropertyToID("SpawnCount");
    static readonly int ID_BaseSize = Shader.PropertyToID("BaseSize");
    static readonly int ID_PointColor = Shader.PropertyToID("PointColor");

    void Awake()
    {
        // ---------- basic sanity ----------
        if (pointCloudData == null || pointCloudData.positions == null || pointCloudData.positions.Length == 0)
        {
            Debug.LogError($"{name}: PointCloudData asset missing or empty.");
            enabled = false;
            return;
        }

        vfx = GetComponent<VisualEffect>();

        // ---------- allocate GPU buffer ----------
        int count = pointCloudData.positions.Length;
        int stride = sizeof(float) * 3;          // Vector3 = 12 bytes
        positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        positionBuffer.SetData(pointCloudData.positions);

        // ---------- push data into VFX Graph ----------
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)count);
        vfx.SetFloat(ID_BaseSize, baseSize);
        vfx.SetVector4(ID_PointColor, pointColor);

        // ---------- optional: hide original mesh ----------
        if (TryGetComponent(out MeshRenderer mr)) mr.enabled = false;

        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        positionBuffer?.Dispose();   // release GPU memory
        positionBuffer = null;
    }
    
    void Update()
{
    Debug.Log($"Alive particles: {vfx.aliveParticleCount}");
    enabled = false;  // only log once
}

}