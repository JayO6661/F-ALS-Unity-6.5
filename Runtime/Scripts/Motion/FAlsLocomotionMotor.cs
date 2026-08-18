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

        [Header("Rotation (ALS RefreshGroundedRotation)")]
        [SerializeField] private float rotationHalfLifeMoving = 0.1f;
        [SerializeField] private float rotationSpeedCapVelocity = 800f;
        [SerializeField] private float rotationSpeedCapView = 500f;
        [SerializeField] private float turnInPlaceSpeedCap = 180f;

        [Header("Pivot")]
        [SerializeField] private float pivotAngleThreshold = 135f;
        [SerializeField] private float pivotDuration = 0.2f;
        [SerializeField] private float pivotDecelMultiplier = 1.5f;

        [Header("Gait dynamics (replaces UE accel curve)")]
        [SerializeField] private float accelMultiplierWalk = 0.7f;
        [SerializeField] private float accelMultiplierSprint = 1.2f;

        [SerializeField] private CharacterController characterController;

        private Vector3 _velocity;
        private float _pivotTimer;
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

        /// <summary>Switches the rotation mode applied by the motor (ALS RotationMode).</summary>
        public void SetRotationMode(FAlsRotationMode mode)
        {
            _state.RotationMode = mode;
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

            var targetSpeed = FAlsMotionRules.SelectTargetSpeed(input, _state, walkSpeed, runSpeed, sprintSpeed);
            targetSpeed = _state.Mode == FAlsLocomotionMode.Grounded
                ? FAlsMotionRules.ClampByGroundSlope(targetSpeed, input.GroundNormal)
                : targetSpeed * airControl;

            _state.DesiredSpeed = Mathf.Lerp(_state.DesiredSpeed, targetSpeed, 1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));

            var inputDir = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            if (inputDir.sqrMagnitude > 1f)
            {
                inputDir.Normalize();
            }

            _state.HasInput = inputDir.sqrMagnitude > 0.0001f;

            var worldMove = Quaternion.LookRotation(transform.rotation * Vector3.forward, Vector3.up) * inputDir;
            var desiredVelocity = worldMove * _state.DesiredSpeed;
            _state.DesiredVelocity = desiredVelocity;
            _state.DesiredVelocityYawAngle = desiredVelocity.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(desiredVelocity.x, desiredVelocity.z) * Mathf.Rad2Deg
                : transform.eulerAngles.y;

            // Pivot: desired direction inverts current velocity at speed (ALS pivot).
            if (_state.Mode == FAlsLocomotionMode.Grounded && _state.HasInput &&
                _state.Velocity.magnitude > runSpeed * 0.5f &&
                Vector3.Angle(_state.Velocity, desiredVelocity) > pivotAngleThreshold)
            {
                _pivotTimer = pivotDuration;
            }
            _state.Pivoting = _pivotTimer > 0f;
            if (_pivotTimer > 0f) _pivotTimer -= deltaTime;

            // Gait amount drives accel/decel (stands in for the UE curve asset).
            _state.GaitAmount = FAlsMotionRules.GaitAmountFromSpeed(_state.DesiredSpeed, walkSpeed, runSpeed, sprintSpeed);
            float gaitT = Mathf.InverseLerp(1f, 3f, Mathf.Max(_state.GaitAmount, 1f));
            float gaitDynamics = Mathf.Lerp(accelMultiplierWalk, accelMultiplierSprint, gaitT);

            var accel = _state.DesiredSpeed > _state.Velocity.magnitude ? acceleration : deceleration;
            accel *= gaitDynamics;
            if (_state.Pivoting) accel *= pivotDecelMultiplier;

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
            _state.IsMoving = new Vector2(_velocity.x, _velocity.z).magnitude > 0.1f;
            _state.VelocityYawAngle = _state.IsMoving
                ? Mathf.Atan2(_velocity.x, _velocity.z) * Mathf.Rad2Deg
                : transform.eulerAngles.y;
            _state.Gait = FAlsMotionRules.SelectGait(_state.DesiredSpeed, walkSpeed, runSpeed);
            _state.Stance = input.Crouch ? FAlsStance.Crouching : FAlsStance.Standing;
            _state.StrideBlend = Mathf.InverseLerp(walkSpeed, sprintSpeed, _state.DesiredSpeed);
            _state.MoveAlpha = Mathf.Clamp01(input.MoveInput.magnitude);
            _state.PhysicalControl = Mathf.Lerp(_state.PhysicalControl, 1f, 6f * deltaTime);
            _state.FootLockAlpha = Mathf.Clamp01(1f - Vector2.Distance(input.MoveInput, Vector2.zero) * 0.4f);
            ApplyLean(worldMove, deltaTime);

            RefreshGroundedRotation(input, deltaTime);
        }

        /// <summary>
        /// ALS RefreshGroundedRotation semantics: extra-smooth body yaw toward the
        /// desired velocity (VelocityDirection) or view (ViewDirection/Aiming),
        /// sprint rotates to velocity, idle turn-in-place past the yaw threshold.
        /// </summary>
        private void RefreshGroundedRotation(FAlsMotorInput input, float deltaTime)
        {
            _state.TurningInPlace = false;
            _state.TurnInPlaceYawOffset = 0f;

            float rotationScale = Mathf.Clamp01(input.RotationScale);
            if (_state.Mode != FAlsLocomotionMode.Grounded || rotationScale <= 0f)
            {
                _state.RotationRate = 0f;
                return;
            }

            float bodyYaw = transform.eulerAngles.y;
            float viewYaw = input.ViewDirection.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(input.ViewDirection.x, input.ViewDirection.z) * Mathf.Rad2Deg
                : bodyYaw;

            float newYaw = bodyYaw;
            bool movingOrInput = _state.IsMoving || _state.HasInput;

            if (movingOrInput)
            {
                float targetYaw;
                float cap;
                if (_state.RotationMode == FAlsRotationMode.VelocityDirection)
                {
                    targetYaw = _state.DesiredVelocityYawAngle;
                    cap = rotationSpeedCapVelocity;
                }
                else
                {
                    // ALS: sprinting rotates to velocity even in view/aim modes.
                    targetYaw = _state.Gait == FAlsGait.Sprinting ? _state.VelocityYawAngle : viewYaw;
                    cap = rotationSpeedCapView;
                }
                newYaw = ExtraSmoothYaw(bodyYaw, targetYaw, rotationHalfLifeMoving, cap * rotationScale, deltaTime);
            }
            else if (_state.RotationMode != FAlsRotationMode.VelocityDirection)
            {
                // Idle: turn in place toward the view past the threshold.
                float offset = FAlsMotionRules.TurnInPlaceCheck(viewYaw, bodyYaw);
                if (Mathf.Abs(offset) > 0.01f)
                {
                    float step = Mathf.Clamp(offset, -turnInPlaceSpeedCap * deltaTime, turnInPlaceSpeedCap * deltaTime);
                    newYaw = bodyYaw + step * rotationScale;
                    _state.TurningInPlace = true;
                    _state.TurnInPlaceYawOffset = offset;
                }
            }
            // Idle + VelocityDirection: hold last yaw (ALS keeps the last target).

            _state.RotationRate = deltaTime > 0f ? Mathf.DeltaAngle(bodyYaw, newYaw) / deltaTime : 0f;
            if (Mathf.Abs(_state.RotationRate) > 0.01f)
            {
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
        }

        /// <summary>ALS SetRotationExtraSmooth: exponential half-life approach with a rate cap.</summary>
        private static float ExtraSmoothYaw(float current, float target, float halfLife, float maxRateDegPerSec, float deltaTime)
        {
            float delta = Mathf.DeltaAngle(current, target);
            if (Mathf.Abs(delta) < 0.01f) return target;

            float t = halfLife > 0.0001f ? 1f - Mathf.Pow(0.5f, deltaTime / halfLife) : 1f;
            float step = Mathf.Clamp(delta * t, -maxRateDegPerSec * deltaTime, maxRateDegPerSec * deltaTime);
            return current + step;
        }

        private void ApplyLean(Vector3 worldMove, float deltaTime)
        {
            var targetLean = worldMove.magnitude * leanByTurn;
            _state.Lean = Mathf.Lerp(_state.Lean, targetLean, 1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));
        }
    }
}
