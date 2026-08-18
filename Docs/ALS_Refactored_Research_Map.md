# ALS-Refactored → F-ALS research map

Цель: использовать third_party/ALS-Refactored как базовую механику для fals-unity без разрушающих изменений API из INTEGRATION.md.

Текущий статус: плагин уже потребляется в проекте FGP_First Touch как локальный UPM-пакет; любые изменения в публичных сигнатурах должны быть аддитивными, если нет миграции в проекте.

## 0) Жесткие ограничения интеграции (не ломать)

- API contract (из INTEGRATION.md):
  - FAlsLocomotionMotor: поля/методы characterController, TickMotor, State.
  - FAlsController: Tick, Signals.
  - FAlsCore: FAlsMotorInput, FAlsLocomotionState, FAlsGait.
  - FAlsAction: FAlsFootballActionInput, FAlsFootballActionOutput.
- Сериализуемые поля сцены мотора должны сохраниться: walkSpeed, runSpeed, sprintSpeed, acceleration, deceleration.
- FAlsInputDriver/FAlsBootstrap в проекте не используются (Input System через проектный handler).
- Известно, что сейчас проект закрывает:
  - поворот корпуса на стороне сцены (FAlsPlayerDriver),
  - jumpImpulse (жестко 6.5),
  - tick в Update,
  - отсутствующий animator bridge.

## 1) Matrix KEEP / ADAPT / DROP

### A. Движение и состояние (Core movement)

Источники ALS: ALS/Public/AlsCharacterMovementComponent.h/.cpp, AlsCharacter.h/cpp (RefreshLocomotion*, SetRotation*, режимы Grounded/InAir).

- KEEP:
  - двухсостояние Grounded / InAir в FAlsLocomotionState.
  - сериализация gait, времени пребывания в состоянии (GroundedTime, AirTime), Action, Velocity, IsGrounded, IsMoving, HasInput, yaw-angle поля.
  - структура input/output в FAlsCore.
- ADAPT:
  - Mode переключение по факту CharacterController.isGrounded сейчас, но добавить/усилить guard по isGrounded с hysteresis для устойчивости.
  - интерполяцию желаемой скорости и ускорений под ALS-паттерн GaitAmountFromSpeed.
- DROP:
  - UE сетевые prediction hooks/SavedMove и PREDICTED-подходы.
  - ACharacter-спец. зависимости и net fields.

### B. Locomotion rules и gait

Источники ALS: ALS/Public/Settings/AlsMovementSettings.h, AlsGroundedSettings.h, AlsInAirSettings.h.

- KEEP:
  - SelectTargetSpeed + отдельные параметры walk/run/sprint.
  - crouch-модификатор скорости.
- ADAPT:
  - CanSprint как конус ввода относительно facing.
  - сглаживание скорости через FAlsMotionRules и FAlsMath.Damp/DampAngle вместо одних Mathf.Lerp/LerpAngle.
- DROP:
  - зависимости от UCurveFloat/CurveAssets пока не переносятся напрямую.

### C. Rotation / orientation

Источники ALS: AlsCharacter.cpp (RefreshGroundedRotation, RefreshInAirRotation, ConstrainAimingRotation, ApplyRotationYawSpeedAnimationCurve) и AlsMath.cpp.

- KEEP:
  - режимы FAlsRotationMode: VelocityDirection, ViewDirection, Aiming.
  - RotationScale в input как gate для блокировок/локов.
- ADAPT:
  - для Grounded уже сделан ExtraSmoothYaw и split по режимам; требуется:
    - выделить SetRotationSmooth/SetRotationExtraSmooth как reusable методы,
    - добавить in-air branch с own cap/half-life,
    - учесть turn-in-place offset + TurningInPlace флаг для сигналов.
- DROP:
  - полные animation-curve hooks ALS, пока нет animator graph интеграции.

### D. Foot / pelvis / lock system

