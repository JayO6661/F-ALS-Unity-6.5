using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Motion
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class FAlsLocomotionMotor : MonoBehaviour
    {
        [Header("Motion Rules")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.8f;
        [SerializeField] private float sprintSpeed = 7.4f;
        [SerializeField] private float acceleration = 22f;
        [SerializeField] private float deceleration = 26f;
        [SerializeField] private float airControl = 0.35f;

        [Header("Body tuning")]
        [SerializeField] private float leanByTurn = 12f;
        [SerializeField] private float speedBlendSmoothing = 16f;

        [SerializeField] private CharacterController characterController;

        private Vector3 _velocity;
        private FAlsLocomotionState _state = new FAlsLocomotionState
        {
            Mode = FAlsLocomotionMode.Grounded,
            RotationMode = FAlsRotationMode.ViewDirection,
            Stance = FAlsStance.Standing,
            Action = FAlsLocomotionAction.None,
            IsGrounded = true,
            MoveAlpha = 1f,
            PhysicalControl = 1f
        };

        public FAlsLocomotionState State => _state;

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void TickMotor(FAlsMotorInput input, float deltaTime)
        {
            if (deltaTime <= 0f || characterController == null)
            {
                return;
            }

            var wasGrounded = _state.IsGrounded;
            _state.IsGrounded = characterController.isGrounded;
            _state.Mode = _state.IsGrounded ? FAlsLocomotionMode.Grounded : FAlsLocomotionMode.InAir;

            if (_state.IsGrounded != wasGrounded)
            {
                _state.GroundedTime = 0f;
                _state.AirTime = 0f;
            }

            if (_state.Mode == FAlsLocomotionMode.Grounded)
            {
                _state.GroundedTime += deltaTime;
                _state.AirTime = 0f;
            }
            else
            {
                _state.AirTime += deltaTime;
                _state.GroundedTime = 0f;
            }

            var targetSpeed = FAlsMotionRules.SelectTargetSpeed(input, _state);
            targetSpeed = _state.Mode == FAlsLocomotionMode.Grounded
                ? FAlsMotionRules.ClampByGroundSlope(targetSpeed, input.GroundNormal)
                : targetSpeed * airControl;

            _state.DesiredSpeed = Mathf.Lerp(_state.DesiredSpeed, targetSpeed, 1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));

            var inputDir = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            if (inputDir.sqrMagnitude > 1f)
            {
                inputDir.Normalize();
            }

            var worldMove = Quaternion.LookRotation(transform.rotation * Vector3.forward, Vector3.up) * inputDir;
            var desiredVelocity = worldMove * _state.DesiredSpeed;
            _state.DesiredVelocity = desiredVelocity;

            var accel = _state.DesiredSpeed > _state.Velocity.magnitude ? acceleration : deceleration;
            _velocity = Vector3.MoveTowards(_velocity, desiredVelocity, accel * deltaTime);

            if (_state.Mode == FAlsLocomotionMode.InAir)
            {
                _velocity.y += Physics.gravity.y * deltaTime;
            }

            if (_state.Mode == FAlsLocomotionMode.Grounded && input.JumpRequested)
            {
                _velocity.y = 6.5f;
                _state.Action = FAlsLocomotionAction.Jump;
                _state.IsGrounded = false;
            }
            else if (_state.Mode == FAlsLocomotionMode.Grounded)
            {
                _velocity.y = -1f;
                _state.Action = FAlsLocomotionAction.None;
            }
            else
            {
                _state.Action = FAlsLocomotionAction.None;
            }

            characterController.Move(_velocity * deltaTime);

            _state.Velocity = _velocity;
            _state.Gait = FAlsMotionRules.SelectGait(_state.DesiredSpeed);
            _state.Stance = input.Crouch ? FAlsStance.Crouching : FAlsStance.Standing;
            _state.StrideBlend = Mathf.InverseLerp(walkSpeed, sprintSpeed, _state.DesiredSpeed);
            _state.MoveAlpha = Mathf.Clamp01(input.MoveInput.magnitude);
            _state.PhysicalControl = Mathf.Lerp(_state.PhysicalControl, 1f, 6f * deltaTime);
            _state.FootLockAlpha = Mathf.Clamp01(1f - Vector2.Distance(input.MoveInput, Vector2.zero) * 0.4f);
            ApplyLean(worldMove, deltaTime);
        }

        private void ApplyLean(Vector3 worldMove, float deltaTime)
        {
            var targetLean = worldMove.magnitude * leanByTurn;
            _state.Lean = Mathf.Lerp(_state.Lean, targetLean, 1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));
        }
    }
}
