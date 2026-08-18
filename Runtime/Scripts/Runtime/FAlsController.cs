using FGP.FALS.Action;
using FGP.FALS.Core;
using FGP.FALS.Motion;
using FGP.FALS.Procedural;
using FGP.FALS.Recovery;
using UnityEngine;

namespace FGP.FALS.Runtime
{
    public readonly struct FAlsActorSignals
    {
        public FAlsLocomotionState Locomotion { get; }
        public FAlsProceduralSignals Procedural { get; }
        public FAlsFootballActionOutput FootballAction { get; }
        public FAlsRecoveryOutput Recovery { get; }

        public FAlsActorSignals(
            FAlsLocomotionState locomotion,
            FAlsProceduralSignals procedural,
            FAlsFootballActionOutput footballAction,
            FAlsRecoveryOutput recovery)
        {
            Locomotion = locomotion;
            Procedural = procedural;
            FootballAction = footballAction;
            Recovery = recovery;
        }
    }

    [DisallowMultipleComponent]
    public class FAlsController : MonoBehaviour
    {
        [SerializeField] private FAlsLocomotionMotor locomotionMotor;
        [SerializeField] private float proceduralSmoothing = 18f;
        [SerializeField] private float stabilityThreshold = FAlsRecoverySolver.DefaultStabilityThreshold;
        [SerializeField] private float recoveryDuration = FAlsRecoverySolver.DefaultRecoveryDuration;
        [SerializeField] private float getUpDuration = FAlsRecoverySolver.DefaultGetUpDuration;

        private FAlsRecoveryRuntimeState _recoveryRuntime = FAlsRecoveryRuntimeState.Default;

        public FAlsActorSignals Signals { get; private set; }
        public FAlsLocomotionMotor LocomotionMotor => locomotionMotor;

        private void Reset()
        {
            locomotionMotor = GetComponent<FAlsLocomotionMotor>();
        }

        private void Awake()
        {
            _recoveryRuntime = FAlsRecoveryRuntimeState.Default;
        }

        public void SetMovementCapacity(FAlsMovementCapacity capacity)
        {
            if (locomotionMotor != null)
            {
                locomotionMotor.SetCapacity(capacity);
            }
        }

        public void ResetMovementCapacity()
        {
            if (locomotionMotor != null)
            {
                locomotionMotor.ResetCapacity();
            }
        }

        public void Tick(FAlsMotorInput motorInput, FAlsFootballActionInput actionInput, float deltaTime)
        {
            if (locomotionMotor == null)
            {
                return;
            }

            locomotionMotor.TickMotor(motorInput, deltaTime);
            var locomotion = locomotionMotor.State;
            var footballAction = FAlsFootballActionResolver.Resolve(actionInput);

            var recoveryInput = new FAlsRecoveryInput
            {
                IsGrounded = locomotion.IsGrounded,
                Velocity = locomotion.Velocity,
                GroundNormal = motorInput.GroundNormal,
                GroundedTime = locomotion.GroundedTime,
                AirTime = locomotion.AirTime,
                PhysicalControl = locomotion.PhysicalControl,
                JumpRequested = motorInput.JumpRequested,
                DeltaTime = deltaTime
            };

            var recovery = FAlsRecoverySolver.Resolve(
                ref _recoveryRuntime,
                recoveryInput,
                stabilityThreshold,
                recoveryDuration,
                getUpDuration);

            // Recovery is part of simulation state, not presentation-only data.
            locomotionMotor.SetPhysicalControl(recovery.PhysicalControl);
            locomotion = locomotionMotor.State;

            var procedural = FAlsProceduralSolver.Resolve(locomotion, proceduralSmoothing, deltaTime);
            Signals = new FAlsActorSignals(locomotion, procedural, footballAction, recovery);
        }
    }
}
