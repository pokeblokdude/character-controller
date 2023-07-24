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

    [Tooltip("The maximum number of recursive collision \"bounces\" before the controller will stop.")]
    public int maxCollisionDepth = 5;
    
    [Tooltip("The maximum angle at which the controller will treat the surface like a slope. Must be less than minWallAngle.")]
    [Range(1, 89)] public float maxSlopeAngle = 55;
    
    // [Tooltip("The maximum height for a wall to be considered a step that the controller will snap up onto.")]
    // public float maxStepHeight = 0.2f;
    
    
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
    private Vector3 gravityVector;
    private bool jumping;
    private float jumpForce;
    private float accel;
    private float deccel;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Bounds bounds;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<CapsuleCollider>();
        col.center = new Vector3(0, col.height/2, 0);

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
        // instant
        if(!useAcceleration) {
            groundSpeed = dir * maxSpeed;
        }
        // acceleration
        else {
            if(move && groundSpeed.magnitude < maxSpeed) {
                groundSpeed += dir * accel * Time.deltaTime;
            }
            else if(move && groundSpeed.magnitude > maxSpeed) {
                groundSpeed = groundSpeed.normalized * maxSpeed;
            }
            else {
                groundSpeed -= groundSpeed.normalized * deccel * Time.deltaTime;
                if(groundSpeed.magnitude < 0.1f) {
                    groundSpeed = Vector2.zero;
                }
            }
        }
        moveAmount = new Vector3(groundSpeed.x, 0, groundSpeed.y) * Time.deltaTime;

        isGrounded = GroundCheck(moveAmount * Time.deltaTime);

        if(isGrounded && onSlope) {
            float mag = moveAmount.magnitude;
            moveAmount = Vector3.ProjectOnPlane(moveAmount, slopeNormal).normalized;
            moveAmount *= mag;
        }

        // --- collision
        if(!noclip) {
            bounds = col.bounds;
            bounds.Expand(-2 * skinWidth);

            moveAmount = CollideAndSlide(moveAmount, transform.position, 0, moveAmount);
        }

        jumping = false;
        if(shouldJump && isGrounded) {
            gravityVector.y = jumpForce * Time.deltaTime;
            jumping = true;
        }

        // --- gravity
        if(useGravity) {
            if(isGrounded && !jumping) {
                gravityVector = new Vector3(0, gravity, 0) * Time.deltaTime * Time.deltaTime;
            }
            else if(Mathf.Abs(gravityVector.y) < maxFallSpeed) {
                gravityVector.y += gravity * Time.deltaTime * Time.deltaTime;
            }
            
            moveAmount += CollideAndSlide(gravityVector, transform.position + moveAmount, 0, gravityVector, true);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        rb.MovePosition(transform.position + moveAmount);
        
        velocity = moveAmount / Time.deltaTime;
        Debug.DrawRay(rb.position, velocity, Color.green, Time.deltaTime);
    }

    private Vector3 CollideAndSlide(Vector3 startAmount, Vector3 startPos, int currentDepth, Vector3 originalMoveAmount, bool gravityPass = false) {
        // just stop if we reach max depth (idek what scenario would have like 5+ surfaces all in one place lmao)
        if(currentDepth == maxCollisionDepth) {
            return Vector3.zero;
        }
        else {
            float dist = Mathf.Abs(startAmount.magnitude) + skinWidth;
            RaycastHit hit;
            // collision
            if(
                Physics.CapsuleCast( 
                    startPos + new Vector3(0, col.radius, 0),
                    startPos + new Vector3(0, col.height - col.radius, 0),
                    bounds.extents.x,
                    startAmount.normalized,
                    out hit,
                    dist,
                    collisionMask
                )
            ) {
                float hitDist = hit.distance;
                float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                float hitAngle = Vector3.Angle(-startAmount.normalized, hit.normal);
                Vector3 snapToSurface = startAmount.normalized * (hit.distance - skinWidth);
                Vector3 leftover = startAmount - snapToSurface;

                // normal ground/slope movement
                if(surfaceAngle <= maxSlopeAngle) {
                    if(gravityPass) {
                        return snapToSurface;
                    }

                    float mag = leftover.magnitude;
                    leftover = Vector3.ProjectOnPlane(leftover, hit.normal).normalized;
                    leftover *= mag;

                    return snapToSurface + CollideAndSlide(leftover, startPos + snapToSurface, currentDepth+1, originalMoveAmount);
                }
                // if the angle is very close to zero, just collide and don't slide
                else if(Mathf.Approximately(hitAngle, 0)) {
                    return snapToSurface;
                }
                // hitting a wall
                else {
                    float mag = leftover.magnitude;
                    float scale = 1 - Vector3.Dot(hit.normal, -new Vector3(originalMoveAmount.x, 0, originalMoveAmount.z).normalized);
                    scale = -((scale-1)*(scale-1)) + 1;     // smooth out the scaling a little bit
                    
                    leftover = Vector3.ProjectOnPlane(leftover, hit.normal).normalized;
                    leftover = new Vector3(leftover.x * mag * scale, 0, leftover.z * mag * scale);


                    return snapToSurface + CollideAndSlide(leftover, startPos + snapToSurface, currentDepth+1, originalMoveAmount);
                }

            }
            // no collision
            else {
                return startAmount;
            }
        }
    }

    private bool GroundCheck(Vector3 moveAmount) {

        float dist = moveAmount.y < 0 ? Mathf.Abs(moveAmount.y) + skinWidth : 2 * skinWidth;

        RaycastHit hit;
        if(
            Physics.SphereCast( 
                bounds.center - new Vector3(0, col.height/2 - col.radius, 0),
                bounds.extents.x,
                Vector3.down,
                out hit,
                dist,
                collisionMask
            )
        ) {
            groundHitDist = hit.distance;
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            slopeAngle = angle;
            slopeNormal = hit.normal;
            if(angle <= maxSlopeAngle) {
                onSlope = angle > 0.1f;
                return true;
            }
        }
        return false;
    }

    public Vector3 GetVelocity() {
        return new Vector3(velocity.x, velocity.y, velocity.z);
    }
}