# F-ALS Setup Guide (Unity)

This guide brings your prefab to a runnable F-ALS 1.0 loop.

## 1) Auto setup (recommended)

1. Select player root GameObject.
2. Open `Tools > F-ALS > Auto Setup Selected Player`.
3. Press `Apply F-ALS Core Setup`.

The tool creates/assigns:

- `CharacterController`
- `FAlsLocomotionMotor`
- `FAlsController`
- `FAlsInputDriver`
- `FAlsBootstrap`
- `FAlsAnimatorBridge`
- `FAlsProceduralPoseDriver`

Auto assigns:

- `FAlsLocomotionMotor.characterController`
- `FAlsBootstrap.controller`
- `FAlsBootstrap.inputDriver`
- `FAlsBootstrap.ballTransform` is left manual (optional)
- `FAlsAnimatorBridge.animator`
- `FAlsProceduralPoseDriver` pelvis/foot transforms if found by name

## 2) Animator parameter contract

Add these parameters to your Animator:

- Bool: `FALS_Grounded`, `FALS_ActionReady`
- Float: `FALS_DesiredSpeed`, `FALS_Stride`, `FALS_MoveAlpha`, `FALS_Lean`
- Float: `FALS_PhysicalControl`, `FALS_FootLock`, `FALS_PelvisUp`, `FALS_PelvisForward`
- Float: `FALS_LeanCorrection`, `FALS_GroundAdaptation`, `FALS_Balance`, `FALS_LeftFootY`, `FALS_RightFootY`
- Int: `FALS_Gait`, `FALS_RotationMode`, `FALS_Stance`, `FALS_Action`, `FALS_FootballAction`, `FALS_LockedFoot`

## 3) Runtime loop

- `FAlsBootstrap.Update()` updates locomotion/action.
- `FAlsBootstrap.LateUpdate()` pushes signals to animator.
- `FAlsProceduralPoseDriver.LateUpdate()` applies pelvis/feet offsets.

## 4) Inputs

Default legacy bindings are:
- Horizontal/Vertical for movement
- LeftShift = sprint
- C = crouch
- Space = jump
- LeftMouse = shot intent

Adjust in `FAlsInputDriver`.
