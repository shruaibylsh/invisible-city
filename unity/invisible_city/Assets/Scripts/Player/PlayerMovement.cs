using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;

[RequireComponent(typeof(Animator))]
public class HumanMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float turnSpeed = 90f;

    private Animator animator;
    private Camera agentCamera;
    private Agent mlAgent;

    private bool manualControl = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agentCamera = GetComponentInChildren<Camera>();
        mlAgent = GetComponent<Agent>();
    }

    void Update()
    {
        // Mode switching
        if (Keyboard.current.mKey.wasPressedThisFrame)
            manualControl = true;

        if (Keyboard.current.rKey.wasPressedThisFrame)
            manualControl = false;

        // Control logic
        if (manualControl)
        {
            ManualControl();
            if (mlAgent != null) mlAgent.enabled = false;
        }
        else
        {
            if (mlAgent != null) mlAgent.enabled = true;
            // Agent will be driven by ML logic
        }
    }

    void ManualControl()
    {
        if (agentCamera == null || !agentCamera.enabled) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        int state = 0; // Idle

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
        {
            state = 1; // Walk
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
        {
            state = 2; // Turn Left
            transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        }
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
        {
            state = 3; // Turn Right
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }

        animator.SetInteger("ActionState", state);
    }
}
