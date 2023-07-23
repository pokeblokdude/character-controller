using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class KinematicCharacterController : MonoBehaviour {
    
    [Header("Movement")]

    [Tooltip("Whether or not to ramp up/down movement speed rather than starting and stopping instantaneously.")]
    public bool useAcceleration;

    [Tooltip("The maximum movement speed. For non-acceleration movement, this is the only speed.")]
    public float maxSpeed = 5;
    
    [Tooltip("The amount of time it takes to reach full speed from a stand-still.")]
    public float accelTime = 0.25f;

    [Tooltip("The amount of time it takes to come to a stand-still from full speed.")]
    public float deccelTime = 0.25f;

    [Tooltip("Whether or not to apply gravity to the controller.")]
    public bool useGravity = true;

    [Tooltip("The max speed above which falling speed will be capped.")]
    public float maxFallSpeed = 20;

    
    [Header("Collision")]

    [Tooltip("Whether or not collision checks should be performed.")]
    public bool noclip = false;

    [Tooltip("Which layers the controller should take into account when checking for collisions.")]
    public LayerMask collisionMask;

    [Tooltip("\"Buffer\" distance inside the collider from which to start all collision checks. Should be very small (but not too small).")]
    public float skinWidth = 0.015f;
    
    [Tooltip("The maximum angle at which the controller will treat the surface like a slope. Must be less than minWallAngle.")]
    [Range(1, 89)] public float maxSlopeAngle = 60;

    [Tooltip("The minimum angle at which the controller will treat a surface like a wall. Must be greater than maxSlopeAngle.")]
    public float minWallAngle = 89;
    
    [Tooltip("The minimum angle at which the controller will treat a surface like a ceiling.")]
    [Range(91, 179)] public float minCeilingAngle = 179;
    
    public float maxStepHeight = 0.2f;
    
    
    [Header("Jump")]

    [Tooltip("The height the controller can jump. Determines gravity along with jumpDistance.")]
    public float jumpHeight = 2;

    [Tooltip("The distance the controller can jump when moving at maxSpeed. Determines gravity along with jumpHeight.")]
    public float jumpDistance = 4;

    
    private Vector2 groundSpeed;
    private Vector3 velocity;
    public bool isGrounded { get; private set; } = false;
    private float groundHitDist;

    public bool onSlope { get; private set; }
    public float slopeAngle { get; private set; }
    public bool sliding { get; private set; }
    private Vector3 slopeNormal;
    
    private Vector3 wallNormal;
    private float wallHitDist;
    public bool hittingWall { get; private set; }

    public float gravity { get; private set; }
    private bool jumping;
    private float jumpForce;
    private float accel;
    private float deccel;

    private Rigidbody rb;
    private CapsuleCollider col;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<CapsuleCollider>();

        float halfDist = jumpDistance/2;
        gravity = (-2 * jumpHeight * maxSpeed * maxSpeed) / (halfDist * halfDist);
        jumpForce = (2 * jumpHeight * maxSpeed) / halfDist;
        accel = maxSpeed / accelTime;
        deccel = maxSpeed / deccelTime;
    }

    public void Move(Vector2 dir, bool shouldJump) {
        bool move = dir != Vector2.zero;
        Vector3 moveAmount;

        // --- movement input
        if(!useAcceleration) {
            groundSpeed = dir * maxSpeed;
        }
        else {
            // --- WIP ---
            // if(move && new Vector3(moveAmount.x, 0, moveAmount.z).magnitude < maxSpeed) {
            //     moveAmount += new Vector3(dir.x * accel, 0, dir.y * accel) * Time.deltaTime;
            // }
            // else {
            //     moveAmount -= moveAmount * deccel * Time.deltaTime;
            // }
        }
        moveAmount = new Vector3(groundSpeed.x, velocity.y, groundSpeed.y);
        
        // --- gravity
        if(isGrounded) {
            moveAmount.y = gravity * Time.deltaTime;
        }
        if(Mathf.Abs(moveAmount.y) < maxFallSpeed) {
            moveAmount.y += gravity * Time.deltaTime;
        }

        jumping = false;
        if(shouldJump && isGrounded) {
            moveAmount.y = jumpForce;
            jumping = true;
        }


        // --- collision
        if(!noclip) {
            Bounds bounds = col.bounds;
            bounds.Expand(-2 * skinWidth);

            moveAmount *= Time.deltaTime;

            // -- ground
            if(!jumping) {
                GroundCollisions(ref moveAmount, bounds);
            }
            else {
                isGrounded = false;
            }
            // - handle slopes
            // normal slope movement
            if(isGrounded && onSlope && !sliding) {
                float mag = groundSpeed.magnitude * Time.deltaTime;
                moveAmount = Vector3.ProjectOnPlane(moveAmount, slopeNormal).normalized;
                moveAmount *= mag;
            }
            // sliding down steep slope
            else if(onSlope && sliding) {
                float mag = moveAmount.magnitude;
                moveAmount = Vector3.ProjectOnPlane(moveAmount, slopeNormal).normalized;
                moveAmount *= mag;
            }

            // -- walls
            WallCollisions(moveAmount, bounds);
            // if hitting a wall, snap to it and slide along it
            if(hittingWall) {
                Vector3 moveToWall = moveAmount.normalized * wallHitDist;
                Vector3 leftOver = moveAmount - moveToWall;

                float scale = 1 - Vector3.Dot(wallNormal, -new Vector3(moveAmount.x, 0, moveAmount.z).normalized);
                scale = -((scale-1)*(scale-1)) + 1;     // smooth out the scaling a little bit
                float mag = leftOver.magnitude;
                
                Vector3 proj = Vector3.ProjectOnPlane(moveAmount, wallNormal).normalized;
                moveAmount = new Vector3(moveToWall.x + (proj.x * mag * scale), moveAmount.y, moveToWall.z + (proj.z * mag * scale));
            }

            // -- ceiling
            CeilingCollisions(ref moveAmount, bounds);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        rb.MovePosition(transform.position + moveAmount);
        
        velocity = moveAmount / Time.deltaTime;
        Debug.DrawRay(rb.position, velocity, Color.green, Time.deltaTime);
    }

    private void GroundCollisions(ref Vector3 moveAmount, Bounds bounds) {
        float dist = moveAmount.y < 0 || onSlope ? Mathf.Abs(moveAmount.y) + skinWidth : 2*skinWidth;

        RaycastHit hit;
        Color color = Color.red;
        if(
            Physics.SphereCast( 
                bounds.center,
                bounds.extents.x,
                Vector3.down,
                out hit,
                bounds.extents.y - bounds.extents.x + dist,
                collisionMask
            )
        ) {
            Debug.DrawRay(hit.point, hit.normal, Color.cyan, Time.deltaTime);
            slopeNormal = hit.normal;
            groundHitDist = hit.distance - (bounds.extents.y - bounds.extents.x) - skinWidth;
            slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            onSlope = slopeAngle > 0.1f;
            if(slopeAngle <= maxSlopeAngle) {
                sliding = false;
                isGrounded = true;
                moveAmount.y = -(hit.distance - (bounds.extents.y - bounds.extents.x) - skinWidth);
                color = Color.green;
            }
            else {
                isGrounded = false;
                sliding = true;
            }
        }
        else {
            isGrounded = false;
            slopeNormal = Vector3.up;
            onSlope = false;
            sliding = false;
        }
        Debug.DrawRay(bounds.center - new Vector3(0, bounds.extents.x, 0), Vector3.down * (bounds.extents.y - bounds.extents.x + dist), color, Time.deltaTime);
    }

    private void WallCollisions(Vector3 moveAmount, Bounds bounds) {
        hittingWall = false;
        float dist = moveAmount.magnitude + skinWidth;
        // if on slope, cast along movement direction, otherwise just cast forward
        Vector3 direction = onSlope ? moveAmount.normalized : new Vector3(moveAmount.x, 0, moveAmount.z).normalized;

        RaycastHit hit;
        Color color = Color.blue;
        if(
            Physics.CapsuleCast( 
                bounds.center + new Vector3(0, col.height/2 - col.radius, 0),
                bounds.center - new Vector3(0, col.height/2 - col.radius, 0),
                bounds.extents.x,
                direction,
                out hit,
                dist,
                collisionMask
            )
        ) {
            wallNormal = hit.normal;
            wallHitDist = hit.distance - skinWidth;
            Debug.DrawRay(hit.point, hit.normal, Color.magenta, Time.deltaTime);
            
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if(angle >= minWallAngle && angle < minCeilingAngle) {
                color = Color.cyan;
                hittingWall = true;
            }
        }
        
        Debug.DrawRay(
            bounds.center + new Vector3(moveAmount.x, 0, moveAmount.z).normalized * bounds.extents.x,
            direction * dist,
            color,
            Time.deltaTime
        );
        // Debug.DrawRay(
        //     bounds.center + (new Vector3(moveAmount.x, 0, moveAmount.z).normalized * bounds.extents.x) + moveAmount.normalized * dist,
        //     Vector3.up,
        //     Color.white,
        //     5
        // );
    }

    private void CeilingCollisions(ref Vector3 moveAmount, Bounds bounds) {

    }

    public Vector3 GetVelocity() {
        return new Vector3(velocity.x, velocity.y, velocity.z);
    }
}