# F-ALS — Football Adaptive Locomotion System

F-ALS is a Unity 6 locomotion foundation for football games. It owns body locomotion and body-state signals; the game owns input, skills, stamina, ball physics, rules, AI and networking.

## Install

In Unity:

`Window > Package Manager > + > Install package from git URL...`

Use:

`https://github.com/JayO6661/F-ALS-Unity-6.5.git`

For reproducible builds, pin a full commit SHA.

## Supported baseline

- Unity 6000.x
- `com.unity.animation.rigging` 1.4.0
- CharacterController-based locomotion

## Production ownership

F-ALS owns:
- walk / run / sprint motor execution;
- acceleration, deceleration and turn-rate execution;
- grounded / in-air locomotion state;
- movement-capacity application;
- recovery / PhysicalControl state;
- procedural locomotion signals;
- football body-action readiness such as prepared, quick, reach and emergency touch.

The game owns:
- human / AI / replay input;
- stamina, skills, injuries and surfaces;
- ball Rigidbody and final impulses;
- possession and first-touch gameplay;
- action legality, match rules and networking.

Animation is presentation, not gameplay authority.

## Quick setup

1. Select the **player root** GameObject, not the visual mesh child.
2. Open `Tools > F-ALS > Setup Selected Player`.
3. Click `Apply Core Setup`.
4. Run `Tools > F-ALS > Validate Selected Player`.

The setup tool adds only the production core:
- `CharacterController`
- `FAlsLocomotionMotor`
- `FAlsController`
- `FAlsAnimatorBridge` when an Animator exists

`FAlsFootIK` is optional and should be added only after Animation Rigging targets and constraints are prepared.

## Runtime API

Primary integration points:
- `FAlsController.Tick(...)`
- `FAlsController.SetMovementCapacity(...)`
- `FAlsController.ResetMovementCapacity()`
- `FAlsController.Signals`
- `FAlsMovementCapacity`
- `FAlsFootballActionInput`

A production game should provide `FAlsMotorInput` and `FAlsFootballActionInput` from its own orchestration layer.

## Important integration rule

Do not run a second locomotion authority on the same actor. In a production game, F-ALS should be the only component that executes player locomotion.

## Optional utilities

`FAlsSignalDebugger` can log locomotion / procedural / football-action signals during development.

Legacy standalone input/bootstrap helpers may exist for package-level experimentation, but production projects should drive `FAlsController` through their own input/AI/network layer.

## Package layout

- `Runtime/` — runtime assembly and components
- `Editor/` — setup and validation tooling
- `package.json` — UPM manifest

No Unreal/ALS-Refactored C++ source is shipped inside the Unity package.
