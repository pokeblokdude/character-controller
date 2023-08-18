using UnityEngine;

public interface ICharacterMotor {
    // properties can't be accessed in the inspector, so a duplicate field will be needed
    // in any implementing classes
    float maxWalkSpeed { get; set; }

    Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel);
    void Sprint(bool s);
    void Crouch(bool c);
}
