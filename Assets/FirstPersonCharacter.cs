using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(KinematicCharacterController))]
public class FirstPersonCharacter : MonoBehaviour {
    
    public Transform spawnPos;
    public Transform cam;
    public Text text;
    public float sens = 0.2f;

    public KinematicCharacterController controller { get; private set; }
    InputActions input;

    Vector2 moveDir;
    Vector2 lookDir;
    float lookX, lookY = 0;
    bool jump;

    Vector3 velocity;

    void Awake() {
        #region input
        input = new InputActions();
        input.Player.Move.performed += ctx => {
            moveDir = ctx.ReadValue<Vector2>();
        };
        input.Player.Move.canceled += ctx => {
            moveDir = Vector2.zero;
        };
        input.Player.Look.performed += ctx => {
            lookDir = ctx.ReadValue<Vector2>();
        };
        input.Player.Look.canceled += ctx => {
            lookDir = Vector2.zero;
        };
        input.Player.Jump.performed += ctx => {
            jump = true;
        };
        input.Player.Jump.canceled += ctx => {
            jump = false;
        };
        input.Player.Reset.performed += ctx => {
            transform.position = spawnPos.position;
        };
        input.Player.Sprint.performed += ctx => {
            controller.isSprinting = true;
        };
        input.Player.Sprint.canceled += ctx => {
            controller.isSprinting = false;
        };
        input.Player.Crouch.performed += ctx => {
            controller.shouldCrouch = true;
        };
        input.Player.Crouch.canceled += ctx => {
            controller.shouldCrouch = false;
        };
        #endregion

        controller = GetComponent<KinematicCharacterController>();
    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        UpdateLookDir();
        setDebugText();
    }

    void FixedUpdate() {
        Vector3 direction = (cam.forward * moveDir.y + cam.right * moveDir.x);
        direction.y = 0;
        direction.Normalize();
        Vector2 dir = new Vector2(direction.x, direction.z);
        // controller.Move(new Vector2(-1, 0), jump);
        velocity = controller.Move(dir, jump);
    }

    void UpdateLookDir() {
        Vector2 look = lookDir * sens;
        lookX -= look.y;
        lookX = Mathf.Clamp(lookX, -89, 89);
        lookY += look.x;
        cam.localEulerAngles = Vector3.right * lookX + Vector3.up * lookY;
    }

    void setDebugText() {
        text.text = $"FPS: {(1/Time.deltaTime).ToString("F0")}\n" +
                    $"deltaTime: {Time.deltaTime}\n" +
                    $"Timescale: {Time.timeScale}\n\n" +

                    $"Gravity: {controller.gravity}\n" +
                    $"Speed: {velocity.magnitude.ToString("f2")}\n" +
                    //$"Acceleration: {controller.Acceleration().ToString("F4")}\n" +
                    $"Velocity: {velocity.ToString("F6")}\n" +
                    $"Position: {transform.position.ToString("F4")}\n" +
                    $"MoveDir: {moveDir}\n" +
                    $"LookDir: {transform.forward.ToString("f2")}\n\n" +
                        
                    $"Grounded: {controller.isGrounded}\n" +
                    $"On Slope: {controller.isOnSlope}\n" +
                    $"Slope Angle: {controller.slopeAngle}\n" +
                    $"Sliding: {controller.isSliding}\n\n" +
                        
                    $"Crouching: {controller.isCrouching}\n" +
                    //$"Sprinting: {controller.motor.isSprinting}\n" +
                    $"Try Jump: {jump}\n" +
                    $"Coyote: {controller.coyote}\n"
        ;
    }

    void OnEnable() {
        input.Enable();
    }

    void OnDisable() {
        input.Disable();
    }
}

