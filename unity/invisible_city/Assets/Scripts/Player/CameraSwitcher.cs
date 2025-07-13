using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    public Camera cam1, cam2, cam3;

    void Start()
    {
        cam1.enabled = true;
        cam2.enabled = cam3.enabled = false;
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
    }
}
