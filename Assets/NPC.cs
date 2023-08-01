using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    KinematicCharacterController controller;

    void Start()
    {
        controller = GetComponent<KinematicCharacterController>();
    }

    void FixedUpdate() {
        controller.Move(Vector2.zero, false);
    }
}
