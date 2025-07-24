using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Baked point-cloud asset")]
    [SerializeField] PointCloudData pointCloudData;
    public PointCloudData PointCloudData => pointCloudData;

    [Header("Tint colour")]
    [SerializeField] Color pointTint = Color.white;

    public GraphicsBuffer PositionBuffer => positionBuffer;
    public GraphicsBuffer MemoryBuffer   => memoryBuffer;
    public GraphicsBuffer FinalPositionBuffer => finalPositionBuffer;
    public GraphicsBuffer VisibilityBuffer => visibilityBuffer;
    public int            PointCount    => pointCount;


    GraphicsBuffer positionBuffer;
    GraphicsBuffer memoryBuffer;
    GraphicsBuffer finalPositionBuffer;
    GraphicsBuffer visibilityBuffer;
    VisualEffect   vfx;
    int            pointCount;
    GraphicsBuffer prevRayDirBuffer;
GraphicsBuffer prevVisibilityBuffer;

public GraphicsBuffer PrevRayDirBuffer => prevRayDirBuffer;
public GraphicsBuffer PrevVisibilityBuffer => prevVisibilityBuffer;


    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_MemoryBuffer   = Shader.PropertyToID("MemoryBuffer");
    static readonly int ID_FinalPositionBuffer = Shader.PropertyToID("FinalPositionBuffer");
    static readonly int ID_VisibilityBuffer = Shader.PropertyToID("VisibilityBuffer");
    static readonly int ID_SpawnCount     = Shader.PropertyToID("SpawnCount");
    static readonly int ID_PointTint      = Shader.PropertyToID("PointColor");

    void Awake()
    {
        if (pointCloudData == null || pointCloudData.positions == null || pointCloudData.positions.Length == 0)
        {
            Debug.LogError($"[{name}] PointCloudData is missing or empty"); enabled = false; return;
        }

        pointCount = pointCloudData.positions.Length;
        vfx        = GetComponent<VisualEffect>();

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
        positionBuffer.SetData(pointCloudData.positions);
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)pointCount);

        memoryBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float));
        memoryBuffer.SetData(new float[pointCount]);
        vfx.SetGraphicsBuffer(ID_MemoryBuffer, memoryBuffer);

        finalPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
        finalPositionBuffer.SetData(new Vector3[pointCount]);
        vfx.SetGraphicsBuffer(ID_FinalPositionBuffer, finalPositionBuffer);

        visibilityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(int));
        visibilityBuffer.SetData(new int[pointCount]);

        prevRayDirBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
prevRayDirBuffer.SetData(new Vector3[pointCount]);

prevVisibilityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(int));
prevVisibilityBuffer.SetData(new int[pointCount]);

        vfx.SetVector4(ID_PointTint, pointTint);
        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        memoryBuffer?.Release();
        finalPositionBuffer?.Release();
        visibilityBuffer?.Release();
        prevRayDirBuffer?.Release();
prevVisibilityBuffer?.Release();

    }
}
