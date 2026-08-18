using FGP.FALS.Action;
using FGP.FALS.Core;
using FGP.FALS.Procedural;
using UnityEngine;

namespace FGP.FALS.Runtime
{
    [DisallowMultipleComponent]
    public class FAlsAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("Locomotion Params")]
        [SerializeField] private string modeGroundedParam = "FALS_Grounded";
        [SerializeField] private string desiredSpeedParam = "FALS_DesiredSpeed";
        [SerializeField] private string strideParam = "FALS_Stride";
        [SerializeField] private string moveAlphaParam = "FALS_MoveAlpha";
        [SerializeField] private string leanParam = "FALS_Lean";
        [SerializeField] private string gaitParam = "FALS_Gait";
        [SerializeField] private string rotationModeParam = "FALS_RotationMode";
        [SerializeField] private string stanceParam = "FALS_Stance";

        [Header("Action Params")]
        [SerializeField] private string locomotionActionParam = "FALS_Action";
        [SerializeField] private string footballActionParam = "FALS_FootballAction";
        [SerializeField] private string actionReadyParam = "FALS_ActionReady";
        [SerializeField] private string physicalControlParam = "FALS_PhysicalControl";

        [Header("Procedural Params")]
        [SerializeField] private string footLockParam = "FALS_FootLock";
        [SerializeField] private string pelvisUpParam = "FALS_PelvisUp";
        [SerializeField] private string pelvisForwardParam = "FALS_PelvisForward";
        [SerializeField] private string leanCorrectionParam = "FALS_LeanCorrection";
        [SerializeField] private string groundAdaptationParam = "FALS_GroundAdaptation";
        [SerializeField] private string balanceParam = "FALS_Balance";
        [SerializeField] private string leftFootYParam = "FALS_LeftFootY";
        [SerializeField] private string rightFootYParam = "FALS_RightFootY";
        [SerializeField] private string lockedFootParam = "FALS_LockedFoot";

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        public void Apply(FAlsActorSignals signals)
        {
            if (animator == null)
            {
                return;
            }

            var locomotion = signals.Locomotion;
            var procedural = signals.Procedural;
            var football = signals.FootballAction;

            animator.SetBool(modeGroundedParam, locomotion.IsGrounded);
            animator.SetFloat(desiredSpeedParam, locomotion.DesiredSpeed);
            animator.SetFloat(strideParam, locomotion.StrideBlend);
            animator.SetFloat(moveAlphaParam, locomotion.MoveAlpha);
            animator.SetFloat(leanParam, locomotion.Lean);

            animator.SetInteger(gaitParam, ToInt(locomotion.Gait));
            animator.SetInteger(rotationModeParam, ToInt(locomotion.RotationMode));
            animator.SetInteger(stanceParam, ToInt(locomotion.Stance));
            animator.SetInteger(locomotionActionParam, ToInt(locomotion.Action));

            animator.SetInteger(footballActionParam, ToInt(football.ActionType));
            animator.SetBool(actionReadyParam, football.IsActionReady);
            animator.SetFloat(physicalControlParam, locomotion.PhysicalControl);

            animator.SetFloat(footLockParam, procedural.FootLock);
            animator.SetFloat(pelvisUpParam, procedural.PelvisOffset.y);
            animator.SetFloat(pelvisForwardParam, procedural.PelvisOffset.z);
            animator.SetFloat(leanCorrectionParam, procedural.LeanCorrection);
            animator.SetFloat(groundAdaptationParam, procedural.GroundAdaptation);
            animator.SetFloat(balanceParam, procedural.Balance);
            animator.SetFloat(leftFootYParam, procedural.LeftFootOffset.y);
            animator.SetFloat(rightFootYParam, procedural.RightFootOffset.y);
            animator.SetInteger(lockedFootParam, ToInt(procedural.LockedFoot));
        }

        private static int ToInt(System.Enum value)
        {
            return System.Convert.ToInt32(value);
        }
    }
}
