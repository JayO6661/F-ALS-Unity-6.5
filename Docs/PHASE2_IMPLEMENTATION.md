# F-ALS Phase 2 Implementation Notes

## Completed in Phase 2

### 1. Recovery System (`FAlsRecoverySolver`)
- **File**: `Runtime/Scripts/Recovery/FAlsRecoverySolver.cs`
- **Features**:
  - `FAlsRecoveryState`: None → Falling → GroundedRecovery → GetUp → Standing
  - Stability calculation based on velocity, ground slope, and air time
  - PhysicalControl blending (0 = ragdoll, 1 = animated)
  - Automatic get-up request when stability drops below threshold

### 2. Controller Integration
- **File**: `Runtime/Scripts/Runtime/FAlsController.cs`
- **Changes**:
  - Added `FAlsRecoveryOutput` to `FAlsActorSignals`
  - Integrated recovery solver into the main tick loop
  - Exposed tuning parameters: `stabilityThreshold`, `recoveryDuration`, `getUpDuration`

### 3. Animator Bridge Extension
- **File**: `Runtime/Scripts/Runtime/FAlsAnimatorBridge.cs`
- **New Parameters**:
  - `FALS_RecoveryState` (Int): current recovery state enum
  - `FALS_Stability` (Float): 0..1 stability score
  - `FALS_RequestGetUp` (Bool): trigger for get-up animation
  - Updated `FALS_PhysicalControl` to use recovery value when active

### 4. Bootstrap & Input Driver Restored
- **Files**: 
  - `Runtime/Scripts/Runtime/FAlsBootstrap.cs` (from .bak)
  - `Runtime/Scripts/Runtime/FAlsInputDriver.cs` (from .bak)
- **Status**: Ready for Player prefab integration

### 5. Test Scene Placeholder
- **File**: `Scenes/FAls_TestScene.unity`
- **Contents**: Basic Unity scene settings (skybox, lighting, navmesh stub)

## Required Unity Setup

### Animator Parameters (add to your Animator Controller)

#### Locomotion
| Parameter | Type | Description |
|-----------|------|-------------|
| FALS_Grounded | Bool | Is character on ground |
| FALS_DesiredSpeed | Float | Target movement speed |
| FALS_Stride | Float | Stride blend (0..1) |
| FALS_MoveAlpha | Float | Input magnitude |
| FALS_Lean | Float | Body lean amount |
| FALS_Gait | Int | Walking(0)/Running(1)/Sprinting(2) |
| FALS_RotationMode | Int | Velocity(0)/View(1)/Aiming(2) |
| FALS_Stance | Int | Standing(0)/Crouching(1) |

#### Action
| Parameter | Type | Description |
|-----------|------|-------------|
| FALS_Action | Int | Locomotion action enum |
| FALS_FootballAction | Int | Football action enum |
| FALS_ActionReady | Bool | Action is ready to play |
| FALS_PhysicalControl | Float | Animated ↔ Ragdoll blend |

#### Procedural
| Parameter | Type | Description |
|-----------|------|-------------|
| FALS_FootLock | Float | Foot lock alpha |
| FALS_PelvisUp | Float | Pelvis vertical offset |
| FALS_PelvisForward | Float | Pelvis forward offset |
| FALS_LeanCorrection | Float | Lean correction value |
| FALS_GroundAdaptation | Float | Ground adaptation blend |
| FALS_Balance | Float | Balance score |
| FALS_LeftFootY | Float | Left foot IK offset Y |
| FALS_RightFootY | Float | Right foot IK offset Y |
| FALS_LockedFoot | Int | Which foot is locked (Left=0/Right=1) |

#### Recovery (NEW in Phase 2)
| Parameter | Type | Description |
|-----------|------|-------------|
| FALS_RecoveryState | Int | None(0)/Falling(1)/GroundedRecovery(2)/GetUp(3)/Standing(4) |
| FALS_Stability | Float | Stability score 0..1 |
| FALS_RequestGetUp | Bool | Request get-up transition |

## Player Prefab Setup

1. Create empty GameObject or use existing Player
2. Add components:
   - `CharacterController`
   - `FAlsLocomotionMotor`
   - `FAlsController`
   - `FAlsInputDriver`
   - `FAlsBootstrap`
   - `FAlsAnimatorBridge` (if using Animator)
   - `FAlsProceduralPoseDriver` (optional, for debug without Rigging)

3. Configure `FAlsBootstrap`:
   - Assign `controller`, `inputDriver`, `animatorBridge` (auto-filled by Reset)
   - Optionally assign `ballTransform` for live ball distance

4. Configure `FAlsController`:
   - Tune `stabilityThreshold` (default: 0.25)
   - Tune `recoveryDuration` (default: 0.8s)
   - Tune `getUpDuration` (default: 0.6s)

## Next Steps (Phase 2 remaining)

- [ ] Implement ActiveRagdoll physics bridge (requires Rigidbody/Ragdoll setup)
- [ ] Create Animation Rigging FootIK layer (replace procedural pose driver)
- [ ] Build test 1v1 scene with two players + ball
- [ ] Validate DoD: walk/run/sprint/jump/land/first touch fallback
- [ ] Add FAlsSignalDebugger for runtime signal inspection

## Architecture Notes

### Data Flow
```
Input (FAlsInputDriver)
    ↓
FAlsMotorInput + FAlsFootballActionInput
    ↓
FAlsController.Tick()
    ├── FAlsLocomotionMotor.TickMotor() → FAlsLocomotionState
    ├── FAlsFootballActionResolver.Resolve() → FAlsFootballActionOutput
    ├── FAlsProceduralSolver.Resolve() → FAlsProceduralSignals
    └── FAlsRecoverySolver.Resolve() → FAlsRecoveryOutput
    ↓
FAlsActorSignals (aggregated)
    ↓
FAlsAnimatorBridge.Apply() → Animator parameters
FAlsProceduralPoseDriver.LateUpdate() → Transform offsets (optional)
```

### Recovery Integration
The recovery system runs in parallel with locomotion and does NOT override motor physics.
Instead, it provides:
- `PhysicalControl` for animation blending between ragdoll and keyed poses
- `Stability` score for animator state machine transitions
- `RequestGetUp` flag to trigger recovery animations

The motor continues to simulate movement; recovery affects only the visual representation
until full stability is restored.
