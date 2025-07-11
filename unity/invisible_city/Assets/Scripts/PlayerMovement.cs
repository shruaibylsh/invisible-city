using UnityEngine;
using UnityEngine.InputSystem;  // Make sure you have the Input System package installed

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;    // forward speed (m/s)
    public float turnSpeed = 120f;  // degrees per second

    private Keyboard kb;

    void Awake()
    {
        kb = Keyboard.current;
        if (kb == null)
            Debug.LogError("No keyboard connected. Check that the Input System is enabled and you have a Keyboard device.");
    }

    void Update()
    {
        if (kb == null) return;

        // 1) Move forward with W or UpArrow
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }

        // 2) Rotate left/right with A/D or Left/Right Arrows
        float turn = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            turn = -1f;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            turn = 1f;

        if (turn != 0f)
        {
            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
        }
    }
}
