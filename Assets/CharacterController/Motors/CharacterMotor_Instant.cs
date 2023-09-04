using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple movement with instantaneous starting/stopping.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Instant : MonoBehaviour, ICharacterMotor {
    
    [SerializeField] private float _walkSpeed = 5;
    [SerializeField] private float _crouchSpeedMult = 0.5f;
    [SerializeField] private float _sprintSpeedMult = 2.0f;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character) {
        Vector3 v = wishDir * _walkSpeed;
        if(character.IsCrouching) { return v * _crouchSpeedMult; }
        if(character.IsSprinting) { return v * _sprintSpeedMult; }
        return v;
    }
}
