using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     [BROKEN] Quake/Source-style acceleration movement with air-strafing, bunnyhopping, etc.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Quake : MonoBehaviour, ICharacterMotor {
    
    public float friction = 4.8f;
    public float accelerate = 5.6f;
    public float airAccelerate = 12;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character) {
        
        if(character.isGrounded) {
            // one-frame window of no friction (for bhopping)
            if(!character.landedThisFrame) {
                float speed = currentVel.magnitude;
                if(speed != 0) {
                    float drop = speed * friction * Time.deltaTime;
                    currentVel *= Mathf.Max(speed - drop) / speed;
                }
            }

            return Accel(wishDir, currentVel, accelerate, character.maxSpeed);
        }
        else {
            return Accel(wishDir, currentVel, airAccelerate, character.maxSpeed);
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
