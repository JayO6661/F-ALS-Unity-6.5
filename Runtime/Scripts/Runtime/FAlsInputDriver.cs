using FGP.FALS.Action;
using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Runtime
{
    [DisallowMultipleComponent]
    public class FAlsInputDriver : MonoBehaviour
    {
        [Header("Legacy input bindings")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchKey = KeyCode.C;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode shotKey = KeyCode.Mouse0;

        [Header("Action tuning")]
        [SerializeField] private float preparedDistance = 1.0f;
        [SerializeField] private float quickDistance = 1.8f;
        [SerializeField] private float reachDistance = 2.6f;
        [SerializeField] private float leftReach = 0.95f;
        [SerializeField] private float rightReach = 0.9f;

        public FAlsMotorInput ReadMotorInput()
        {
            return new FAlsMotorInput
            {
                MoveInput = new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis)),
                Sprint = Input.GetKey(sprintKey),
                Crouch = Input.GetKey(crouchKey),
                JumpRequested = Input.GetKeyDown(jumpKey),
                SprintPressed = Input.GetKeyDown(sprintKey),
                ViewDirection = transform.forward,
                VelocityInput = Vector3.zero,
                GroundNormal = Vector3.up,
                DesiredRotationMode = FAlsRotationMode.VelocityDirection,
                AimHeld = false,
                RotationScale = 1f
            };
        }

        public FAlsFootballActionInput ReadFootballInput(float ballDistance)
        {
            return new FAlsFootballActionInput
            {
                ShotPressed = Input.GetKeyDown(shotKey),
                BallDistance = ballDistance,
                PreparedDistance = preparedDistance,
                QuickDistance = quickDistance,
                ReachDistance = reachDistance,
                LeftFootReachDistance = leftReach,
                RightFootReachDistance = rightReach
            };
        }
    }
}
