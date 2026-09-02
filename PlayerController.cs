using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CollideAndSlide))]
public class PlayerController : MonoBehaviour
{
    [Header("Input File")]
    public InputActionAsset inputAsset;
    
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    [Header("References")]
    public Transform cameraTransform;
    public CollideAndSlide motor;

    [Header("Movement Settings")]
    public float lookSens = 15f;
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    public float acceleration = 20f;
    public float airAcceleration = 5f;

    [Header("Gravity & Jump")]
    public float gravity = 30f;
    public float jumpHeight = 2.5f;
    public float coyoteTime = 0.15f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private Vector3 velNow;
    private float velVert;
    private float coyoteTimer;

    private void Awake()
    {
        inputAsset = Instantiate(inputAsset);
        //Find the action map
        var playerMap = inputAsset.FindActionMap("Player");
        //find all the actions
        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");
        jumpAction = playerMap.FindAction("Jump");
        sprintAction = playerMap.FindAction("Sprint");

        //ensure motor is assigned
        if (!motor) motor = GetComponent<CollideAndSlide>();
    }

    private void OnEnable() 
    { 
        if (moveAction != null)
        {
        //endable inputs
        moveAction.Enable(); 
        lookAction.Enable(); 
        jumpAction.Enable(); 
        sprintAction.Enable();
        }
    }
    private void OnDisable()
    {
        if (moveAction != null)
        {
        //disable inputs
        moveAction.Disable(); 
        lookAction.Disable(); 
        jumpAction.Disable(); 
        sprintAction.Disable();
        }
    }

    void Start()
    {
        //Lock the cursor position & make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //Read Inputs
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();

        if (Time.timeSinceLevelLoad > 0.1f)
        {
            LookF();
        }

        MoveF();
    }

    void LookF()
    {
        //Get mouse input, scale by sensitivity and deltaTime
        float mouseX = lookInput.x * lookSens * Time.deltaTime;
        float mouseY = lookInput.y * lookSens * Time.deltaTime;

        xRotation -= mouseY; //rotate up/down
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); //limit looking too far up/down
        
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); //apply rotation to camera
        transform.Rotate(Vector3.up * mouseX); //rotate player object left/right
    }

    void MoveF()
    {
        if (motor.isGrounded)
        {
            coyoteTimer = coyoteTime; //reset coyote timer
            velVert = -2f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime; //count down coyote timer
            velVert -= gravity * Time.deltaTime; //apply gravity
        }

        //jump
        if (jumpAction.triggered && coyoteTimer > 0)
        {
            velVert = Mathf.Sqrt(jumpHeight * 2f * gravity);
            coyoteTimer = 0;
        }

        //horizontal movement
        //determine direction based on camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        //flatten vectors
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDir = (forward * moveInput.y + right * moveInput.x).normalized; //calculate movement direction

        //determine speed
        float targetSpeed = sprintAction.IsPressed() ? sprintSpeed: moveSpeed;
        if (moveInput.magnitude < 0.1f) targetSpeed = 0;

        //Determine acceleration
        float accelNow = motor.isGrounded ? acceleration : airAcceleration;

        //calculate velocity
        Vector3 targetVel = targetDir * targetSpeed;

        //interpolate velocity to target
        velNow = Vector3.MoveTowards(velNow, targetVel, accelNow * Time.deltaTime);

        //move
        //combine horizontal & vertical
        Vector3 gravityVector = Vector3.up * velVert * Time.deltaTime;
        Vector3 moveVector = velNow * Time.deltaTime; // Convert velocity to distance

        motor.Move(moveVector, gravityVector);
    }
}