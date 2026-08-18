using FGP.FALS.Action;
using FGP.FALS.Core;
using FGP.FALS.Motion;
using FGP.FALS.Procedural;
using UnityEngine;

namespace FGP.FALS.Runtime
{
    public readonly struct FAlsActorSignals
    {
        public FAlsLocomotionState Locomotion { get; }
        public FAlsProceduralSignals Procedural { get; }
        public FAlsFootballActionOutput FootballAction { get; }

        public FAlsActorSignals(
            FAlsLocomotionState locomotion,
            FAlsProceduralSignals procedural,
            FAlsFootballActionOutput footballAction)
        {
            Locomotion = locomotion;
            Procedural = procedural;
            FootballAction = footballAction;
        }
    }

    [DisallowMultipleComponent]
    public class FAlsController : MonoBehaviour
    {
        [SerializeField] private FAlsLocomotionMotor locomotionMotor;
        [SerializeField] private float proceduralSmoothing = 18f;

        public FAlsActorSignals Signals { get; private set; }

        private void Reset()
        {
            locomotionMotor = GetComponent<FAlsLocomotionMotor>();
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
            var procedural = FAlsProceduralSolver.Resolve(locomotion, proceduralSmoothing, deltaTime);

            Signals = new FAlsActorSignals(locomotion, procedural, footballAction);
        }
    }
}
