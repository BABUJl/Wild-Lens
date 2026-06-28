using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person controller optimized for mobile photography gameplay
/// Supports touch controls with adjustable sensitivity
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float minVerticalAngle = -60f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Mobile Touch Settings")]
    [SerializeField] private float touchSensitivity = 0.5f;
    [SerializeField] private bool invertY = false;

    // Components
    private CharacterController controller;

    // Movement state
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting;

    // Camera state
    private float cameraPitch = 0f;
    private Vector2 lookInput;

    private void Awake()
    {
        Instance = this;

        controller = GetComponent<CharacterController>();

        // Lock cursor for desktop testing (comment out for mobile build)
#if UNITY_EDITOR || UNITY_STANDALONE
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleCameraRotation();
        ApplyGravity();
    }

    #region Ground Check
    private void HandleGroundCheck()
    {
        // Sphere cast slightly below player to check if grounded
        isGrounded = Physics.CheckSphere(
            transform.position - new Vector3(0, controller.height / 2, 0),
            groundCheckDistance,
            groundMask
        );

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to stay grounded
        }
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        // Calculate movement direction relative to player facing
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Determine current speed
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Move the character
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Camera Rotation
    private void HandleCameraRotation()
    {
        // Horizontal rotation (rotate player body)
        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        // Vertical rotation (rotate camera)
        float verticalDelta = lookInput.y * lookSensitivity;
        if (invertY) verticalDelta = -verticalDelta;

        cameraPitch -= verticalDelta;
        cameraPitch = Mathf.Clamp(cameraPitch, minVerticalAngle, maxVerticalAngle);

        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }
    #endregion

    #region Input Callbacks (Unity Input System)

    // Called by Input System when move input changes
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Called by Input System when look input changes
    public void OnLook(InputAction.CallbackContext context)
    {
        Debug.Log("Look");
        Vector2 rawLookInput = context.ReadValue<Vector2>();

        // Apply touch sensitivity for mobile
#if UNITY_ANDROID || UNITY_IOS
        lookInput = rawLookInput * touchSensitivity;
#else
        lookInput = rawLookInput;
#endif
    }

    // Called by Input System when sprint input changes
    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    #endregion

    #region Public Methods for External Systems

    /// <summary>
    /// Enable/disable player movement (useful when taking photos)
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
        }
    }

    /// <summary>
    /// Get current movement speed for other systems
    /// </summary>
    public float GetCurrentSpeed()
    {
        return controller.velocity.magnitude;
    }

    /// <summary>
    /// Check if player is moving
    /// </summary>
    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }

    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Visualize ground check sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position - new Vector3(0, GetComponent<CharacterController>()?.height / 2 ?? 1f, 0),
            groundCheckDistance
        );
    }
    #endregion
}