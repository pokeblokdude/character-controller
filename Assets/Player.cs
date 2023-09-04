using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(KinematicCharacterController))]
public class Player : MonoBehaviour {
    
    public Transform spawnPos;
    public Transform camTarget;
    public Text text;

    public KinematicCharacterController controller { get; private set; }
    InputActions input;

    Camera cam;

    Vector2 moveDir;
    Vector2 lookDir;
    float lookX;
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
            controller.IsSprinting = true;
        };
        input.Player.Sprint.canceled += ctx => {
            controller.IsSprinting = false;
        };
        input.Player.Crouch.performed += ctx => {
            controller.ShouldCrouch = true;
        };
        input.Player.Crouch.canceled += ctx => {
            controller.ShouldCrouch = false;
        };
        #endregion

        controller = GetComponent<KinematicCharacterController>();
        cam = Camera.main;
    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        setDebugText();
    }

    void FixedUpdate() {
        Vector3 direction = (Camera.main.transform.forward * moveDir.y + Camera.main.transform.right * moveDir.x);
        direction.y = 0;
        direction.Normalize();
        Vector2 dir = new Vector2(direction.x, direction.z);
        // velocity = controller.Move(new Vector2(0, 1), jump);
        velocity = controller.Move(dir, jump);
    }

    void setDebugText() {
        text.text = $"FPS: {(1/Time.deltaTime).ToString("F0")}\n" +
                    $"deltaTime: {Time.deltaTime}\n" +
                    $"Timescale: {Time.timeScale}\n\n" +

                    $"Gravity: {controller.Gravity}\n" +
                    $"Speed: {velocity.magnitude.ToString("f2")}\n" +
                    //$"Acceleration: {controller.Acceleration().ToString("F4")}\n" +
                    $"Velocity: {velocity.ToString("F6")}\n" +
                    $"Position: {transform.position.ToString("F4")}\n" +
                    $"MoveDir: {moveDir}\n" +
                    $"LookDir: {cam.transform.eulerAngles.ToString("F4")}\n\n" +
                        
                    $"Grounded: {controller.IsGrounded}\n" +
                    $"On Slope: {controller.IsOnSlope}\n" +
                    $"Slope Angle: {controller.SlopeAngle}\n" +
                    $"Sliding: {controller.IsSliding}\n" +
                    $"Climbing Step: {controller.isClimbingStep}\n\n" +

                    $"Crouching: {controller.IsCrouching}\n" +
                    //$"Sprinting: {controller.motor.isSprinting}\n" +
                    $"Try Jump: {jump}\n" +
                    $"Coyote: {controller.Coyote}\n"
        ;
    }

    void OnEnable() {
        input.Enable();
    }

    void OnDisable() {
        input.Disable();
    }
}
