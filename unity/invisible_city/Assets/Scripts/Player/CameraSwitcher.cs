using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    public Camera cam1, cam2, cam3;
    public PointCloudVisibilityManager memoryManager;  // Reference to the memory system

    void Start()
    {
        Activate(cam1);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.digit1Key.wasPressedThisFrame) Activate(cam1);
        else if (kb.digit2Key.wasPressedThisFrame) Activate(cam2);
        else if (kb.digit3Key.wasPressedThisFrame) Activate(cam3);
    }

    void Activate(Camera active)
    {
        cam1.enabled = (active == cam1);
        cam2.enabled = (active == cam2);
        cam3.enabled = (active == cam3);

        if (memoryManager != null)
            memoryManager.activeCamera = active;
    }
}
