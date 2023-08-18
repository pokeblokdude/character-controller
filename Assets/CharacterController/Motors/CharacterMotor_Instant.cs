using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple movement with instantaneous starting/stopping.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Instant : MonoBehaviour, ICharacterMotor {
    
    public float walkSpeed = 5;     // properties can't be accessed in the inspector, so duplicate is needed
    public float maxWalkSpeed { get; set; }
    public float crouchSpeedMult = 0.5f;
    public float sprintSpeedMult = 2.0f;

    private bool isCrouching;
    private bool isSprinting;

    void Awake() {
        maxWalkSpeed = walkSpeed;
    }

    void Update() {
#if UNITY_EDITOR
        maxWalkSpeed = walkSpeed;
#endif
    }

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel) {
        Vector3 v = wishDir * maxWalkSpeed;
        if(isCrouching) { return v * crouchSpeedMult; }
        if(isSprinting) { return v * sprintSpeedMult; }
        return v;
    }

    public void Sprint(bool s) {
        isSprinting = s;
    }

    public void Crouch(bool c) {
        isCrouching = c;
    }

}
