# Minimal Animator Setup for F-ALS 1.0

Цель: получить рабочий baseline-монитор locomotion (idle/walk/run/jump), чтобы проверить pipeline `FAlsBootstrap -> FAlsController -> FAlsAnimatorBridge`.

## Параметры

Create/ensure parameters:

- `FALS_Grounded` (Bool)
- `FALS_DesiredSpeed` (Float)
- `FALS_Stride` (Float)
- `FALS_MoveAlpha` (Float)
- `FALS_Lean` (Float)
- `FALS_Gait` (Int)
- `FALS_RotationMode` (Int)
- `FALS_Stance` (Int)
- `FALS_Action` (Int)
- `FALS_FootballAction` (Int)
- `FALS_ActionReady` (Bool)
- `FALS_PhysicalControl` (Float)
- `FALS_FootLock` (Float)
- `FALS_PelvisUp` (Float)
- `FALS_PelvisForward` (Float)
- `FALS_LeanCorrection` (Float)
- `FALS_GroundAdaptation` (Float)
- `FALS_Balance` (Float)
- `FALS_LeftFootY` (Float)
- `FALS_RightFootY` (Float)
- `FALS_LockedFoot` (Int)

## Состояния

1. `ALS_Idle`
2. `ALS_Move`
3. `ALS_Jump`
4. `ALS_Air` (optional для первых тестов)

## Логика переходов (минимум)

- `ALS_Idle -> ALS_Move`
  - условие: `FALS_Grounded == true && FALS_MoveAlpha > 0.05`
- `ALS_Move -> ALS_Idle`
  - условие: `FALS_Grounded == true && FALS_MoveAlpha <= 0.05`
- `ALS_Idle -> ALS_Jump`
  - условие: `FALS_Grounded == false || FALS_Action == 3` *(если Motion enum maps `Jump`=3)*
- `ALS_Move -> ALS_Jump`
  - условие: `FALS_Action == 3`
- `ALS_Jump -> ALS_Air`
  - условие: `FALS_Grounded == false`
- `ALS_Air -> ALS_Move`
  - условие: `FALS_Grounded == true && FALS_MoveAlpha > 0.05`
- `ALS_Air -> ALS_Idle`
  - условие: `FALS_Grounded == true && FALS_MoveAlpha <= 0.05`

Return transitions should have short blend times (0.08–0.12).

## Motion blending внутри ALS_Move

- `ALS_Move` uses Blend Tree with:
  - `Speed = FALS_DesiredSpeed`
  - clips `walk`, `run`, `sprint` as thresholds `0..3.2..5.8..7.4+`
- Optionally use `FALS_Stride` to drive secondary blend.

## Дебаг

- Attach `FAlsSignalDebugger` and set `logOnUpdate = true` for 4-8 sec.
- If `FALS_Grounded` не меняется при прыжке, проверьте:
  - `CharacterController` collision и сцена
  - `FAlsController` получает input
  - `FAlsLocomotionMotor.TickMotor` вызывается
