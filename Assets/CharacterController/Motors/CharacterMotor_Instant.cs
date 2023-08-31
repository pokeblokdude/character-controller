using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple movement with instantaneous starting/stopping.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Instant : MonoBehaviour, ICharacterMotor {
    
    public float walkSpeed = 5;     // properties can't be accessed in the inspector, so duplicate is needed
    public float crouchSpeedMult = 0.5f;
    public float sprintSpeedMult = 2.0f;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character) {
        Vector3 v = wishDir * walkSpeed;
        if(character.isCrouching) { return v * crouchSpeedMult; }
        if(character.isSprinting) { return v * sprintSpeedMult; }
        return v;
    }
}
