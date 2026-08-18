# F-ALS × FGP_First Touch — интеграционная заметка

Для Codex: плагин подключён к проекту `C:\Users\MainAdmin\FGP_First Touch` как локальный пакет
(`Packages/manifest.json` → `"com.fgp.fals": "file:.../fals-unity"`). Эта заметка фиксирует, что проект
уже потребляет из плагина, что было изменено в самом плагине и какие изменения API ломают проект.

## 1. Изменения, внесённые в плагин извне (нужно знать)

- **`Runtime/FALS.Runtime.asmdef` — починен.** Файл содержал литеральные `` `n `` (PowerShell-артефакт)
  вместо переводов строк → невалидный JSON, сборка `FALS.Runtime` вообще не компилировалась.
  Переписан с тем же содержимым, но корректными переводами строк.
- **Отключены (переименованы в `.bak`), т.к. ссылались на `FAlsInputDriver.cs.bak`:**
  - `Runtime/Scripts/Runtime/FAlsBootstrap.cs` → `FAlsBootstrap.cs.bak`
  - `Editor/FalsAutoSetupWindow.cs` → `FalsAutoSetupWindow.cs.bak`
  - Если `FAlsInputDriver` вернётся в компиляцию, эти два файла нужно вернуть вместе с ним.

## 2. Что проект потребляет (ломать нельзя без миграции)

Живой потребитель — `Assets/_FGP/Scripts/FalsBridge/FAlsPlayerDriver.cs` в проекте:

- `FGP.FALS.Motion.FAlsLocomotionMotor` — поле `characterController`, `TickMotor(FAlsMotorInput, float)`, `State`.
- `FGP.FALS.Runtime.FAlsController` — `Tick(FAlsMotorInput, FAlsFootballActionInput, float)`, `Signals`.
- `FGP.FALS.Core`: `FAlsMotorInput`, `FAlsLocomotionState` (поля `Velocity`, `Gait`, `DesiredSpeed`, …),
  `FAlsGait` (`Walking/Running/Sprinting`).
- `FGP.FALS.Action`: `FAlsFootballActionInput`, `FAlsFootballActionOutput`.

Сериализуемые поля мотора, выставленные в сцене: `walkSpeed/runSpeed/sprintSpeed/acceleration/deceleration`
(значения из `MovementPreset_Player.asset`: 2.0 / 4.5 / 7.0 / 30 / 40).

## 3. Что проект НЕ использует и почему

- `FAlsInputDriver` / `FAlsBootstrap` — legacy `UnityEngine.Input`; в проекте Input System package,
  legacy-ввод отключён. Ввод идёт через `FGP.Input.InputHandler → IntentFrame` (канон Vision.md §2).
- `FAlsAnimatorBridge` — клиповый слой анимации отложен; сейчас процедурная репрезентация
  (`FGP.Presentation.ProceduralGaitAnimator`) поверх сигналов.

## 4. Известные пробелы мотора (закрыты на стороне проекта — кандидаты в плагин)

- **Мотор не поворачивает корпус** (`RotationMode` объявлен, кода нет) — проект крутит transform сам
  (velocity-facing, turnRate из пресета).
- **Тик в `Update`, не в `FixedUpdate`** — проект принял для прототипа; мяч физически независим.
- **`JumpImpulse` захардкожен (6.5)** — не тронуто.
- **CharacterController не толкает Rigidbody-мяч** — закрыто `FAlsBallPush` (OnControllerColliderHit → impulse).
- Стамина/спринт-гейт и action locks (Vision §3.4) — на стороне проекта в драйвере.

## 5. Просьба при развитии плагина

- Эволюция API — аддитивно (новые поля/методы ок; переименование/удаление — сначала миграция в проекте).
- Если появятся: поворот корпуса в моторе, intent-driven ввод (не legacy), физический тик —
  проект упростит/удалит мосты. Об этом напишем в этот файл.
