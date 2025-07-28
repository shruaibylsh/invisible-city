using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
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
    private NavMeshAgent navAgent;
    private NavMeshPath navPath;

    private float actionTimer = 0f;
    private int currentAction = 0;
    private int previousAction = -1;
    private int secondPreviousAction = -1;

    private Vector3 spawnPoint;
    private Vector3 origin = Vector3.zero;

    private Transform[] targetBuildings = new Transform[3];
    private int currentTargetIndex = 0;

    private float turnCooldownTimer = 0f;
    private float turnCooldownDuration = 3f;

    public override void Initialize()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
        navPath = new NavMeshPath();
    }

    public override void OnEpisodeBegin()
    {
        spawnPoint = FindRandomPointOnMesh();
        if (NavMesh.SamplePosition(spawnPoint, out var hit, 3f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
        {
            transform.position = Vector3.zero;
            spawnPoint = Vector3.zero;
        }

        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        AssignRandomTargetBuildings();
        currentTargetIndex = 0;
        navAgent.SetDestination(targetBuildings[currentTargetIndex].position);

        actionTimer = 0f;
        currentAction = 0;
        previousAction = -1;
        secondPreviousAction = -1;
        turnCooldownTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var t in targetBuildings)
        {
            Vector3 local = transform.InverseTransformPoint(t.position);
            sensor.AddObservation(local / 100f);
        }

        sensor.AddObservation(transform.position / 150f);

        Transform target = targetBuildings[currentTargetIndex];
        if (target != null &&
            NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath) &&
            navPath.status == NavMeshPathStatus.PathComplete &&
            navPath.corners.Length > 1)
        {
            Vector3 toNext = (navPath.corners[1] - transform.position).normalized;
            sensor.AddObservation(transform.InverseTransformDirection(toNext));
            float pathLen = 0f;
            for (int i = 1; i < navPath.corners.Length; i++)
                pathLen += Vector3.Distance(navPath.corners[i - 1], navPath.corners[i]);
            sensor.AddObservation(pathLen / 100f);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        turnCooldownTimer -= Time.deltaTime;

        if (currentAction == 1 &&
            Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, detectDistance, buildingLayerMask))
        {
            if (hit.collider.CompareTag("Building"))
            {
                int turn = Random.value > 0.5f ? 2 : 3;
                currentAction = turn;
                actionTimer = Random.Range(1.0f, 1.5f);
                animator.SetInteger("ActionState", currentAction);
                turnCooldownTimer = turnCooldownDuration;
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

        bool isTurning = (action == 2 || action == 3);
        if (isTurning && turnCooldownTimer > 0f)
        {
            action = 1;
        }

        if (action == 0 && action == previousAction && previousAction == secondPreviousAction)
        {
            AddReward(-0.05f);
        }

        if (isTurning)
        {
            AddReward(-0.03f);
            turnCooldownTimer = turnCooldownDuration;
        }

        secondPreviousAction = previousAction;
        previousAction = action;

        currentAction = action;
        actionTimer = (action == 0 || action == 1) ? Random.Range(1.5f, 3f) : Random.Range(0.5f, 2f);
        PerformCurrentAction();

        if (!NavMesh.SamplePosition(transform.position, out var _, 1f, NavMesh.AllAreas))
            EndEpisode();

        if (roadMesh != null && roadMesh.bounds.Contains(transform.position))
        {
            AddReward(0.25f * Time.deltaTime);
            if (currentAction == 1)
                AddReward(0.15f * Time.deltaTime);
        }

        Transform target = targetBuildings[currentTargetIndex];
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist < 3f)
        {
            AddReward(0.3f);
            Vector3 toTarget = (target.position - transform.position).normalized;
            if (Vector3.Dot(transform.forward, toTarget) > 0.7f)
                AddReward(0.1f);

            currentTargetIndex = (currentTargetIndex + 1) % 3;
            navAgent.SetDestination(targetBuildings[currentTargetIndex].position);
        }

        if (Vector3.Distance(transform.position, Vector3.zero) > 140f)
        {
            AddReward(-10f);
            EndEpisode();
        }
    }

    private void PerformCurrentAction()
    {
        animator.SetInteger("ActionState", currentAction);

        if (currentAction == 1 && navAgent.hasPath)
        {
            Vector3 direction = navAgent.desiredVelocity.normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        else if (currentAction == 2)
        {
            transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        }
        else if (currentAction == 3)
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }

        navAgent.nextPosition = transform.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        float r = Random.value;
        if (r < 0.5f)      discrete[0] = 1;
        else if (r < 0.7f) discrete[0] = 2;
        else if (r < 0.9f) discrete[0] = 3;
        else               discrete[0] = 0;
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

            Vector3 above = world + Vector3.up * 5f;
            if (Physics.Raycast(above, Vector3.down, out RaycastHit hit, 10f))
            {
                if (hit.collider == roadMesh && NavMesh.SamplePosition(hit.point, out var navHit, 1f, NavMesh.AllAreas))
                    return navHit.position;
            }
        }

        return Vector3.zero;
    }

    private void AssignRandomTargetBuildings()
    {
        var all = GameObject.FindGameObjectsWithTag("Building");
        if (all.Length < 3) return;

        targetBuildings = all.OrderBy(x => Random.value).Take(3).Select(x => x.transform).ToArray();
    }
}
