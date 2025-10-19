using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
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

    // "Current agent" = the one whose POV camera is enabled
    bool IsCurrentAgent() => agentCamera != null && agentCamera.enabled;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Global hotkeys (apply regardless of which agent is current)
        if (kb.escapeKey.wasPressedThisFrame)
        {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }

        if (kb.tabKey.wasPressedThisFrame)
        {
            // Ensure unpaused before reload
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }


        // Only the current agent responds to M / R
        if (IsCurrentAgent())
        {
            if (kb.mKey.wasPressedThisFrame) SetMode(true);   // Manual
            if (kb.rKey.wasPressedThisFrame) SetMode(false);  // Agent/Auto
        }
        else
        {
            // Non-current agents should stay in agent mode
            if (manualControl) SetMode(false);
        }

        // Control logic
        if (manualControl)
        {
            ManualControl();
            if (mlAgent != null) mlAgent.enabled = false;
        }
        else
        {
            if (mlAgent != null) mlAgent.enabled = true;
            // ML-Agent drives this character
        }
    }

    void SetMode(bool manual)
    {
        manualControl = manual;
        if (mlAgent != null) mlAgent.enabled = !manual;
        // (Optional) reset animator state on switch
        if (!manual) animator.SetInteger("ActionState", 0); // Idle
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