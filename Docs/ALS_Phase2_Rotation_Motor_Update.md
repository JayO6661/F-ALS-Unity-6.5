# Phase 2: ALS-Style Rotation and Motion Softening (Unity Motor)

Ниже — прямой next-step после текущей карты `ALS_Refactored_Research_Map.md`.

## Что изменено

- `FAlsLocomotionMotor` переписан в более ALS-похожий flow:
  - поддержка режимов `FAlsRotationMode` (`ViewDirection`, `VelocityDirection`, `Aiming`);
  - сглаживание поворота через `FAlsMath.DampAngle` и `Mathf.MoveTowardsAngle`;
  - отдельные коэффициенты поворота для `Grounded` и `InAir`.
- Добавлен `FAlsMath`:
  - `Damp`, `DampAngle`, `ToYaw`, `LerpAngle`.

## Что проверять в текущей версии

1. В сцене персонажа повесьте обновлённый `FAlsLocomotionMotor`.
2. Задайте `rotationMode = VelocityDirection`, если нужно ориентирование по движению.
3. Задайте `rotationMode = ViewDirection` для ориентирования по камере/виду.
4. Проверьте:
   - разгон/торможение,
   - плавный поворот при резкой смене Input,
   - отсутствие резких рывков высоты поворота (провокация `deltaTime` jitter),
   - корректный `Action = Jump` на `JumpRequested` grounded только.

## Резервные файлы

- До стабилизации можно временно хранить:
  - `Runtime/Scripts/Motion/FAlsLocomotionMotor.cs.bak`
  - `Runtime/Scripts/Motion/FAlsLocomotionMotor_new.cs`

После финальной синхронизации их можно удалить.
