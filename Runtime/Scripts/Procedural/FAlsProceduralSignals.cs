using UnityEngine;

namespace FGP.FALS.Procedural
{
    public enum FAlsFootId
    {
        Left = 0,
        Right = 1
    }

    public enum FAlsLockedFoot
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    public struct FAlsProceduralSignals
    {
        public float FootLock;
        public Vector3 PelvisOffset;
        public float LeanCorrection;
        public float GroundAdaptation;
        public float Balance;
        public Vector3 LeftFootOffset;
        public Vector3 RightFootOffset;
        public FAlsLockedFoot LockedFoot;
    }

    public static class FAlsProceduralSolver
    {
        public static FAlsProceduralSignals Resolve(
            FGP.FALS.Core.FAlsLocomotionState locomotion,
            float smoothing,
            float deltaTime)
        {
            var balance = locomotion.PhysicalControl;
            var speedFactor = Mathf.Clamp01(locomotion.DesiredSpeed / 8f);

            var walkCycle = speedFactor > 0.001f
                ? Mathf.Repeat(locomotion.GroundedTime * Mathf.Lerp(1.4f, 4f, speedFactor), 1f)
                : 0f;

            var leftPhase = Mathf.Cos(walkCycle * Mathf.PI * 2f);
            var rightPhase = Mathf.Cos((walkCycle + 0.5f) * Mathf.PI * 2f);
            var footLift = Mathf.Lerp(0.05f, 0.14f, speedFactor);

            var strideL = Mathf.Max(0f, leftPhase) * footLift;
            var strideR = Mathf.Max(0f, rightPhase) * footLift;

            var footLock = Mathf.Lerp(locomotion.FootLockAlpha, 1f - speedFactor, 0.8f * speedFactor);
            var groundBlend = locomotion.Mode == FGP.FALS.Core.FAlsLocomotionMode.Grounded ? 1f : Mathf.Max(0.2f, Mathf.Lerp(1f, 0.2f, locomotion.AirTime));

            var pelvisUp = Mathf.Max(0f, Mathf.Cos(speedFactor * Mathf.PI * 0.5f)) * 0.08f;
            var pelvisForward = locomotion.IsGrounded ? Mathf.Sin(locomotion.StrideBlend * Mathf.PI * 0.5f) * 0.06f : 0f;
            var pelvisOffset = new Vector3(0f, pelvisUp, pelvisForward);

            var leftFootOffset = new Vector3(0f, -footLock * 0.01f + (locomotion.IsGrounded ? 0f : -0.03f), -leanToOffset(locomotion.Lean) * 0.01f) + new Vector3(0f, strideL, 0f) * groundBlend;
            var rightFootOffset = new Vector3(0f, -footLock * 0.01f + (locomotion.IsGrounded ? 0f : -0.03f), leanToOffset(locomotion.Lean) * 0.01f) + new Vector3(0f, strideR, 0f) * groundBlend;

            var lockedFoot = leftPhase > rightPhase ? FAlsLockedFoot.Left : FAlsLockedFoot.Right;

            return new FAlsProceduralSignals
            {
                FootLock = footLock,
                PelvisOffset = pelvisOffset,
                LeanCorrection = locomotion.Lean * 0.4f,
                GroundAdaptation = groundBlend,
                Balance = Mathf.Lerp(0.8f, 1f, balance),
                LeftFootOffset = leftFootOffset,
                RightFootOffset = rightFootOffset,
                LockedFoot = lockedFoot
            };
        }

        private static float leanToOffset(float lean)
        {
            return Mathf.Clamp(lean * 0.02f, -1f, 1f);
        }
    }
}
