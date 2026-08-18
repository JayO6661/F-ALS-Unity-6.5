using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Recovery
{
    public enum FAlsRecoveryState
    {
        None,
        Falling,
        GroundedRecovery,
        GetUp,
        Standing
    }

    public struct FAlsRecoveryInput
    {
        public bool IsGrounded;
        public Vector3 Velocity;
        public Vector3 GroundNormal;
        public float GroundedTime;
        public float AirTime;
        public float PhysicalControl;
        public bool JumpRequested;
        public float DeltaTime;
    }

    public struct FAlsRecoveryRuntimeState
    {
        public FAlsRecoveryState State;
        public float StateTime;
        public float PhysicalControl;

        public static FAlsRecoveryRuntimeState Default => new FAlsRecoveryRuntimeState
        {
            State = FAlsRecoveryState.None,
            StateTime = 0f,
            PhysicalControl = 1f
        };
    }

    public struct FAlsRecoveryOutput
    {
        public FAlsRecoveryState State;
        public float PhysicalControl;
        public float Stability;
        public bool RequestGetUp;
        public string DebugHint;
    }

    public static class FAlsRecoverySolver
    {
        public const float DefaultStabilityThreshold = 0.25f;
        public const float DefaultRecoveryDuration = 0.8f;
        public const float DefaultGetUpDuration = 0.6f;

        public static FAlsRecoveryOutput Resolve(
            ref FAlsRecoveryRuntimeState runtime,
            FAlsRecoveryInput input,
            float stabilityThreshold = DefaultStabilityThreshold,
            float recoveryDuration = DefaultRecoveryDuration,
            float getUpDuration = DefaultGetUpDuration)
        {
            float dt = Mathf.Max(0f, input.DeltaTime);
            float stability = CalculateStability(input);
            var nextState = runtime.State;

            if (!input.IsGrounded && input.AirTime > 0.4f)
            {
                nextState = FAlsRecoveryState.Falling;
            }
            else if (input.IsGrounded)
            {
                if (runtime.State == FAlsRecoveryState.Falling || stability < stabilityThreshold)
                    nextState = FAlsRecoveryState.GroundedRecovery;
                else if (runtime.State == FAlsRecoveryState.GroundedRecovery && runtime.StateTime >= recoveryDuration)
                    nextState = FAlsRecoveryState.GetUp;
                else if (runtime.State == FAlsRecoveryState.GetUp && runtime.StateTime >= getUpDuration)
                    nextState = FAlsRecoveryState.Standing;
                else if (runtime.State == FAlsRecoveryState.Standing && runtime.StateTime >= 0.1f)
                    nextState = FAlsRecoveryState.None;
            }

            if (nextState != runtime.State)
            {
                runtime.State = nextState;
                runtime.StateTime = 0f;
            }
            else
            {
                runtime.StateTime += dt;
            }

            float targetControl;
            bool requestGetUp;
            string hint;

            switch (runtime.State)
            {
                case FAlsRecoveryState.Falling:
                    targetControl = 0.2f;
                    requestGetUp = false;
                    hint = "falling";
                    break;
                case FAlsRecoveryState.GroundedRecovery:
                    targetControl = 0f;
                    requestGetUp = true;
                    hint = "grounded recovery";
                    break;
                case FAlsRecoveryState.GetUp:
                    targetControl = Mathf.Clamp01(runtime.StateTime / Mathf.Max(getUpDuration, 0.01f));
                    requestGetUp = true;
                    hint = "get up";
                    break;
                case FAlsRecoveryState.Standing:
                    targetControl = 1f;
                    requestGetUp = false;
                    hint = "standing";
                    break;
                default:
                    targetControl = 1f;
                    requestGetUp = false;
                    hint = "stable";
                    break;
            }

            runtime.PhysicalControl = Mathf.MoveTowards(runtime.PhysicalControl, targetControl, 6f * dt);

            return new FAlsRecoveryOutput
            {
                State = runtime.State,
                PhysicalControl = Mathf.Clamp01(runtime.PhysicalControl),
                Stability = stability,
                RequestGetUp = requestGetUp,
                DebugHint = hint
            };
        }

        public static float CalculateStability(FAlsRecoveryInput input)
        {
            float horizontalSpeed = new Vector2(input.Velocity.x, input.Velocity.z).magnitude;
            float upness = input.GroundNormal.sqrMagnitude > 0.0001f
                ? Vector3.Dot(input.GroundNormal.normalized, Vector3.up)
                : 1f;
            float slopePenalty = 1f - Mathf.Clamp01(upness);
            float airPenalty = input.IsGrounded ? 0f : Mathf.Min(1f, input.AirTime * 0.8f);
            return Mathf.Clamp01(1f - (horizontalSpeed * 0.08f + slopePenalty * 0.6f + airPenalty));
        }
    }
}
