using UnityEngine;

public interface ICharacterMotor {
    Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character);
}
