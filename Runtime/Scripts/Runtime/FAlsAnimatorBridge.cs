using System.Collections.Generic;
using FGP.FALS.Recovery;
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

        [Header("Recovery Params")]
        [SerializeField] private string recoveryStateParam = "FALS_RecoveryState";
        [SerializeField] private string stabilityParam = "FALS_Stability";
        [SerializeField] private string requestGetUpParam = "FALS_RequestGetUp";

        private readonly HashSet<int> _availableParameters = new HashSet<int>();

        private void Awake()
        {
            RebuildParameterCache();
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        private void OnValidate()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        public void RebuildParameterCache()
        {
            _availableParameters.Clear();
            if (animator == null) return;

            foreach (var parameter in animator.parameters)
            {
                _availableParameters.Add(parameter.nameHash);
            }
        }

        public void Apply(FAlsActorSignals signals)
        {
            if (animator == null) return;
            if (_availableParameters.Count == 0 && animator.runtimeAnimatorController != null) RebuildParameterCache();

            var locomotion = signals.Locomotion;
            var procedural = signals.Procedural;
            var football = signals.FootballAction;
            var recovery = signals.Recovery;

            SetBool(modeGroundedParam, locomotion.IsGrounded);
            SetFloat(desiredSpeedParam, locomotion.DesiredSpeed);
            SetFloat(strideParam, locomotion.StrideBlend);
            SetFloat(moveAlphaParam, locomotion.MoveAlpha);
            SetFloat(leanParam, locomotion.Lean);

            SetInteger(gaitParam, (int)locomotion.Gait);
            SetInteger(rotationModeParam, (int)locomotion.RotationMode);
            SetInteger(stanceParam, (int)locomotion.Stance);
            SetInteger(locomotionActionParam, (int)locomotion.Action);

            SetInteger(footballActionParam, (int)football.ActionType);
            SetBool(actionReadyParam, football.IsActionReady);

            float physicalControl = recovery.State != FAlsRecoveryState.None
                ? recovery.PhysicalControl
                : locomotion.PhysicalControl;
            SetFloat(physicalControlParam, physicalControl);

            SetFloat(footLockParam, procedural.FootLock);
            SetFloat(pelvisUpParam, procedural.PelvisOffset.y);
            SetFloat(pelvisForwardParam, procedural.PelvisOffset.z);
            SetFloat(leanCorrectionParam, procedural.LeanCorrection);
            SetFloat(groundAdaptationParam, procedural.GroundAdaptation);
            SetFloat(balanceParam, procedural.Balance);
            SetFloat(leftFootYParam, procedural.LeftFootOffset.y);
            SetFloat(rightFootYParam, procedural.RightFootOffset.y);
            SetInteger(lockedFootParam, (int)procedural.LockedFoot);

            SetInteger(recoveryStateParam, (int)recovery.State);
            SetFloat(stabilityParam, recovery.Stability);
            SetBool(requestGetUpParam, recovery.RequestGetUp);
        }

        private bool Has(string parameterName)
        {
            return !string.IsNullOrEmpty(parameterName) && _availableParameters.Contains(Animator.StringToHash(parameterName));
        }

        private void SetFloat(string parameterName, float value)
        {
            if (Has(parameterName)) animator.SetFloat(parameterName, value);
        }

        private void SetInteger(string parameterName, int value)
        {
            if (Has(parameterName)) animator.SetInteger(parameterName, value);
        }

        private void SetBool(string parameterName, bool value)
        {
            if (Has(parameterName)) animator.SetBool(parameterName, value);
        }
    }
}
