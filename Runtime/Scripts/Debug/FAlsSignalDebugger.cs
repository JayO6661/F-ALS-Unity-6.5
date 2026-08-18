using UnityEngine;

using FGP.FALS.Runtime;

namespace FGP.FALS.Debugging
{
    public class FAlsSignalDebugger : MonoBehaviour
    {
        [SerializeField] private FAlsController controller;
        [SerializeField] private float logInterval = 0.25f;
        [SerializeField] private bool logOnUpdate;

        private float _nextLog;

        private void Reset()
        {
            controller = GetComponent<FAlsController>();
        }

        private void Update()
        {
            if (controller == null || !logOnUpdate)
            {
                return;
            }

            if (Time.unscaledTime < _nextLog)
            {
                return;
            }

            _nextLog = Time.unscaledTime + logInterval;
            LogSignals();
        }

        public void LogSignals()
        {
            if (controller == null)
            {
                return;
            }

            var l = controller.Signals.Locomotion;
            var p = controller.Signals.Procedural;
            var a = controller.Signals.FootballAction;

            Debug.Log($"[FALS] grounded={l.IsGrounded} mode={l.Mode} gait={l.Gait} action={l.Action} speed={l.DesiredSpeed:F2} move={l.MoveAlpha:F2} stride={l.StrideBlend:F2} lean={l.Lean:F2} footLock={p.FootLock:F2} bal={p.Balance:F2} lockedFoot={p.LockedFoot} action={a.ActionType} ready={a.IsActionReady} ball={a.BallContactExpected}");
        }
    }
}
