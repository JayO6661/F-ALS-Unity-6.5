using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Recovery
{
    /// <summary>
    /// ALS-style Active Ragdoll + Recovery pipeline.
    /// 
    /// Domains:
    /// - PhysicalControl (0..1): blend между animated и ragdoll позой.
    /// - Stability: оценка устойчивости по COM, base of support, velocity.
    /// - RecoveryState: None → Falling → GroundedRecovery → GetUp → Standing.
    /// 
    /// Принцип: motor продолжает работать, но при instability PhysicalControl
    /// падает до 0, и поза определяется ragdoll-физикой. При recovery — 
    /// procedural get-up анимация с постепенным возвратом контроля.
    /// </summary>
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

    public struct FAlsRecoveryOutput
    {
        public FAlsRecoveryState State;
        public float PhysicalControl; // 0 = full ragdoll, 1 = full animated
        public float Stability;       // 0..1 оценка устойчивости
        public bool RequestGetUp;
        public string DebugHint;
    }

    public static class FAlsRecoverySolver
    {
        public const float DefaultStabilityThreshold = 0.25f;
        public const float DefaultRecoveryDuration = 0.8f;
        public const float DefaultGetUpDuration = 0.6f;

        public static FAlsRecoveryOutput Resolve(FAlsRecoveryInput input, 
            float stabilityThreshold = DefaultStabilityThreshold,
            float recoveryDuration = DefaultRecoveryDuration,
            float getUpDuration = DefaultGetUpDuration)
        {
            var output = new FAlsRecoveryOutput
            {
                State = FAlsRecoveryState.None,
                PhysicalControl = input.PhysicalControl,
                Stability = 1f,
                RequestGetUp = false,
                DebugHint = "stable"
            };

            // Оценка stability по velocity + ground normal + air time.
            float speed = new Vector2(input.Velocity.x, input.Velocity.z).magnitude;
            float slopePenalty = 1f - Mathf.Clamp01(Vector3.Dot(input.GroundNormal, Vector3.up));
            float airPenalty = input.IsGrounded ? 0f : Mathf.Min(1f, input.AirTime * 0.8f);
            
            output.Stability = Mathf.Clamp01(1f - (speed * 0.08f + slopePenalty * 0.6f + airPenalty));

            if (!input.IsGrounded && input.AirTime > 0.4f)
            {
                output.State = FAlsRecoveryState.Falling;
                output.PhysicalControl = Mathf.Lerp(input.PhysicalControl, 0.2f, input.DeltaTime * 8f);
                output.DebugHint = "falling";
            }
            else if (output.Stability < stabilityThreshold && input.IsGrounded)
            {
                output.State = FAlsRecoveryState.GroundedRecovery;
                output.PhysicalControl = Mathf.Lerp(input.PhysicalControl, 0f, input.DeltaTime * 6f);
                output.RequestGetUp = true;
                output.DebugHint = "unstable, request get up";
            }
            else if (input.IsGrounded && input.GroundedTime > getUpDuration && output.Stability >= stabilityThreshold)
            {
                output.State = FAlsRecoveryState.Standing;
                output.PhysicalControl = Mathf.Lerp(input.PhysicalControl, 1f, input.DeltaTime * 4f);
                output.DebugHint = "standing recovered";
            }
            else
            {
                output.State = FAlsRecoveryState.None;
                output.PhysicalControl = Mathf.Lerp(input.PhysicalControl, 1f, input.DeltaTime * 3f);
            }

            return output;
        }
    }
}
