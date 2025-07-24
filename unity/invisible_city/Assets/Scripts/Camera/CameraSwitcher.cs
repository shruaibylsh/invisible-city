using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    public Camera cam1, cam2, cam3, cam4, cam5, cam6;
    public PointCloudVisibilityManager memoryManager;  // Reference to the memory system

    void Start()
    {
        Activate(cam1);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Activate(cam1);
        else if (kb.digit2Key.wasPressedThisFrame) Activate(cam2);
        else if (kb.digit3Key.wasPressedThisFrame) Activate(cam3);
        else if (kb.digit4Key.wasPressedThisFrame) Activate(cam4);
        else if (kb.digit5Key.wasPressedThisFrame) Activate(cam5);
        else if (kb.digit6Key.wasPressedThisFrame) Activate(cam6);
    }

    void Activate(Camera active)
    {
        cam1.enabled = (active == cam1);
        cam2.enabled = (active == cam2);
        cam3.enabled = (active == cam3);
        cam4.enabled = (active == cam4);
        cam5.enabled = (active == cam5);
        cam6.enabled = (active == cam6);
    }
}


