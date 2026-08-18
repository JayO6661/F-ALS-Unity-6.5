# F-ALS — Unity plugin foundation (reverse-engineering-first)

Цель: не портирование Unreal-кода ALS-Refactored как есть, а построение
engine-independent модели locomotion, затем её реализация в Unity.

Исходный репозиторий локально сохранён:
`C:\Users\MainAdmin\Documents\ChatGPT\Footsim\third_party\ALS-Refactored`

## Что подтверждено

- ALS-Refactored — Unreal-плагин с модулями `ALS`, `ALSCamera`, `ALSExtras`,
`ALSEditor` и зависимостями под UE5 (`GameplayTags`, `ControlRig`, `Niagara`,
`AnimGraphRuntime` и т.д.).
- Основные домены в коде:
  - движение: `AlsCharacterMovementComponent`
  - state/анимационные сигналы: `AlsAnimationInstance`
  - игрок/экшены: `AlsCharacter`
  - настройки: `Public/Settings/*.h`
  - состояния: `Public/State/*.h`
  - foot lock/IK/rig: `Public/Nodes`, `Public/Utility`, `Private/Nodes`
- Архитектурно ALS-референс сильно привязан к UE runtime (replication, tags,
 anim blueprint/control rig), поэтому копипаста по классам делать нельзя.

## Принципы F-ALS 1.0 для Unity

1. Core-движок получает только `PlayerIntent` (ввод + сенсорные/физические условия).
2. Решение состояния и параметров выносится в чистые data-модели.
3. Анимация и IK используют только сигналы, а не управляют движением напрямую.
4. Для футбольных действий: `Shot/Pass/Touch` — это `Intent`, а не “проиграть конкретный
   анима клип по умолчанию”.
5. Наличие `PhysicalControl` (0..1) для мягких переходов `animated ↔ ragdoll`.

## Маппинг ALS → F-ALS

### KEEP (поведенческая логика переносится в Unity как модели/алгоритмы)
- направление скорости, ускорение и торможение (базовая kinematics логика)
- gait/locomotion action graph (walk/run/sprint, stride/acceleration/braking)
- grounding и переходы ground/air с предикатами устойчивости
- управление поворотом (velocity/view/aim modes)
- foot lock и pelvis compensation (как процедурные решения)
- recovery / get up пайплайн после physical instability

### MODIFY (UE-специфичная часть заменяется под Unity)
- `GameplayTags` → enum/чистые state keys + ScriptableEventMap
- AnimBlueprint/ControlRig ноды → Animator state + AnimationRigging/Playables
- root-motion/replication контракт → Unity CharacterController/Rigidbody + netcode-проектный слой

### DROP (не переносим как архитектуру)
- `ACharacter`/`UCharacterMovementComponent` классы
- Unreal Animation Notifies как базовый слой управления игровым состоянием
- UE replication internals (Iris/Push Model), `EnhancedInput` как есть
- `ALS.uplugin` и связанные Unreal editor-средства

## Предлагаемая структура плагина в Unity

- `FAls.Core`
  - `Input`, `State`, `Rules`, `Math`, `StateMachine`, `Settings`, `Signals`
- `FAls.Motion`
  - motor, ускорение, ускорительное сглаживание, land predictor
- `FAls.Procedural`
  - foot IK, pelvis/ground adaptation, lean/balance
- `FAls.Actions`
  - shot/pass/touch intent resolver + fallback (miss/quick/emergency)
- `FAls.Recovery`
  - active ragdoll ↔ full ragdoll ↔ recovery ↔ get up
- `FAls.UnityIntegration`
  - bridges к Animator, CharacterController, Physic, Input

## Что теперь делаем в коде

Следующий шаг в этой папке — расширять каркас:
- `FAlsMotor`: детерминированная обработка LocomotionState
- `FAlsProceduralBody`: сигналы `footOffset`, `pelvisOffset`, `stability`
- `FAlsFootballActionResolver`: выбор базового/быстрого/экстренного действия по reachability

Сделано так, чтобы переход к фазе 2 (реализация) был механически быстрым.

## Реализация в Unity — текущее состояние (этап 1)

Готово к следующему шагу подключения:

- `FAlsLocomotionMotor` — ядро movement-потока с состоянием, grounded/air и сигнальными полями.
- `FAlsMotionRules` — базовая математика и подбор целевой скорости/gait.
- `FAlsFootballActionResolver` — intent-first логика удара/контакта с fallback.
- `FAlsProceduralSolver` — сигналы для Foot Lock и Pelvis/Balance.
- `FAlsController` — фасад агрегации `locomotion + procedural + action`.
- `FAlsInputDriver` + `FAlsBootstrap` — быстрый прототип запуска цикла в Unity.

