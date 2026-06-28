using UnityEngine;
using UnityEngine.InputSystem;

public class Input : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerInputActions inputActions = new();
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += Move_performed;
        inputActions.Player.Look.performed += Look_performed;
        inputActions.Player.Sprint.performed += Sprint_performed;
        inputActions.Player.ZoomScroll.performed += ZoomScroll_performed;
        inputActions.Player.ZoomIn.performed += ZoomIn_performed;
        inputActions.Player.ZoomOut.performed += ZoomOut_performed;
    }

    private void ZoomOut_performed(InputAction.CallbackContext context)
    {
        CameraZoom.Instance.ZoomOut();
    }

    private void ZoomIn_performed(InputAction.CallbackContext context)
    {
        CameraZoom.Instance.ZoomIn();
    }

    private void ZoomScroll_performed(InputAction.CallbackContext context)
    {
        CameraZoom.Instance.OnZoomScroll(context);
    }

    private void Sprint_performed(InputAction.CallbackContext context)
    {
        PlayerController.Instance.OnSprint(context);
    }

    private void Look_performed(InputAction.CallbackContext context)
    {
        PlayerController.Instance.OnLook(context);
    }

    private void Move_performed(InputAction.CallbackContext context)
    {
        PlayerController.Instance.OnMove(context);
    }
}
