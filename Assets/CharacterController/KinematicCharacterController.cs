using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class KinematicCharacterController : MonoBehaviour {
    
    [Header("Movement")]

    [Tooltip("The height of the collider when crouching.")]
    public float crouchHeight = 1f;

    [Tooltip("Whether or not to apply gravity to the controller.")]
    public bool useGravity = true;

    [Tooltip("The max speed above which falling speed will be capped.")]
    public float maxFallSpeed = 20;

    
    [Header("Collision")]

    //[Tooltip("Whether or not collision checks should be performed.")]
    //public bool noclip = false;

    [Tooltip("Which layers the controller should take into account when checking for collisions.")]
    public LayerMask collisionMask;

    [Tooltip("Buffer distance inside the collider from which to start all collision checks. Should be very small (but not too small).")]
    public float skinWidth = 0.015f;

    [Tooltip("The maximum number of recursive collision \"bounces\" before the controller will stop.")]
    [SerializeField] private int maxCollisionDepth = 5;
    
    [Tooltip("The maximum angle at which the controller will treat the surface like a slope.")]
    [Range(1, 89)] public float maxSlopeAngle = 55;
    
    [Tooltip("The minimum angle at which the controller will treat a surface like a flat ceiling, stopping vertical movement.")]
    public float minCeilingAngle = 165;

    // [Tooltip("The maximum height for a wall to be considered a step that the controller will snap up onto.")]
    // public float maxStepHeight = 0.2f;
    
    [Header("Jump")]

    [Tooltip("The height the controller can jump. Determines gravity along with jumpDistance.")]
    public float jumpHeight = 2;

    [Tooltip("The distance the controller can jump when moving at max speed. Determines gravity along with jumpHeight.")]
    public float jumpDistance = 4;

    [Tooltip("How long after you leave the ground can you still jump.")]
    public float coyoteTime = 0.2f;


    [Header("Debug")]
    public bool SHOW_DEBUG = false;

    public ICharacterMotor motor { get; private set; }
    private Vector3 moveAmount;
    private Vector3 velocity;

    public bool isGrounded { get; private set; }
    private bool wasGrounded;
    public bool isBumpingHead { get; private set; }

    public bool isSliding { get; private set; }
    public bool isOnSlope { get; private set; }
    public float slopeAngle { get; private set; }
    private Vector3 slopeNormal;

    private List<RaycastHit> hitPoints;
    
    public bool shouldCrouch { get; set; }
    public bool isCrouching { get; private set; }
    private float height;

    public float gravity { get; private set; }
    private Vector3 gravityVector;
    public bool coyote { get; private set; }
    private bool jumping;
    private float jumpForce;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Bounds bounds;

    Color[] colors = { Color.red, new Color(1, 0.5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<CapsuleCollider>();
        col.center = new Vector3(0, col.height/2, 0);
        height = col.height;

        motor = GetComponent<ICharacterMotor>();

        float halfDist = jumpDistance/2;
        gravity = (-2 * jumpHeight * motor.maxWalkSpeed * motor.maxWalkSpeed) / (halfDist * halfDist);
        jumpForce = (2 * jumpHeight * motor.maxWalkSpeed) / halfDist;

        hitPoints = new List<RaycastHit>();
    }

    void Update() {
#if UNITY_EDITOR
        float halfDist = jumpDistance/2;
        gravity = (-2 * jumpHeight * motor.maxWalkSpeed * motor.maxWalkSpeed) / (halfDist * halfDist);
        jumpForce = (2 * jumpHeight * motor.maxWalkSpeed) / halfDist;
#endif
    }

    void OnDrawGizmos() {
        if(SHOW_DEBUG) {
            if(hitPoints == null) { return; }

            int i = 0;
            foreach(RaycastHit hit in hitPoints) {
                Color color = colors[i % (colors.Length-1)];
                Gizmos.DrawWireSphere(hit.point, 0.1f);
                Debug.DrawRay(hit.point, hit.normal, color, Time.deltaTime);
                i++;
            }
        }
    }

    /// <summary>
    ///  Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using
    ///  the "collide and slide" algorithm. Returns the current velocity.
    /// </summary>
    public Vector3 Move(Vector2 moveDir, bool shouldJump) {
        bounds = col.bounds;
        bounds.Expand(-2 * skinWidth);

        isCrouching = UpdateCrouchState(shouldCrouch);
        motor.Crouch(isCrouching);

        moveAmount = motor.Accelerate(new Vector3(moveDir.x, 0, moveDir.y), velocity) * Time.deltaTime;

        isGrounded = GroundCheck();
        isBumpingHead = CeilingCheck();

        // coyote time
        if(wasGrounded && !isGrounded) {
            StartCoroutine(CoyoteTime());
        }

        // scale movement to slope angle
        if(isGrounded && isOnSlope && !isBumpingHead) {
            moveAmount = ProjectAndScale(moveAmount, slopeNormal);
        }
        
        hitPoints.Clear();

        // --- collision
        //if(!noclip) {    
        moveAmount = CollideAndSlide(moveAmount, transform.position, 0, moveAmount);
        //}

        // --- gravity
        if(useGravity) {
            jumping = false;
            if(shouldJump && (isGrounded || coyote)) {
                gravityVector.y = jumpForce * Time.deltaTime;
                jumping = true;
                coyote = false;
            }

            if((isGrounded && !jumping) || (!isGrounded && isBumpingHead)) {
                gravityVector = new Vector3(0, gravity, 0) * Time.deltaTime * Time.deltaTime;
            }
            else if(gravityVector.y > -maxFallSpeed) {
                gravityVector.y += gravity * Time.deltaTime * Time.deltaTime;
            }
            
            moveAmount += CollideAndSlide(gravityVector, transform.position + moveAmount, 0, gravityVector, true);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        rb.MovePosition(transform.position + moveAmount);
        
        velocity = moveAmount / Time.deltaTime;
        if(SHOW_DEBUG) Debug.DrawRay(rb.position, velocity, Color.green, Time.deltaTime);

        wasGrounded = isGrounded;

        return velocity;
    }

    IEnumerator CoyoteTime() {
        coyote = true;
        yield return new WaitForSeconds(coyoteTime);
        coyote = false;
    }

    private bool UpdateCrouchState(bool shouldCrouch) {
        if(shouldCrouch && !isCrouching) {
            col.height = crouchHeight;
            col.center = new Vector3(0, col.height/2, 0);
            return true;
        }
        else if(isCrouching && !shouldCrouch) {
            if(CanUncrouch()) {
                col.height = height;
                col.center = new Vector3(0, col.height/2, 0);
                return false;
            }
        }
        return isCrouching;
    }

    private Vector3 CollideAndSlide(Vector3 startAmount, Vector3 startPos, int currentDepth, Vector3 initialMoveAmount, bool gravityPass = false) {
        // just stop if we reach max depth
        if(currentDepth >= maxCollisionDepth) {
            Debug.LogWarning("Maxing out collision depth");
            return Vector3.zero;
        }
        if(Mathf.Approximately(startAmount.magnitude, 0) || Vector3.Angle(startAmount, initialMoveAmount) > 90) {
            return Vector3.zero;
        }
        
        float dist = Mathf.Abs(startAmount.magnitude) + skinWidth;
        RaycastHit hit;
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
            hitPoints.Add(hit);

            float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
            Vector3 snapToSurface = startAmount.normalized * (hit.distance - skinWidth);
            Vector3 leftover = startAmount - snapToSurface;

            if(snapToSurface.magnitude <= skinWidth) {      // do not move at all during collision if the distance is too small
                snapToSurface = Vector3.zero;
            }

            // normal ground/slope movement
            if(surfaceAngle <= maxSlopeAngle) {
                if(gravityPass) {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // hitting a wall
            else {
                Vector3 hitNormalHorizontal = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                // scale projected amount based on the horizontal angle the controller is hitting the wall
                float scale = 1 - Vector3.Dot(
                    hitNormalHorizontal,
                    -new Vector3(initialMoveAmount.x, 0, initialMoveAmount.z).normalized
                );
                scale = -((scale-1)*(scale-1)) + 1;     // smooth out the scaling to be non-linear
                
                // if grounded and encounter a steep slope, treat it as a flat wall on flat ground
                if(isGrounded && !gravityPass) {
                    leftover = ProjectAndScale(new Vector3(leftover.x, 0, leftover.z), hitNormalHorizontal) * scale;
                }
                else {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

            return snapToSurface + CollideAndSlide(leftover, startPos + snapToSurface, currentDepth+1, initialMoveAmount);
        }

        // no collision
        return startAmount;
    }

    private Vector3 ProjectAndScale(Vector3 vector, Vector3 planeNormal) {
        float mag = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, planeNormal).normalized;
        vector *= mag;
        return vector;
    }

    private bool GroundCheck() {
        isSliding = false;

        float dist = 2 * skinWidth;
        Vector3 origin = bounds.center - new Vector3(0, col.height/2 - col.radius, 0);

        RaycastHit hit;
        if(Physics.SphereCast(origin, bounds.extents.x, Vector3.down, out hit, dist, collisionMask)) {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            slopeAngle = angle;
            slopeNormal = hit.normal;
            if(angle <= maxSlopeAngle) {
                isOnSlope = angle > 0.1f;
                return true;
            }
            else {
                isSliding = true;
            }
        }
        return false;
    }

    private bool CeilingCheck() {
        float dist = 2 * skinWidth;
        Vector3 origin = bounds.center + new Vector3(0, col.height/2 - col.radius, 0);

        RaycastHit hit;
        if(Physics.SphereCast(origin, bounds.extents.x, Vector3.up, out hit, dist, collisionMask)) {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            float hitAngle = Vector3.Angle(moveAmount.normalized, hit.normal);
            if(angle >= minCeilingAngle || hitAngle >= minCeilingAngle) {
                return true;
            }
        }
        return false;
    }

    private bool CanUncrouch() {
        float dist = height - crouchHeight + skinWidth;
        Vector3 origin = bounds.center + new Vector3(0, col.height/2 - col.radius, 0);

        RaycastHit hit;
        return !Physics.SphereCast(origin, bounds.extents.x, Vector3.up, out hit, dist, collisionMask);
    }
}