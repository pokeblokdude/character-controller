using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     [BROKEN] Quake/Source-style acceleration movement with air-strafing, bunnyhopping, etc.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Quake : MonoBehaviour, ICharacterMotor {
    
    [SerializeField] private float _friction = 4.8f;
    [SerializeField] private float _accelerate = 5.6f;
    [SerializeField] private float _airAccelerate = 12;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character) {
        
        if(character.IsGrounded) {
            // one-frame window of no friction (for bhopping)
            if(!character.LandedThisFrame) {
                float speed = currentVel.magnitude;
                if(speed != 0) {
                    float drop = speed * _friction * Time.deltaTime;
                    currentVel *= Mathf.Max(speed - drop) / speed;
                }
            }

            return Accel(wishDir, currentVel, _accelerate, character.MaxSpeed);
        }
        else {
            return Accel(wishDir, currentVel, _airAccelerate, character.MaxSpeed);
        }
    }

    private Vector3 Accel(Vector3 wishDir, Vector3 currentVel, float accel, float maxSpeed) {
        float projVel = Vector3.Dot(currentVel, wishDir);
        float accelVel = accel * Time.deltaTime;

        if(projVel + accelVel > maxSpeed) {
            accelVel = maxSpeed - projVel;
        }

        return currentVel + wishDir * accelVel;
    }
}
