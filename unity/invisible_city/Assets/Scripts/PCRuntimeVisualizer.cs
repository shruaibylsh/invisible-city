using UnityEngine;

[ExecuteInEditMode]
public class PCRuntimeVisualizer : MonoBehaviour
{
    [Header("Data & Limits")]
    [SerializeField] private PointCloudData pointCloudData;
    [SerializeField, Min(1)]  private int maxPoints    = 1000;
    [SerializeField, Min(0f)] private float sphereRadius = 0.02f;
    [SerializeField]           private Color gizmoColor = Color.yellow;

    [Header("Runtime Visualization")]
    [SerializeField] private Material runtimeMaterial;

    // Parent for runtime markers
    private GameObject markersParent;

    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;   // skip gizmos in play if you prefer
        if (pointCloudData?.positions == null) return;

        Gizmos.color = gizmoColor;
        int count = Mathf.Min(maxPoints, pointCloudData.positions.Length);
        for (int i = 0; i < count; i++)
        {
            Vector3 world = transform.TransformPoint(pointCloudData.positions[i]);
            Gizmos.DrawSphere(world, sphereRadius);
        }
    }

    void Start()
    {
        // only do this in play mode
        if (!Application.isPlaying || pointCloudData?.positions == null)
            return;

        // clean up any old parent
        if (markersParent != null) Destroy(markersParent);
        markersParent = new GameObject("PointCloudMarkers");
        markersParent.transform.SetParent(transform, false);

        int count = Mathf.Min(maxPoints, pointCloudData.positions.Length);
        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = pointCloudData.positions[i];

            // create a sphere primitive
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"pt{i}";
            sphere.transform.SetParent(markersParent.transform, false);
            sphere.transform.localPosition = localPos;
            sphere.transform.localScale = Vector3.one * sphereRadius * 2f;

            // apply material if provided
            if (runtimeMaterial != null)
                sphere.GetComponent<MeshRenderer>().material = runtimeMaterial;

            // remove collider so it doesn’t interfere
            DestroyImmediate(sphere.GetComponent<Collider>());
        }
    }

    void OnDestroy()
    {
        if (markersParent != null)
            DestroyImmediate(markersParent);
    }
}
