# Simple Kinematic Character Controller (WIP)

This character controller is my attempt at a "one size fits all" solution for a 3D character controller. It's implemented using the "collide and slide" algorithm, outlined by Kasper Fauerby in his paper, "[Improved Collision detection and Response](https://www.peroxide.dk/papers/collision/collision.pdf)", and iterated on by Jeff Linahan in "[Improving the Numerical Robustness of Sphere Swept Collision Detection](https://arxiv.org/ftp/arxiv/papers/1211/1211.0059.pdf)."

I made a video showcasing and explaining the original algorithm and my implementation here: \
https://youtu.be/YR6Q7dUz2uk

and a follow-up video covering Linahan's improved algorithm, as well as stair detection, here: \
`TBA`

This controller can be used in both first and third-person games. All you need to do is provide a normalized `moveDir`.

### Public Methods

```cs
public Vector3 Move(Vector2 moveDir, bool shouldJump);
```
Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using the "collide and slide" algorithm. Returns the current velocity.

**Parameters** \
`moveAmount`: `Vector2` \
A normalized 2D vector representing the current direction the player wants to move (**NOT** scaled by `deltaTime`). \
`shouldJump`: `bool` \
Whether or not the controller should try to jump this frame.

**Returns** \
`Vector3` \
A 3D vector representing the controller's new (unscaled) velocity after gravity and collisions.

## Simple KCC Features
My character controller currently supports
- Custom collision detection
- Ground/ceiling detection
- Smooth movement on slopes, plus sliding down steep slopes\
- Climbing/descending stairs
- Parameterized jump height and distance 
  - Gravity is set based on these two values
- Coyote Time
- Crouching
- Extensible movement behavior using *Character Motor* components (see below)

My character controller currently does not support
- Colliding with other characters
- Moving platforms
- Flying, swimming, noclipping, or any other niche form of movement
- Networking/multiplayer

## Character Motors

The Kinematic Character Controller accelerates based on the methods provided by a *Character Motor* component, which implements the `ICharacterMotor` interface.

```cs
public interface ICharacterMotor {
    Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel);
}
```

Sample character motor implementations can be found in `/Assets/CharacterController/Motors/`.