# F-ALS 1.0 gap analysis against ALS-Refactored

Reference: Sixze/ALS-Refactored. Goal is behavioral/architectural adaptation for Unity, not API parity with Unreal Engine.

## Production principles retained

- Input intent is separated from locomotion simulation.
- Locomotion state is authoritative; Animator and IK consume signals only.
- Gait, acceleration/deceleration, pivoting and rotation modes are simulation concerns.
- Procedural foot/pelvis correction is presentation driven by simulation signals.
- Recovery/physical control is a simulation feedback loop, not an Animator-only value.
- Football actions resolve intent into prepared/quick/reach/emergency outcomes before presentation.

## 1.0 gaps closed

### Movement capacity contract

`FAlsMovementCapacity` adds runtime multipliers for speed, acceleration, deceleration, turn rate and air control. Higher-level systems such as stamina, player skills, injury, possession and surface modifiers can compose these values without taking ownership of F-ALS input or animation.

### Recovery state persistence

Recovery now keeps runtime state and timing across ticks:

`None -> Falling -> GroundedRecovery -> GetUp -> Standing -> None`

`PhysicalControl` is written back to the locomotion motor, closing the simulation feedback loop.

### Rotation input contract

The default input driver now explicitly supplies `RotationScale = 1` and a desired rotation mode. Sprint and shot edge events use `GetKeyDown` rather than remaining asserted every frame.

### Emergency football actions

Emergency kick/touch fallback now requires live ball-to-foot distance. A distant ball no longer becomes an automatic toe-poke because of static reach constants.

### Foot IK

Foot IK imports the locomotion state contract correctly, uses hit-point offsets for ground adaptation, lowers the pelvis toward the lower support foot, and applies support-foot shift with consistent local-space semantics.

### Unity package reproducibility

`.gitignore` no longer excludes Unity source assets (`.meta`, `.asmdef`, `.prefab`, `.asset`, `Packages/`). The package declares Unity 6 baseline and Animation Rigging dependency.

## Deliberate non-goals for F-ALS 1.0

The following ALS-Refactored features are not required for the football locomotion foundation and should not block 1.0:

- mantling framework;
- ALS camera framework;
- overlay system parity;
- Unreal Gameplay Tags parity;
- Unreal replication internals;
- root-motion-driven gameplay authority;
- editor tooling parity;
- full active-ragdoll muscle simulation.

## Required validation before merge/release

1. Open package in the target Unity 6.x editor with Animation Rigging installed.
2. Zero compile errors in Runtime scripts.
3. Player prefab can idle, walk, run, sprint, pivot, jump and land.
4. `RotationScale = 1` rotates normally; `RotationScale = 0` intentionally locks body rotation.
5. Applying movement capacity `Speed = 0.5` reduces all gait speeds approximately by half without changing Animator code.
6. Falling transitions into recovery and reaches `GetUp`/`Standing` instead of resetting each frame.
7. Foot IK keeps planted feet within configured visual tolerance on flat and stepped surfaces.
8. Emergency action returns `Miss` when neither foot is in reach and `ToePoke`/`StretchTouch` only when a live foot distance is valid.
9. Animator remains presentation-only: removing Animator must not stop locomotion simulation.

## Post-1.0 extension points

- ScriptableObject movement profiles/curves if tuning volume justifies them.
- New Input System adapter beside the legacy prototype driver.
- Ball-relative body readiness and contact geometry for football actions.
- Network command/snapshot adapter around intent and authoritative locomotion state.
- More advanced active-ragdoll/physical-animation layer only after gameplay collision requirements are proven.
