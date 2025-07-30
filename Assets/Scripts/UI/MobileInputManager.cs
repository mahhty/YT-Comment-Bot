using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileInputManager : MonoBehaviour
{
    [Header("Virtual Joystick")]
    public GameObject joystickContainer;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float joystickRange = 50f;

    [Header("Camera Touch")]
    public float cameraSensitivity = 2f;
    public RectTransform cameraArea; // Area for camera touch input

    [Header("Action Buttons")]
    public Button jumpButton;
    public Button runButton;
    public Button interactButton;
    public Button inventoryButton;
    public Button craftingButton;

    [Header("UI Elements")]
    public Canvas gameCanvas;

    // Input values
    private Vector2 joystickInput;
    private Vector2 cameraDelta;
    private bool isRunning;
    private bool jumpPressed;
    private bool interactPressed;

    // Touch tracking
    private int joystickTouchId = -1;
    private int cameraTouchId = -1;
    private Vector2 lastCameraTouchPosition;

    void Start()
    {
        SetupButtons();
        SetupJoystick();
    }

    void Update()
    {
        HandleTouchInput();
        ResetInputs();
    }

    void SetupButtons()
    {
        if (jumpButton != null)
            jumpButton.onClick.AddListener(() => jumpPressed = true);

        if (runButton != null)
        {
            // Add event triggers for run button (hold to run)
            EventTrigger trigger = runButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = runButton.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { isRunning = true; });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { isRunning = false; });
            trigger.triggers.Add(pointerUp);
        }

        if (interactButton != null)
            interactButton.onClick.AddListener(() => interactPressed = true);

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(() => ToggleInventory());

        if (craftingButton != null)
            craftingButton.onClick.AddListener(() => ToggleCrafting());
    }

    void SetupJoystick()
    {
        if (joystickContainer != null)
        {
            joystickContainer.SetActive(true);
        }
    }

    void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 touchPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch, touchPosition);
                    break;

                case TouchPhase.Moved:
                    HandleTouchMoved(touch, touchPosition);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded(touch);
                    break;
            }
        }

        // Handle mouse input for testing in editor
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            HandleTouchBegan(new Touch { fingerId = 0 }, mousePos);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            HandleTouchMoved(new Touch { fingerId = 0 }, mousePos);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleTouchEnded(new Touch { fingerId = 0 });
        }
    }

    void HandleTouchBegan(Touch touch, Vector2 touchPosition)
    {
        // Check if touch is on joystick area
        if (IsPointInJoystickArea(touchPosition) && joystickTouchId == -1)
        {
            joystickTouchId = touch.fingerId;
            UpdateJoystick(touchPosition);
        }
        // Check if touch is on camera area
        else if (IsPointInCameraArea(touchPosition) && cameraTouchId == -1)
        {
            cameraTouchId = touch.fingerId;
            lastCameraTouchPosition = touchPosition;
        }
    }

    void HandleTouchMoved(Touch touch, Vector2 touchPosition)
    {
        if (touch.fingerId == joystickTouchId)
        {
            UpdateJoystick(touchPosition);
        }
        else if (touch.fingerId == cameraTouchId)
        {
            UpdateCamera(touchPosition);
        }
    }

    void HandleTouchEnded(Touch touch)
    {
        if (touch.fingerId == joystickTouchId)
        {
            joystickTouchId = -1;
            ResetJoystick();
        }
        else if (touch.fingerId == cameraTouchId)
        {
            cameraTouchId = -1;
            cameraDelta = Vector2.zero;
        }
    }

    bool IsPointInJoystickArea(Vector2 screenPoint)
    {
        if (joystickBackground == null) return false;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, screenPoint, gameCanvas.worldCamera, out localPoint);
        
        return joystickBackground.rect.Contains(localPoint);
    }

    bool IsPointInCameraArea(Vector2 screenPoint)
    {
        if (cameraArea == null) return true; // If no specific area defined, use entire screen
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cameraArea, screenPoint, gameCanvas.worldCamera, out localPoint);
        
        return cameraArea.rect.Contains(localPoint);
    }

    void UpdateJoystick(Vector2 touchPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, touchPosition, gameCanvas.worldCamera, out localPoint);

        Vector2 offset = Vector2.ClampMagnitude(localPoint, joystickRange);
        joystickHandle.localPosition = offset;

        joystickInput = offset / joystickRange;
    }

    void ResetJoystick()
    {
        joystickHandle.localPosition = Vector2.zero;
        joystickInput = Vector2.zero;
    }

    void UpdateCamera(Vector2 touchPosition)
    {
        Vector2 deltaPosition = touchPosition - lastCameraTouchPosition;
        cameraDelta = deltaPosition * cameraSensitivity * Time.deltaTime;
        lastCameraTouchPosition = touchPosition;
    }

    void ResetInputs()
    {
        // Reset one-frame inputs
        jumpPressed = false;
        interactPressed = false;
        
        // Camera delta fades over time if no input
        if (cameraTouchId == -1)
        {
            cameraDelta = Vector2.Lerp(cameraDelta, Vector2.zero, Time.deltaTime * 10f);
        }
    }

    // Public input getters
    public float GetHorizontalInput()
    {
        return joystickInput.x;
    }

    public float GetVerticalInput()
    {
        return joystickInput.y;
    }

    public Vector2 GetCameraDelta()
    {
        return cameraDelta;
    }

    public bool GetJumpInput()
    {
        return jumpPressed;
    }

    public bool GetInteractInput()
    {
        return interactPressed;
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    // UI Actions
    void ToggleInventory()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.ToggleInventory();
        }
    }

    void ToggleCrafting()
    {
        CraftingManager craftingManager = FindObjectOfType<CraftingManager>();
        if (craftingManager != null)
        {
            craftingManager.ToggleCrafting();
        }
    }
}