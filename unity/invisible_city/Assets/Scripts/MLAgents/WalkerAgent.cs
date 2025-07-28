using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;

[RequireComponent(typeof(Animator))]
public class WalkerAgent : Agent
{
    [Header("Spawn & Environment")]
    public MeshCollider roadMesh;
    public float spawnSampleRadius = 50f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float turnSpeed = 20f;
    public float actionDurationMin = 0.5f;
    public float actionDurationMax = 2.0f;

    [Header("Obstacle Avoidance")]
    public float detectDistance = 2f;
    public LayerMask buildingLayerMask;

    private Animator animator;
    private float actionTimer = 0f;
    private int currentAction = 0;

    private Vector3 spawnPoint;
    private Vector3 origin = Vector3.zero;

    private int previousAction = -1;
    private int secondPreviousAction = -1;

    private Transform[] targetBuildings = new Transform[3];
    private int currentTargetIndex = 0;
    private HashSet<Transform> visitedTargets = new HashSet<Transform>();

    public override void Initialize()
    {
        animator = GetComponent<Animator>();
    }

    public override void OnEpisodeBegin()
    {
        spawnPoint = FindRandomPointOnMesh();
        if (NavMesh.SamplePosition(spawnPoint, out var hit, 3f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
        {
            Debug.LogWarning("Failed to find valid NavMesh point, falling back to (0,0,0)");
            transform.position = Vector3.zero;
            spawnPoint = Vector3.zero;
        }

        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        AssignRandomTargetBuildings();
        visitedTargets.Clear();

        actionTimer = 0f;
        currentAction = 0;
        previousAction = -1;
        secondPreviousAction = -1;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var t in targetBuildings)
        {
            Vector3 local = transform.InverseTransformPoint(t.position);
            sensor.AddObservation(local / 100f);
        }

        sensor.AddObservation(transform.position / 150f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Obstacle detection
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, detectDistance, buildingLayerMask))
        {
            if (hit.collider.CompareTag("Building"))
            {
                int turn = Random.value > 0.5f ? 2 : 3;
                currentAction = turn;
                actionTimer = Random.Range(1.0f, 1.5f);
                animator.SetInteger("ActionState", currentAction);
                PerformCurrentAction();
                return;
            }
        }

        if (actionTimer > 0f)
        {
            actionTimer -= Time.deltaTime;
            PerformCurrentAction();
            return;
        }

        int action = actions.DiscreteActions[0];

        // Only penalize repeating idle 3x
        if (action == 0 && action == previousAction && previousAction == secondPreviousAction)
            AddReward(-0.05f);

        secondPreviousAction = previousAction;
        previousAction = action;
        currentAction = action;
        actionTimer = (action == 0 || action == 1) ? Random.Range(1.5f, 3f) : Random.Range(0.5f, 2f);
        PerformCurrentAction();

        if (!NavMesh.SamplePosition(transform.position, out var _, 1f, NavMesh.AllAreas))
            EndEpisode();

        // Reward: standing on road mesh
        if (roadMesh != null && roadMesh.bounds.Contains(transform.position))
            AddReward(0.05f * Time.deltaTime);

        // Reward logic for target visitation
        Transform target = targetBuildings[currentTargetIndex];
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist < 3f)
        {
            if (!visitedTargets.Contains(target))
            {
                AddReward(0.3f);
                visitedTargets.Add(target);
            }
            else
            {
                AddReward(0.2f); // revisit bonus
            }

            // Encourage facing
            Vector3 toTarget = (target.position - transform.position).normalized;
            if (Vector3.Dot(transform.forward, toTarget) > 0.7f)
                AddReward(0.1f);

            // Cycle to next
            currentTargetIndex = (currentTargetIndex + 1) % 3;
        }

        // Penalize if stuck on the same target and visited already
        if (visitedTargets.Count < 3 && visitedTargets.Contains(targetBuildings[currentTargetIndex]))
            AddReward(-0.05f);

        if (Vector3.Distance(transform.position, origin) > 140f)
            AddReward(-0.1f * Time.deltaTime);

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
        if (roadMesh == null || roadMesh.sharedMesh == null)
            return Vector3.zero;

        Mesh mesh = roadMesh.sharedMesh;
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            int i = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            float a = Random.value;
            float b = Random.value * (1 - a);
            float c = 1 - a - b;
            Vector3 local = a * v0 + b * v1 + c * v2;
            Vector3 world = roadMesh.transform.TransformPoint(local);

            Vector2 flat = new Vector2(world.x, world.z);
            if (flat.magnitude > spawnSampleRadius) continue;

            if (NavMesh.SamplePosition(world, out var hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        Debug.LogWarning("[WalkerAgent] Failed to sample valid spawn.");
        return Vector3.zero;
    }

    private void AssignRandomTargetBuildings()
    {
        var all = GameObject.FindGameObjectsWithTag("Building");
        if (all.Length < 3)
        {
            Debug.LogWarning("Not enough buildings in scene.");
            return;
        }

        targetBuildings = all.OrderBy(x => Random.value).Take(3).Select(x => x.transform).ToArray();

        Debug.Log($"[WalkerAgent] Spawned at {transform.position}. Assigned targets:");
        foreach (var t in targetBuildings)
            Debug.Log($" - {t.name} at {t.position}");
    }
}
