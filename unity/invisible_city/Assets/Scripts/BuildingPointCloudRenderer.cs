// Assets/Scripts/BuildingPointCloudRenderer.cs
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Point-cloud data (baked asset)")]
    [SerializeField] PointCloudData pointCloudData;

    [Header("Visual settings")]
    [SerializeField] Color pointColor = Color.white;

    GraphicsBuffer positionBuffer;
    GraphicsBuffer memoryBuffer;
    VisualEffect   vfx;
    int            pointCount;

    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_MemoryBuffer   = Shader.PropertyToID("MemoryBuffer");
    static readonly int ID_SpawnCount     = Shader.PropertyToID("SpawnCount");
    static readonly int ID_PointColor     = Shader.PropertyToID("PointColor");

    // Exposed for the visibility manager
    public GraphicsBuffer PositionBuffer => positionBuffer;
    public GraphicsBuffer MemoryBuffer   => memoryBuffer;
    public int            PointCount     => pointCount;

    void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        pointCount = pointCloudData.positions.Length;

        // 1) Position buffer
        positionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(float) * 3
        );
        positionBuffer.SetData(pointCloudData.positions);
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)pointCount);

        // 2) Memory buffer (all ones = fully visible)
        memoryBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(float)
        );
        var ones = new float[pointCount];
        for (int i = 0; i < pointCount; i++) ones[i] = 1f;
        memoryBuffer.SetData(ones);
        vfx.SetGraphicsBuffer(ID_MemoryBuffer, memoryBuffer);

        // 3) Color parameter (drives alpha in your graph)
        vfx.SetVector4(ID_PointColor, pointColor);

        // 4) Hide original mesh
        if (TryGetComponent<MeshRenderer>(out var mr))
            mr.enabled = false;

        // 5) Spawn all points
        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        memoryBuffer?.Release();
    }
}