Источники ALS: AlsAnimationInstance и Rig Units (FootOffset, ApplyFootOffset, states: AlsFeetState).

- KEEP:
  - непрерывный FootLock в сигнале.
  - LeftFootOffset, RightFootOffset, PelvisOffset, LockedFoot.
- ADAPT:
  - current FAlsProceduralSolver считать v1: добавить стабилизацию lock-window (предыдущее значение lock для избежания дрожания),
  - перейти от чисто шага к land/air blend через AirTime/GroundedTime.
- DROP:
  - exact control rig graph и UE rig math.

### E. Action & recoveries / ragdoll

Источники ALS: AlsCharacter_Actions.cpp, AlsRagdollingSettings.h, AlsRagdollingState.h.

- KEEP:
  - intent-first action архитектуру и enum-like action outputs (уже близко в FAlsFootballActionOutput).
  - PhysicalControl как fallback-коэффициент в FAlsLocomotionState.
- ADAPT:
  - добавить locomotion actions и флаги фазы: ActiveRagdoll, FullRagdoll, Recovery.
  - подключить их через FAlsController без смены существующих public API.
- DROP:
  - full ragdoll phys-body integration из UE пока out of scope.

### F. Animator bridge

Источники ALS: animation/animgraph layer + parameter routing.

- KEEP:
  - текущий FAlsAnimatorBridge маппит основной set (FALS_Grounded, FALS_FootLock, FALS_PelvisUp, FALS_LeftFootY, FALS_RightFootY).
- ADAPT:
  - расширить контракт параметров только через новые поля bridge-резервов или optional mappings.
- DROP:
  - старый clip-driven path (legacy) на уровне plugin bridge не используется.

## 2) Что уже реализовано в текущей ветке плагина

- FAlsLocomotionMotor.cs:
  - TickMotor(FAlsMotorInput,float),
  - RotationMode и RefreshGroundedRotation,
  - jump request/ground timers,
  - pivot gate по углу + decel,
  - FAlsMotionRules+FAlsMath integration.
- Runtime/FALS.Runtime.asmdef и package manifest уже выровнены на рабочий.
- FAlsController.Tick и Signals уже формируются из Motion + Action + Procedural.
- FAlsProceduralSignals/Resolver уже выдают lock/pelvis/foot offsets/balance как непрерывные сигналы.

## 3) Что нужно закрыть для следующего шага (Roadmap)

1. Phase 2.5 (безопасно и без breakage):
   - выделить rotation-utilities в FAlsMotion как чистые функции и использовать их в motor и bridge.
   - добавить in-air rotation branch с отдельными half-life/скоростными лимитами.
2. Phase 3: locomotion state parity:
   - доработать FAlsMotionRules под GaitAmount и MovementDirection в стиле ALS.
   - добавить устойчивые переходы ground -> air -> land, включая landing grace для false negatives.
3. Phase 4: procedural parity:
   - stabilizers для FootLock/Balance через low-pass/хистерезис.
   - добавить влияние GroundedTime/AirTime на высоты и смещения.
4. Phase 5: action/ragdoll:
   - добавить action-state расширения и мост PhysicalControl в bridge для animation blend.

## 4) Что уже НЕ переносится как как есть

- репликация и NetworkPrediction из UE,
- GameplayTags и UCharacterMovementComponent-интеграция,
- ControlRig/rig units ассетный пайплайн,
- root motion и animation blueprint execution order.

## 5) Рекомендуемый порядок для Kimi (чтобы не ломать интеграцию сейчас)

1. Keep public API unchanged while adding additive fields/methods.
2. Реализовать in-air rotation и landing blending (минимально зависимо от проекта).
3. Добавить/закрыть валидацию для FAlsMotionRules и FAlsLocomotionMotor по sprint cone, ground slope clamp, rotation gating.
4. После этого передавать diff только по этим файлам; миграции в проекте не требуется.
