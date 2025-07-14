// Assets/Scripts/BuildingPointCloudRenderer.cs
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
    static readonly int ID_SpawnCount      = Shader.PropertyToID("SpawnCount");
    static readonly int ID_BaseSize        = Shader.PropertyToID("BaseSize");
    static readonly int ID_PointColor      = Shader.PropertyToID("PointColor");

    void Awake()
    {

        vfx = GetComponent<VisualEffect>();

        // Allocate GPU buffer
        int count  = pointCloudData.positions.Length;
        int stride = sizeof(float) * 3; // Vector3 = 12 bytes
        positionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            count,
            stride
        );
        positionBuffer.SetData(pointCloudData.positions);

        // Push data into VFX Graph
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)count);
        vfx.SetFloat(ID_BaseSize, baseSize);
        vfx.SetVector4(ID_PointColor, pointColor);

        // Optionally hide the original meshrenderer
        if (TryGetComponent<MeshRenderer>(out var mr))
            mr.enabled = false;

        // Trigger spawn (assumes your VFX Graph listens for "SpawnEvent")
        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        // Release GPU memory
        positionBuffer?.Release();
        positionBuffer = null;
    }
}
