using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Motion
{
    public static class FAlsMotionRules
    {
        public const float DefaultWalkSpeed = 1.75f;
        public const float DefaultRunSpeed = 3.75f;
        public const float DefaultSprintSpeed = 6.5f;
        public const float SprintConeDeg = 50f;
        public const float TurnInPlaceThresholdDeg = 50f;

        [System.Obsolete("Use the overload with explicit gait speeds (motor fields).")]
        public static float SelectTargetSpeed(FAlsMotorInput input, FAlsLocomotionState state)
        {
            return SelectTargetSpeed(input, state, DefaultWalkSpeed, DefaultRunSpeed, DefaultSprintSpeed);
        }

        public static float SelectTargetSpeed(
            FAlsMotorInput input,
            FAlsLocomotionState state,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed)
        {
            float magnitude = Mathf.Clamp01(input.MoveInput.magnitude);
            if (magnitude < 0.01f)
            {
                return 0f;
            }

            if (input.Crouch)
            {
                return walkSpeed * 0.8f * magnitude;
            }

            if (input.Sprint && CanSprint(input, state))
            {
                return sprintSpeed * magnitude;
            }

            // Analog input below the walk/run threshold scales walking speed;
            // stronger input requests running speed without changing motor authority.
            return magnitude < 0.45f
                ? walkSpeed * Mathf.InverseLerp(0.01f, 0.45f, magnitude)
                : Mathf.Lerp(walkSpeed, runSpeed, Mathf.InverseLerp(0.45f, 1f, magnitude));
        }

        public static bool CanSprint(FAlsMotorInput input, FAlsLocomotionState state)
        {
            if (!input.Sprint || input.MoveInput.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float inputYaw = Mathf.Atan2(input.MoveInput.x, input.MoveInput.y) * Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(inputYaw, 0f)) <= SprintConeDeg;
        }

        public static FAlsGait SelectGait(float targetSpeed)
        {
            return SelectGait(targetSpeed, DefaultWalkSpeed, DefaultRunSpeed);
        }

        public static FAlsGait SelectGait(float targetSpeed, float walkSpeed, float runSpeed)
        {
            if (targetSpeed < (walkSpeed + runSpeed) * 0.5f)
            {
                return FAlsGait.Walking;
            }

            return targetSpeed < runSpeed + 0.8f ? FAlsGait.Running : FAlsGait.Sprinting;
        }

        public static float GaitAmountFromSpeed(float speed, float walkSpeed, float runSpeed, float sprintSpeed)
        {
            if (speed < 0.05f) return 0f;
            if (speed <= walkSpeed) return Mathf.Lerp(0.5f, 1f, speed / Mathf.Max(walkSpeed, 0.01f));
            if (speed <= runSpeed) return Mathf.Lerp(1f, 2f, (speed - walkSpeed) / Mathf.Max(runSpeed - walkSpeed, 0.01f));
            return Mathf.Lerp(2f, 3f, Mathf.Clamp01((speed - runSpeed) / Mathf.Max(sprintSpeed - runSpeed, 0.01f)));
        }

        public static float TurnInPlaceCheck(float viewYaw, float bodyYaw)
        {
            float delta = Mathf.DeltaAngle(bodyYaw, viewYaw);
            return Mathf.Abs(delta) > TurnInPlaceThresholdDeg ? delta : 0f;
        }

        public static float ClampByGroundSlope(float speed, Vector3 groundNormal)
        {
            if (groundNormal.sqrMagnitude < 0.0001f)
            {
                return speed;
            }

            float upness = Vector3.Dot(groundNormal.normalized, Vector3.up);
            if (upness >= 1f)
            {
                return speed;
            }

            return speed * Mathf.Lerp(0.78f, 1f, Mathf.Clamp01((upness + 0.2f) / 0.8f));
        }
    }
}
