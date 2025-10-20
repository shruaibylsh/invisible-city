using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;

[RequireComponent(typeof(Animator))]
public class DwellerAgent : Agent
{
    [Header("Spawn & Environment")]
    public MeshCollider spawnAreaMesh;
    public float spawnSampleRadius = 50f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float turnSpeed = 20f;
    public float actionDurationMin = 0.5f;
    public float actionDurationMax = 2.0f;


    private Animator animator;
    private float actionTimer = 0f;
    private int currentAction = 0;
    private bool turningLastAction = false;

    private int previousAction = -1;
    private int secondPreviousAction = -1;

    private List<Transform> homeBuildings = new List<Transform>();
    private Vector3 origin = Vector3.zero;
    private Vector3 spawnPoint;

    public override void Initialize()
    {
        animator = GetComponent<Animator>();
    }

    public override void OnEpisodeBegin()
    {
        spawnPoint = FindRandomPointOnMesh();
        if (NavMesh.SamplePosition(spawnPoint, out var hit, 3f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning("Failed to find valid NavMesh point, falling back to (0,0,0)");
            transform.position = Vector3.zero;
            spawnPoint = Vector3.zero;
        }

        transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        FindClosestHomeBuildings();
        actionTimer = 0f;
        currentAction = 0;
        turningLastAction = false;
        previousAction = -1;
        secondPreviousAction = -1;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var home in homeBuildings)
        {
            Vector3 relativePos = transform.InverseTransformPoint(home.position);
            sensor.AddObservation(relativePos / 100f);
        }

        sensor.AddObservation(transform.position / 150f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        if (actionTimer > 0f)
        {
            actionTimer -= Time.deltaTime;
            PerformCurrentAction();
            return;
        }

        int action = actions.DiscreteActions[0];
        bool isTurning = (action == 2 || action == 3);
        if (turningLastAction && isTurning)
            AddReward(-0.05f);
        turningLastAction = isTurning;

        // Repetition penalty
        if (action == previousAction && previousAction == secondPreviousAction)
            AddReward(-0.05f);

        secondPreviousAction = previousAction;
        previousAction = action;

        currentAction = action;
        actionTimer = action == 0 || action == 1 ? Random.Range(1.5f, 3f) : Random.Range(0.5f, 2f);
        PerformCurrentAction();

        if (!NavMesh.SamplePosition(transform.position, out var _, 1f, NavMesh.AllAreas))
{
    AddReward(-1.0f);
    EndEpisode();
}


        foreach (var home in homeBuildings)
        {
            float dist = Vector3.Distance(transform.position, home.position);
            if (dist < 3f)
            {
                AddReward(0.05f * Time.deltaTime);
                Vector3 toHome = (home.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, toHome);
                if (dot > 0.7f)
                    AddReward(0.03f * Time.deltaTime);
                else
                    AddReward(-0.01f * Time.deltaTime);
            }
        }

        if (Vector3.Distance(transform.position, origin) > 140f)
        {
            AddReward(-1f * Time.deltaTime);
            EndEpisode();
        }

        if (Vector3.Distance(transform.position, spawnPoint) > 50f)
                AddReward(-0.05f * Time.deltaTime);
    }

    private void PerformCurrentAction()
    {
        animator.SetInteger("ActionState", currentAction);
        switch (currentAction)
        {
            case 1: transform.position += transform.forward * moveSpeed * Time.deltaTime; break;
            case 2: transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime); break;
            case 3: transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime); break;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;
        if (kb == null) return;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) discrete[0] = 1;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) discrete[0] = 2;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) discrete[0] = 3;
    }

    private Vector3 FindRandomPointOnMesh()
    {
        if (spawnAreaMesh == null || spawnAreaMesh.sharedMesh == null)
            return Vector3.zero;

        Mesh mesh = spawnAreaMesh.sharedMesh;
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            int index = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = verts[tris[index]];
            Vector3 v1 = verts[tris[index + 1]];
            Vector3 v2 = verts[tris[index + 2]];

            float a = Random.value;
            float b = Random.value * (1 - a);
            float c = 1 - a - b;
            Vector3 localPoint = a * v0 + b * v1 + c * v2;
            Vector3 worldPoint = spawnAreaMesh.transform.TransformPoint(localPoint);

            Vector2 flatXZ = new Vector2(worldPoint.x, worldPoint.z);
            if (flatXZ.magnitude > spawnSampleRadius) continue;

            if (NavMesh.SamplePosition(worldPoint, out var hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        Debug.LogWarning("[DwellerAgent] Failed to sample spawn location within radius.");
        return Vector3.zero;
    }

    private void FindClosestHomeBuildings()
    {
        var allMeshes = GameObject.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
        var candidates = new List<(Transform, float)>();

        foreach (var m in allMeshes)
        {
            if (m.sharedMesh == null || !m.CompareTag("Building")) continue;
            Vector3 worldCenter = m.transform.TransformPoint(m.sharedMesh.bounds.center);
            float dist = Vector3.Distance(transform.position, worldCenter);
            candidates.Add((m.transform, dist));
        }

        homeBuildings = candidates
            .OrderBy(pair => pair.Item2)
            .Take(3)
            .Select(pair => pair.Item1)
            .ToList();

        Debug.Log($"[DwellerAgent] Spawned at {transform.position}. Assigned home buildings:");
        foreach (var home in homeBuildings)
            Debug.Log($" - {home.name} at {home.position}");
    }
}
