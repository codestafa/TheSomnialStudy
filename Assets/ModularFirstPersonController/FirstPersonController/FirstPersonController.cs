using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // NEW system

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    #region Input Actions
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;
    public InputActionReference zoomAction;
    #endregion

    #region Camera Movement Variables
    public Camera playerCamera;
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;
    #endregion

    #region Zoom
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;
    private bool isZoomed = false;
    #endregion

    #region Movement
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;
    private bool isWalking = false;

    #region Sprint
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;
    #endregion

    #region Jump
    public bool enableJump = true;
    public float jumpPower = 5f;
    private bool isGrounded = false;
    #endregion

    #region Crouch
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;
    private bool isCrouched = false;
    private Vector3 originalScale;
    #endregion
    #endregion

    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);
    private Vector3 jointOriginalPos;
    private float timer = 0;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        crosshairObject = GetComponentInChildren<Image>();
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
        zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
        zoomAction.action.Disable();
    }

    void Start()
    {
        // OnEnable(); 
        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;

        if (crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }

sprintBarCG = GetComponentInChildren<CanvasGroup>();

if (useSprintBar && sprintBarBG != null && sprintBar != null)
{
    sprintBarBG.gameObject.SetActive(true);
    sprintBar.gameObject.SetActive(true);

    sprintBarWidth = Screen.width * sprintBarWidthPercent;
    sprintBarHeight = Screen.height * sprintBarHeightPercent;

    sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
    sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

    if (hideBarWhenFull) sprintBarCG.alpha = 0;
}
else
{
    if (sprintBarBG != null) sprintBarBG.gameObject.SetActive(false);
    if (sprintBar != null) sprintBar.gameObject.SetActive(false);
}

    }

    private void Update()
    {
        HandleLook();
        HandleZoom();
        HandleSprint();
        HandleJump();
        HandleCrouch();
        CheckGround();
        if (enableHeadBob) HeadBob();
    }

    private void FixedUpdate()
    {
        if (playerCanMove) HandleMovement();
    }

    #region Camera
    void HandleLook()
    {
        if (!cameraCanMove) return;
        Vector2 look = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;
        yaw += look.x;
        pitch += invertCamera ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        transform.localEulerAngles = new Vector3(0, yaw, 0);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }
    #endregion

    #region Zoom
    void HandleZoom()
    {
        if (!enableZoom || isSprinting) return;
        if (!holdToZoom)
        {
            if (zoomAction.action.WasPressedThisFrame())
                isZoomed = !isZoomed;
        }
        else
        {
            isZoomed = zoomAction.action.IsPressed();
        }

        float targetFOV = isZoomed ? zoomFOV : fov;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomStepTime * Time.deltaTime);
    }
    #endregion

    #region Sprint
    void HandleSprint()
    {
        if (!enableSprint) return;

        if (isSprinting)
        {
            isZoomed = false;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

            if (!unlimitedSprint)
            {
                sprintRemaining -= Time.deltaTime;
                if (sprintRemaining <= 0)
                {
                    isSprinting = false;
                    isSprintCooldown = true;
                }
            }
        }
        else
        {
            sprintRemaining = Mathf.Clamp(sprintRemaining + Time.deltaTime, 0, sprintDuration);
        }

        if (isSprintCooldown)
        {
            sprintCooldown -= Time.deltaTime;
            if (sprintCooldown <= 0) isSprintCooldown = false;
        }
        else
        {
            sprintCooldown = sprintCooldownReset;
        }

        if (useSprintBar && !unlimitedSprint)
        {
            float sprintPercent = sprintRemaining / sprintDuration;
            sprintBar.transform.localScale = new Vector3(sprintPercent, 1f, 1f);
        }
    }
    #endregion

    #region Jump
    void HandleJump()
    {
        if (enableJump && jumpAction.action.WasPressedThisFrame() && isGrounded)
            Jump();
    }

    void Jump()
    {
        rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
        isGrounded = false;
        if (isCrouched && !holdToCrouch) Crouch();
    }
    #endregion

    #region Crouch
    void HandleCrouch()
    {
        if (!enableCrouch) return;

        if (!holdToCrouch && crouchAction.action.WasPressedThisFrame())
            Crouch();
        else if (holdToCrouch)
        {
            if (crouchAction.action.WasPressedThisFrame())
            {
                isCrouched = false;
                Crouch();
            }
            else if (crouchAction.action.WasReleasedThisFrame())
            {
                isCrouched = true;
                Crouch();
            }
        }
    }

    void Crouch()
    {
        if (isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;
            isCrouched = false;
        }
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            isCrouched = true;
        }
    }
    #endregion

    #region Movement
    void HandleMovement()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 targetVelocity = new Vector3(input.x, 0, input.y);

        isWalking = (targetVelocity.x != 0 || targetVelocity.z != 0) && isGrounded;

        if (enableSprint && sprintAction.action.IsPressed() && sprintRemaining > 0f && !isSprintCooldown)
        {
            targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;
            isSprinting = true;
        }
        else
        {
            targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;
            isSprinting = false;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 velocityChange = (targetVelocity - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
    #endregion

    #region Ground Check
    void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        isGrounded = Physics.Raycast(origin, Vector3.down, 0.75f);
    }
    #endregion

    #region Head Bob
    private void HeadBob()
    {
        if (isWalking)
        {
            if (isSprinting) timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            else if (isCrouched) timer += Time.deltaTime * (bobSpeed * speedReduction);
            else timer += Time.deltaTime * bobSpeed;

            joint.localPosition = new Vector3(
                jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x,
                jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y,
                jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            timer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
        }
    }
    #endregion
}