Список файлов:
- [Runtime/Scripts/Core/FAlsTypes.cs](Runtime/Scripts/Core/FAlsTypes.cs)
- [Runtime/Scripts/Motion/FAlsLocomotionMotor.cs](Runtime/Scripts/Motion/FAlsLocomotionMotor.cs)
- [Runtime/Scripts/Motion/FAlsMotionRules.cs](Runtime/Scripts/Motion/FAlsMotionRules.cs)
- [Runtime/Scripts/Action/FAlsFootballActionResolver.cs](Runtime/Scripts/Action/FAlsFootballActionResolver.cs)
- [Runtime/Scripts/Procedural/FAlsProceduralSignals.cs](Runtime/Scripts/Procedural/FAlsProceduralSignals.cs)
- [Runtime/Scripts/Procedural/FAlsProceduralPoseDriver.cs](Runtime/Scripts/Procedural/FAlsProceduralPoseDriver.cs)
- [Runtime/Scripts/Runtime/FAlsController.cs](Runtime/Scripts/Runtime/FAlsController.cs)
- [Runtime/Scripts/Runtime/FAlsInputDriver.cs](Runtime/Scripts/Runtime/FAlsInputDriver.cs)
- [Runtime/Scripts/Runtime/FAlsBootstrap.cs](Runtime/Scripts/Runtime/FAlsBootstrap.cs)

## Что внедрить в первую очередь (Phase 2)

1. Подключить `FAlsBootstrap` к Player prefab и считать `assumedBallDistance` из реального расстояния до мяча.
2. Реализовать Animator bridge (передача `FAlsActorSignals` в параметры аниматора).
3. Добавить `FootIK`/`Pelvis` слой на базе Animation Rigging и тестировать с 1 игроком.
4. Ввести `ActiveRagdoll` + `Recovery` transition и `PhysicalControl` fallback.
5. Добавить тестовую карту/сцену 1v1 и пройти DoD: walk/run/sprint/jump/land/first touch fallback.

## Быстрый старт для Unity сцены

1. Импортируйте папку `fals-unity` как локальный пакет или просто как часть Assets.
2. На геймплейном объекте персонажа повесьте:
   - `CharacterController`
   - `FAlsLocomotionMotor`
   - `FAlsController`
   - `FAlsInputDriver`
   - `FAlsBootstrap`
   - `FAlsAnimatorBridge` (если есть Animator)
3. В `FAlsBootstrap` назначьте:
   - `controller` (на тот же объект),
   - `inputDriver` (на тот же объект),
   - опционально `animatorBridge` (если нужно сразу писать параметры анимации).
4. В `FAlsAnimatorBridge` укажите Animator и убедитесь, что в контроллере существуют параметры:
   - `FALS_Grounded` (Bool)
   - `FALS_DesiredSpeed`, `FALS_Stride`, `FALS_MoveAlpha`, `FALS_Lean` (Float)
   - `FALS_Gait`, `FALS_RotationMode`, `FALS_Stance`, `FALS_Action`, `FALS_FootballAction` (Int)
   - `FALS_ActionReady` (Bool)
   - `FALS_PhysicalControl` (Float)
   - `FALS_FootLock`, `FALS_PelvisUp`, `FALS_PelvisForward`, `FALS_LeanCorrection`, `FALS_GroundAdaptation`, `FALS_Balance` (Float)
5. В `Update` происходит motor + action, в `LateUpdate` — запись параметров в Animator. Меняйте `assumedBallDistance` на живое расстояние до мяча.

## Добавить procedural debug-привязку (без IK setup)

Для проверки core-сигналов без Animation Rigging можно сразу повесить:
- `FAlsProceduralPoseDriver` на корень рига персонажа (или отдельный child-пустышку).
- назначить `pelvis`, `leftFoot`, `rightFoot` трансформы.
- в `LateUpdate` driver применяет: `PelvisOffset`, `LeftFootOffset`, `RightFootOffset` из `FAlsProceduralSignals`.

Новые параметры Animator (если используете bridge):
- `FALS_LeftFootY` (Float)
- `FALS_RightFootY` (Float)
- `FALS_LockedFoot` (Int)



## Авто-настройка в Unity Editor

1. Выберите root объект персонажа.
2. Откройте `Tools > F-ALS > Auto Setup Selected Player`.
3. Нажмите `Apply F-ALS Core Setup`.
4. Назначьте `ballTransform` вручную, если хотите live-distance вместо assumed distance.
5. Проверьте отсутствующие Animator параметры и добавьте по списку из раздела параметров.

Подробности см. [Docs/FALS_Setup_Guide.md](Docs/FALS_Setup_Guide.md).

## Минималистичный Animator reference
- См. Docs/Animator/MinimalController.md для быстрого прототипа состояний Idle/Move/Jump.
- Для отладки сигналов: добавьте FAlsSignalDebugger и включите logOnUpdate.
