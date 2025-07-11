using UnityEngine;
using UnityEngine.InputSystem;

public class HumanMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;     // forward speed (m/s)
    public float turnSpeed = 120f;   // degrees per second

    private Camera agentCamera;

    void Awake()
    {
        // find the camera that’s parented under this agent
        agentCamera = GetComponentInChildren<Camera>();
        if (agentCamera == null)
            Debug.LogError($"No child Camera found on {name}!");
    }

    void Update()
    {
        // only accept input if this agent’s camera is the one rendering
        if (agentCamera == null || !agentCamera.enabled) 
            return;

        var kb = Keyboard.current;
        if (kb == null) 
            return;

        // forward
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // turn
        float turn = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) 
            turn = -1f;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) 
            turn = 1f;

        if (turn != 0f)
            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
    }
}
