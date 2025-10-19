using UnityEngine;
using Unity.MLAgents;

[RequireComponent(typeof(Animator))]
public class AutoForwardHumanMovement : MonoBehaviour
{
    public float moveSpeed = 2f;

    private Animator animator;
    private Camera agentCamera;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agentCamera = GetComponentInChildren<Camera>();

        // Optional: disable ML-Agent if attached
        var mlAgent = GetComponent<Agent>();
        if (mlAgent != null) mlAgent.enabled = false;
    }

    void Update()
    {
        if (agentCamera == null || !agentCamera.enabled) return;

        // Move forward
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // Set walking animation
        animator.SetInteger("ActionState", 1); // 1 = Walk
    }
}
