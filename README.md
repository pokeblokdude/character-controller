# Simple Kinematic Character Controller (WIP)

This character controller is my attempt at a "one size fits all" solution for a 3D character controller. It's implemented using the "collide and slide" algorithm, outlined by Kasper Fauerby in his paper, "[Improved Collision detection and Response](https://www.peroxide.dk/papers/collision/collision.pdf)".

I made a video showcasing and explaining the algorithm and my implementation here: \
https://youtu.be/YR6Q7dUz2uk

This controller can be used in both first and third-person games. All you need to do is provide a normalized `moveDir`.

### Public Methods

```cs
public Vector3 Move(Vector2 moveDir, bool shouldJump);
```
Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using the "collide and slide" algorithm. Returns the current velocity.

**Parameters** \
`moveAmount`: `Vector2` \
A 2D vector representing the current speed the player wants to move (**NOT** scaled by `deltaTime`). \
`shouldJump`: `bool` \
Whether or not the controller should try to jump this frame.

**Returns** \
`Vector3` \
A 3D vector representing the controller's new (unscaled) velocity after gravity and collisions.

## Simple KCC Features
My character controller currently supports
- Custom collision detection
- Ground/ceiling detection
- Smooth movement on slopes, plus sliding down steep slopes
- Parameterized jump height and distance 
  - Gravity is set based on these two values
- Coyote Time
- Crouching
- Extensible movement behavior using *Character Motor* components (see below)

My character controller currently does not support
- Climbing stairs
- Colliding with other characters
- Moving platforms
- Flying, swimming, noclipping, or any other niche form of movement

## Character Motors

The Kinematic Character Controller accelerates based on the methods provided by a *Character Motor* component, which implements the `ICharacterMotor` interface.

```cs
public interface ICharacterMotor {
  // properties can't be accessed in the inspector, so a duplicate, serializable field will
  // be needed in any implementing classes
  float maxWalkSpeed { get; set; }

  Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel);
  void Sprint(bool s);
  void Crouch(bool c);
}
```

Sample character motor implementations can be found in `/Assets/CharacterController/Motors/`.