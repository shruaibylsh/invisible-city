using UnityEngine;

[CreateAssetMenu(menuName = "Point Cloud/Data", fileName = "PointCloudData")]
public class PointCloudData : ScriptableObject
{
    public Vector3[] positions;
    public Vector3[] normals;
}
