using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayerMask = 1;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    [Header("Mobile Input")]
    public MobileInputManager mobileInput;
    public bool isMobileInput = true;

    private Rigidbody rb;
    private bool isGrounded;
    private float verticalRotation = 0f;
    private Vector3 moveDirection;
    private bool isRunning;

    // Animation
    private Animator animator;
    private readonly int SpeedHash = Animator.StringToHash("Speed");
    private readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int IsRunningHash = Animator.StringToHash("IsRunning");

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        // Lock cursor for PC builds, unlock for mobile
        if (!isMobileInput)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Initialize mobile input if not assigned
        if (mobileInput == null)
            mobileInput = FindObjectOfType<MobileInputManager>();
    }

    void Update()
    {
        HandleInput();
        HandleCamera();
        CheckGrounded();
        HandleMovement();
        UpdateAnimations();
    }

    void HandleInput()
    {
        if (isMobileInput && mobileInput != null)
        {
            // Mobile input
            moveDirection = new Vector3(mobileInput.GetHorizontalInput(), 0f, mobileInput.GetVerticalInput());
            isRunning = mobileInput.IsRunning();
            
            if (mobileInput.GetJumpInput() && isGrounded)
            {
                Jump();
            }
        }
        else
        {
            // PC input fallback
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveDirection = new Vector3(horizontal, 0f, vertical);
            isRunning = Input.GetKey(KeyCode.LeftShift);
            
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }
        }
    }

    void HandleCamera()
    {
        if (cameraTransform == null) return;

        float mouseX, mouseY;

        if (isMobileInput && mobileInput != null)
        {
            Vector2 cameraDelta = mobileInput.GetCameraDelta();
            mouseX = cameraDelta.x * mouseSensitivity * Time.deltaTime;
            mouseY = cameraDelta.y * mouseSensitivity * Time.deltaTime;
        }
        else
        {
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void CheckGrounded()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayerMask);
    }

    void HandleMovement()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            // Calculate movement relative to player rotation
            Vector3 worldMoveDirection = transform.TransformDirection(moveDirection);
            worldMoveDirection.y = 0f;

            float currentSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
            Vector3 targetVelocity = worldMoveDirection.normalized * currentSpeed;
            targetVelocity.y = rb.velocity.y; // Preserve vertical velocity

            rb.velocity = targetVelocity;
        }
        else
        {
            // Stop horizontal movement but preserve vertical velocity
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        float speed = moveDirection.magnitude;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetBool(IsRunningHash, isRunning && speed > 0.1f);
    }

    // Public methods for other systems
    public bool IsMoving()
    {
        return moveDirection.magnitude > 0.1f;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public float GetCurrentSpeed()
    {
        return rb.velocity.magnitude;
    }

    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
}