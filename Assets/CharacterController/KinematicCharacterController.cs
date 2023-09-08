using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class KinematicCharacterController : MonoBehaviour {
    
    [Header("Movement")]

    [Tooltip("The height of the collider when crouching.")]
    [SerializeField] private float m_crouchHeight = 1f;

    [Tooltip("Whether or not to apply gravity to the controller.")]
    [SerializeField] private bool m_useGravity = true;

    [Tooltip("The max speed above which falling speed will be capped.")]
    [SerializeField] private float m_maxFallSpeed = 20;

    [Tooltip("The default maximum movement speed (can be overridden by character motors).")]
    [field:SerializeField] public float MaxSpeed { get; private set; } = 5;

    
    [Header("Collision")]
    
    [Tooltip("Which layers the controller should take into account when checking for collisions.")]
    [SerializeField] private LayerMask m_collisionMask;

    [Tooltip(
        "Buffer distance inside the collider from which to start collision checks. Should be very small (but not too small)."
    )]
    [SerializeField] private float m_skinWidth = 0.015f;

    [Tooltip("The maximum number of recursive collision \"bounces\" before the controller will stop.")]
    [SerializeField] private int m_maxCollisionDepth = 5;
    
    [Tooltip("The maximum angle at which the controller will treat the surface like a slope.")]
    [SerializeField][Range(1, 89)] private float m_maxSlopeAngle = 55;
    
    [Tooltip("The minimum angle at which the controller will treat a surface like a flat ceiling, stopping vertical movement.")]
    [SerializeField] private float m_minCeilingAngle = 165;

    [Tooltip("The maximum height for a wall to be considered a step that the controller will snap up onto.")]
    [SerializeField] private float m_maxStepHeight = 0.2f;

    [Tooltip("The minimum depth for steps that the controller can climb.")]
    [SerializeField] private float m_minStepDepth = 0.1f;

    
    [Header("Jump")]

    [Tooltip("The height the controller can jump. Determines gravity along with jumpDistance.")]
    [SerializeField] private float m_jumpHeight = 2;

    [Tooltip("The distance the controller can jump when moving at max speed. Determines gravity along with jumpHeight.")]
    [SerializeField] private float m_jumpDistance = 4;

    [Tooltip("How long (in seconds) after you leave the ground can you still jump.")]
    [SerializeField] private float m_coyoteTime = 0.2f;


    [Header("Debug")]
    [SerializeField] private bool SHOW_DEBUG = false;


    public ICharacterMotor Motor { get; private set; }
    private Vector3 _moveAmount;
    private Vector3 _velocity;
    private Vector3 _groundSpeed;

    public bool IsGrounded { get; private set; }
    private bool _wasGrounded;
    public bool LandedThisFrame { get; private set; }
    public bool IsBumpingHead { get; private set; }

    public bool IsSliding { get; private set; }
    public bool IsOnSlope { get; private set; }
    public float SlopeAngle { get; private set; }
    private Vector3 _slopeNormal;

    public bool isClimbingStep { get; private set; }

    private List<RaycastHit> _hitPoints;
    private Vector3 _groundPoint;
    
    public bool ShouldCrouch { get; set; }
    public bool IsCrouching { get; private set; }
    private float _height;

    public bool IsSprinting { get; set; }

    public float Gravity { get; private set; }
    private Vector3 _gravityVector;
    public bool Coyote { get; private set; }
    private bool _jumping;
    private float _jumpForce;

    private Rigidbody _rb;
    private CapsuleCollider _col;
    private Bounds _bounds;

    private Color[] _colors = { Color.red, new Color(1, 0.5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _col = GetComponent<CapsuleCollider>();
        _col.center = new Vector3(0, _col.height/2, 0);
        _height = _col.height;

        Motor = GetComponent<ICharacterMotor>();

        float halfDist = m_jumpDistance/2;
        Gravity = (-2 * m_jumpHeight * MaxSpeed * MaxSpeed) / (halfDist * halfDist);
        _jumpForce = (2 * m_jumpHeight * MaxSpeed) / halfDist;

        _hitPoints = new List<RaycastHit>();
    }

    void Update() {
#if UNITY_EDITOR
        float halfDist = m_jumpDistance/2;
        Gravity = (-2 * m_jumpHeight * MaxSpeed * MaxSpeed) / (halfDist * halfDist);
        _jumpForce = (2 * m_jumpHeight * MaxSpeed) / halfDist;
#endif
    }

    void OnDrawGizmos() {
        if(SHOW_DEBUG) {
            Debug.DrawRay(transform.position, _velocity, Color.green, Time.deltaTime);

            if(IsGrounded || IsSliding) {
                Gizmos.DrawWireSphere(_groundPoint, 0.05f);
            }

            if(_hitPoints == null) { return; }

            int i = 0;
            foreach(RaycastHit hit in _hitPoints) {
                Color color = _colors[i % (_colors.Length-1)];
                Gizmos.DrawWireSphere(hit.point, 0.1f);
                Debug.DrawRay(hit.point, hit.normal, color, Time.deltaTime);
                i++;
            }
        }
    }

    /// <summary>
    ///  Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using
    ///  the "collide and slide" algorithm. Returns the current velocity. (Pick either this or Move())
    /// </summary>
    public Vector3 Move(Vector2 moveDir, bool shouldJump) {
        _bounds = _col.bounds;
        _bounds.Expand(-2 * m_skinWidth);

        IsCrouching = UpdateCrouchState(ShouldCrouch);

        _groundSpeed = Motor.Accelerate(new Vector3(moveDir.x, 0, moveDir.y), _groundSpeed, this);

        _moveAmount = _groundSpeed * Time.deltaTime;

        IsBumpingHead = CeilingCheck(transform.position);
        IsGrounded = GroundCheck(transform.position);
        LandedThisFrame = IsGrounded && !_wasGrounded;

        // coyote time
        if(_wasGrounded && !IsGrounded) {
            StartCoroutine(CoyoteTime());
        }

        // scale movement to slope angle
        if(IsGrounded && IsOnSlope && !IsBumpingHead) {
            _moveAmount = ProjectAndScale(_moveAmount, _slopeNormal);
        }
        
        _hitPoints.Clear();

        // --- collision   
        _moveAmount = CollideAndSlide(_moveAmount, transform.position);

        // --- gravity
        if(m_useGravity) {
            _jumping = false;
            if(shouldJump && (IsGrounded || Coyote)) {
                _gravityVector.y = _jumpForce * Time.deltaTime;
                _jumping = true;
                Coyote = false;
            }

            // if((IsGrounded || _wasGrounded) && !_jumping) {
            //     _moveAmount += SnapToGround(transform.position + _moveAmount);
            // }

            if((IsGrounded && !_jumping) || (!IsGrounded && IsBumpingHead)) {
                _gravityVector = new Vector3(0, Gravity, 0) * Time.deltaTime * Time.deltaTime;
            }
            else if(_gravityVector.y > -m_maxFallSpeed) {
                _gravityVector.y += Gravity * Time.deltaTime * Time.deltaTime;
            }
            
            _moveAmount += CollideAndSlide(_gravityVector, transform.position + _moveAmount, true);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        _rb.MovePosition(transform.position + _moveAmount);
        
        _wasGrounded = IsGrounded;
        _velocity = _moveAmount / Time.deltaTime;
        return _velocity;
    }

    IEnumerator CoyoteTime() {
        Coyote = true;
        yield return new WaitForSeconds(m_coyoteTime);
        Coyote = false;
    }

    private Vector3 CollideAndSlide(Vector3 dir, Vector3 pos, bool gravityPass = false) {
        Vector3 accumulator = Vector3.zero;
        Vector3 planeNormal1 = new Vector3();
        
        bool climbingStep = false;

        for(int i = 0; i < 3; i++) {
            if(Mathf.Approximately(dir.magnitude, 0)) { break; }

            float dist = dir.magnitude + m_skinWidth;
            Vector3 direction = dir.normalized;
            if(Physics.CapsuleCast(
                pos + new Vector3(0, _col.radius, 0),
                pos + new Vector3(0, _col.height - _col.radius, 0),
                _bounds.extents.x,
                direction,
                out RaycastHit hit,
                dist,
                m_collisionMask
            )) {
                _hitPoints.Add(hit);

                float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                Vector3 snapToSurface = direction * (hit.distance - m_skinWidth);

                if(snapToSurface.magnitude <= m_skinWidth) { snapToSurface = Vector3.zero; }
                if(gravityPass && surfaceAngle <= m_maxSlopeAngle) {
                    accumulator += snapToSurface;
                    break;
                }

                Vector3 leftover = dir - snapToSurface;
                
                if(i == 0) {
                    planeNormal1 = hit.normal;
                    // treat steap slope as flat wall when grounded
                    if(surfaceAngle > m_maxSlopeAngle && IsGrounded && !gravityPass) {
                        #region stair detection
                        float stepOffset = hit.point.y - _groundPoint.y + m_skinWidth;
                        Vector3 stepDirection = hit.point - pos;
                        stepDirection = new Vector3(stepDirection.x, 0, stepDirection.z).normalized;
                        if(stepOffset < m_maxStepHeight && stepOffset > m_skinWidth) {
                            print("try to climb step");
                            if(Physics.CapsuleCast(
                                pos + snapToSurface + new Vector3(0, _col.radius + stepOffset, 0),
                                pos + snapToSurface + new Vector3(0, _col.height - _col.radius + stepOffset, 0),
                                _bounds.extents.x,
                                leftover.normalized,
                                out RaycastHit stepCheck,
                                leftover.magnitude,
                                m_collisionMask
                            )) {
                                float stepWallAngle = Vector3.Angle(stepCheck.normal, Vector3.up);
                                if((stepCheck.distance - m_skinWidth) > m_minStepDepth || stepWallAngle <= m_maxSlopeAngle) {
                                    climbingStep = true;
                                }
                            }
                            else {
                                climbingStep = true;
                            }

                            if(climbingStep) {
                                snapToSurface.y += stepOffset;
                                snapToSurface += stepDirection * m_skinWidth;
                                print("step");
                            }
                        }
                        #endregion
                        planeNormal1 = new Vector3(planeNormal1.x, 0, planeNormal1.z).normalized;
                        leftover = new Vector3(leftover.x, 0, leftover.z);
                    }
                    leftover = Vector3.ProjectOnPlane(leftover, planeNormal1);
                    dir = leftover;
                }
                else if(i == 1) {
                    Vector3 crease = Vector3.Cross(planeNormal1, hit.normal).normalized;
                    if(SHOW_DEBUG) Debug.DrawRay(hit.point, crease, Color.cyan, Time.deltaTime);
                    float dis = Vector3.Dot(leftover, crease);
                    dir = crease * dis;
                }

                if(i < 2) {
                    accumulator += snapToSurface;
                    pos += snapToSurface;
                }
            }
            else {  // no collision
                accumulator += dir;
                break;
            }
        }
        return accumulator;
    }

    private Vector3 ProjectAndScale(Vector3 vector, Vector3 planeNormal) {
        float mag = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, planeNormal).normalized;
        vector *= mag;
        return vector;
    }

    private Vector3 SnapToGround(Vector3 pos) {
        float dist = m_maxStepHeight + m_skinWidth;
        if(Physics.CapsuleCast(
            pos + new Vector3(0, _col.radius, 0),
            pos + new Vector3(0, _col.height - _col.radius, 0),
            _bounds.extents.x,
            Vector3.down,
            out RaycastHit hit,
            dist,
            m_collisionMask
        )) {
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if(hit.distance - m_skinWidth < m_maxStepHeight && surfaceAngle <= m_maxSlopeAngle) {
                IsGrounded = true;
                return new Vector3(0, -(hit.distance - m_skinWidth), 0);
            }
        }
        return Vector3.zero;
    }

    private bool GroundCheck(Vector3 pos) {
        IsSliding = false;
        bool grounded = false;

        float dist = 2 * m_skinWidth;
        Vector3 origin = pos + new Vector3(0, _col.radius, 0);
        RaycastHit[] hits = Physics.SphereCastAll(origin, _bounds.extents.x, Vector3.down, dist, m_collisionMask);
        if(hits.Length > 0) {
            foreach(RaycastHit hit in hits) {
                if(Mathf.Approximately(hit.distance, 0)) { continue; }

                float angle = Vector3.Angle(Vector3.up, hit.normal);
                SlopeAngle = angle;
                _slopeNormal = hit.normal;
                _groundPoint = hit.point;
                if(angle <= m_maxSlopeAngle) {
                    IsSliding = false;
                    IsOnSlope = angle > 0.1f;
                    grounded = true;
                    break;
                }
                else { IsSliding = true; }
            }
        }
        return grounded;
    }

    private bool CeilingCheck(Vector3 pos) {
        float dist = 2 * m_skinWidth;
        Vector3 origin = pos + new Vector3(0, _col.height - _col.radius, 0);

        RaycastHit hit;
        if(Physics.SphereCast(origin, _bounds.extents.x, Vector3.up, out hit, dist, m_collisionMask)) {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            float hitAngle = Vector3.Angle(_moveAmount.normalized, hit.normal);
            if(angle >= m_minCeilingAngle || hitAngle >= m_minCeilingAngle) {
                return true;
            }
        }
        return false;
    }

    private bool UpdateCrouchState(bool shouldCrouch) {
        if(shouldCrouch && !IsCrouching) {
            _col.height = m_crouchHeight;
            _col.center = new Vector3(0, _col.height/2, 0);
            return true;
        }
        else if(IsCrouching && !shouldCrouch) {
            if(CanUncrouch()) {
                _col.height = _height;
                _col.center = new Vector3(0, _col.height/2, 0);
                return false;
            }
        }
        return IsCrouching;
    }

    private bool CanUncrouch() {
        float dist = _height - m_crouchHeight + m_skinWidth;
        Vector3 origin = _bounds.center + new Vector3(0, _col.height/2 - _col.radius, 0);
        return !Physics.SphereCast(origin, _bounds.extents.x, Vector3.up, out RaycastHit hit, dist, m_collisionMask);
    }
}