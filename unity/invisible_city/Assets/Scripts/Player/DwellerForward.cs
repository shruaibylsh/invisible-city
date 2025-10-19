using UnityEngine;
using Unity.MLAgents;

[RequireComponent(typeof(Animator))]
public class AutoForwardDelay : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float delayBeforeMove = 18f;

    private Animator animator;
    private Camera agentCamera;
    private float timer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agentCamera = GetComponentInChildren<Camera>();

        var mlAgent = GetComponent<Agent>();
        if (mlAgent != null) mlAgent.enabled = false;
    }

    void Update()
    {
        if (agentCamera == null || !agentCamera.enabled) return;

        timer += Time.deltaTime;

        if (timer >= delayBeforeMove)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            animator.SetInteger("ActionState", 1); // Walk
        }
        else
        {
            animator.SetInteger("ActionState", 0); // Idle
        }
    }
}
