using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMoveScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float pitch;
    private bool isLooking;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        UpdateLookState();
        HandleMouseLook();
        HandleMovement();
    }

    private void UpdateLookState()
    {
        if (Mouse.current == null)
            return;

        bool rightMouseHeld = Mouse.current.rightButton.isPressed;

        if (rightMouseHeld == isLooking)
            return;

        isLooking = rightMouseHeld;

        Cursor.visible = !isLooking;
        Cursor.lockState = isLooking ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void HandleMouseLook()
    {
        if (!isLooking || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        if (mouseDelta == Vector2.zero)
            return;

        pitch -= mouseDelta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float yaw = transform.eulerAngles.y + mouseDelta.x;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector3 moveDirection = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) moveDirection += transform.forward;
        if (Keyboard.current.sKey.isPressed) moveDirection -= transform.forward;
        if (Keyboard.current.dKey.isPressed) moveDirection += transform.right;
        if (Keyboard.current.aKey.isPressed) moveDirection -= transform.right;

        if (moveDirection == Vector3.zero)
            return;

        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }
}
