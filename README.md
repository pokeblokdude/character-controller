## A simple kinematic character controller for Unity.

This character controller is my attempt at a "one size fits all" solution for a 3D character controller. It's implemented using the "collide and slide" algorithm, outlined by Kasper Fauerby in his paper, "[Improved Collision detection and Response](https://www.peroxide.dk/papers/collision/collision.pdf)".

I made a video showcasing and explaining the algorithm and my implementation here: \
https://youtu.be/YR6Q7dUz2uk

This controller can be used in both first and third-person games. There is only one public method, desribed below. Currently you could think of it as equivalent to the built-in controller's `SimpleMove()` function. All you need to do is provide an unscaled `moveAmount`.

```cs
public void Move(Vector2 moveAmount, bool shouldJump)
```
- `moveAmount`: a 2D vector representing the current speed the player wants to move (**NOT** scaled by `deltaTime`).
- `shouldJump`: whether or not the controller should try to jump this frame.

## Features
My character controller currently supports
- Custom collision detection
- Both instantaneous and acceleration-based movement
- Sprinting
- Parameterized jump height and distance (gravity is set based on these two values)
- Ground/ceiling detection (needs some work at the peaks of slopes)
- Smooth movement on slopes, plus sliding down steep slopes

My character controller currently does not support
- Crouching
- Climbing stairs
- Colliding with other characters (I actually have no idea what would happen)
- Moving platforms
- Flying, swimming, noclipping, or any other niche form of movement