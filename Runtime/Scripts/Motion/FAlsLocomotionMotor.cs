using FGP.FALS.Core;
using UnityEngine;

namespace FGP.FALS.Motion
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class FAlsLocomotionMotor : MonoBehaviour
    {
        [Header("Base motion profile")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.8f;
        [SerializeField] private float sprintSpeed = 7.4f;
        [SerializeField] private float acceleration = 22f;
        [SerializeField] private float deceleration = 26f;
        [SerializeField] private float airControl = 0.35f;
        [SerializeField] private float jumpImpulse = 6.5f;

        [Header("Body tuning")]
        [SerializeField] private float leanByTurn = 12f;
        [SerializeField] private float speedBlendSmoothing = 16f;

        [Header("Rotation")]
        [SerializeField] private float rotationHalfLifeMoving = 0.1f;
        [SerializeField] private float rotationSpeedCapVelocity = 800f;
        [SerializeField] private float rotationSpeedCapView = 500f;
        [SerializeField] private float turnInPlaceSpeedCap = 180f;

        [Header("Pivot")]
        [SerializeField] private float pivotAngleThreshold = 135f;
        [SerializeField] private float pivotDuration = 0.2f;
        [SerializeField] private float pivotDecelMultiplier = 1.5f;

        [Header("Gait dynamics")]
        [SerializeField] private float accelMultiplierWalk = 0.7f;
        [SerializeField] private float accelMultiplierSprint = 1.2f;

        [SerializeField] private CharacterController characterController;

        private Vector3 _velocity;
        private float _pivotTimer;
        private FAlsMovementCapacity _capacity = FAlsMovementCapacity.Identity;
        private FAlsLocomotionState _state = new FAlsLocomotionState
        {
            Mode = FAlsLocomotionMode.Grounded,
            RotationMode = FAlsRotationMode.VelocityDirection,
            Stance = FAlsStance.Standing,
            Action = FAlsLocomotionAction.None,
            IsGrounded = true,
            MoveAlpha = 1f,
            PhysicalControl = 1f
        };

        public FAlsLocomotionState State => _state;
        public FAlsMovementCapacity Capacity => _capacity;
        public bool IsReady => characterController != null && characterController.enabled;

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        public void SetRotationMode(FAlsRotationMode mode)
        {
            _state.RotationMode = mode;
        }

        public void SetCapacity(FAlsMovementCapacity capacity)
        {
            _capacity = capacity.Sanitized();
        }

        public void ResetCapacity()
        {
            _capacity = FAlsMovementCapacity.Identity;
        }

        public void SetPhysicalControl(float physicalControl)
        {
            _state.PhysicalControl = Mathf.Clamp01(physicalControl);
        }

        public void TickMotor(FAlsMotorInput input, float deltaTime)
        {
            if (deltaTime <= 0f || characterController == null || !characterController.enabled)
            {
                return;
            }

            var capacity = _capacity.Sanitized();
            float effectiveWalkSpeed = walkSpeed * capacity.Speed;
            float effectiveRunSpeed = runSpeed * capacity.Speed;
            float effectiveSprintSpeed = sprintSpeed * capacity.Speed;

            _state.RotationMode = input.AimHeld ? FAlsRotationMode.Aiming : input.DesiredRotationMode;

            bool wasGrounded = _state.IsGrounded;
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

            float targetSpeed = FAlsMotionRules.SelectTargetSpeed(
                input,
                _state,
                effectiveWalkSpeed,
                effectiveRunSpeed,
                effectiveSprintSpeed);

            targetSpeed = _state.Mode == FAlsLocomotionMode.Grounded
                ? FAlsMotionRules.ClampByGroundSlope(targetSpeed, input.GroundNormal)
                : targetSpeed * airControl * capacity.AirControl;

            _state.DesiredSpeed = Mathf.Lerp(
                _state.DesiredSpeed,
                targetSpeed,
                1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));

            Vector3 inputDir = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            if (inputDir.sqrMagnitude > 1f)
            {
                inputDir.Normalize();
            }

            _state.HasInput = inputDir.sqrMagnitude > 0.0001f;

            Vector3 worldMove = transform.rotation * inputDir;
            worldMove.y = 0f;
            if (worldMove.sqrMagnitude > 1f)
            {
                worldMove.Normalize();
            }

            Vector3 desiredVelocity = worldMove * _state.DesiredSpeed;
            _state.DesiredVelocity = desiredVelocity;
            _state.DesiredVelocityYawAngle = desiredVelocity.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(desiredVelocity.x, desiredVelocity.z) * Mathf.Rad2Deg
                : transform.eulerAngles.y;

            if (_state.Mode == FAlsLocomotionMode.Grounded &&
                _state.HasInput &&
                new Vector2(_state.Velocity.x, _state.Velocity.z).magnitude > effectiveRunSpeed * 0.5f &&
                Vector3.Angle(new Vector3(_state.Velocity.x, 0f, _state.Velocity.z), desiredVelocity) > pivotAngleThreshold)
            {
                _pivotTimer = pivotDuration;
            }

            _state.Pivoting = _pivotTimer > 0f;
            if (_pivotTimer > 0f)
            {
                _pivotTimer = Mathf.Max(0f, _pivotTimer - deltaTime);
            }

            _state.GaitAmount = FAlsMotionRules.GaitAmountFromSpeed(
                _state.DesiredSpeed,
                effectiveWalkSpeed,
                effectiveRunSpeed,
                effectiveSprintSpeed);

            float gaitT = Mathf.InverseLerp(1f, 3f, Mathf.Max(_state.GaitAmount, 1f));
            float gaitDynamics = Mathf.Lerp(accelMultiplierWalk, accelMultiplierSprint, gaitT);
            float horizontalSpeed = new Vector2(_state.Velocity.x, _state.Velocity.z).magnitude;
            bool accelerating = _state.DesiredSpeed > horizontalSpeed;
            float linearRate = accelerating
                ? acceleration * capacity.Acceleration
                : deceleration * capacity.Deceleration;
            linearRate *= gaitDynamics;
            if (_state.Pivoting)
            {
                linearRate *= pivotDecelMultiplier;
            }

            Vector3 horizontalVelocity = new Vector3(_velocity.x, 0f, _velocity.z);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, linearRate * deltaTime);
            _velocity.x = horizontalVelocity.x;
            _velocity.z = horizontalVelocity.z;

            if (_state.Mode == FAlsLocomotionMode.InAir)
            {
                _velocity.y += Physics.gravity.y * deltaTime;
            }

            if (_state.Mode == FAlsLocomotionMode.Grounded && input.JumpRequested)
            {
                _velocity.y = jumpImpulse;
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
            _state.Gait = FAlsMotionRules.SelectGait(_state.DesiredSpeed, effectiveWalkSpeed, effectiveRunSpeed);
            _state.Stance = input.Crouch ? FAlsStance.Crouching : FAlsStance.Standing;
            _state.StrideBlend = Mathf.InverseLerp(effectiveWalkSpeed, Mathf.Max(effectiveSprintSpeed, effectiveWalkSpeed + 0.01f), _state.DesiredSpeed);
            _state.MoveAlpha = Mathf.Clamp01(input.MoveInput.magnitude);
            _state.FootLockAlpha = Mathf.Clamp01(1f - input.MoveInput.magnitude * 0.4f);

            ApplyLean(worldMove, deltaTime);
            RefreshGroundedRotation(input, capacity.TurnRate, deltaTime);
        }

        private void RefreshGroundedRotation(FAlsMotorInput input, float turnRateScale, float deltaTime)
        {
            _state.TurningInPlace = false;
            _state.TurnInPlaceYawOffset = 0f;

            float rotationScale = Mathf.Clamp01(input.RotationScale) * Mathf.Max(0f, turnRateScale);
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
                    targetYaw = _state.Gait == FAlsGait.Sprinting ? _state.VelocityYawAngle : viewYaw;
                    cap = rotationSpeedCapView;
                }

                newYaw = ExtraSmoothYaw(bodyYaw, targetYaw, rotationHalfLifeMoving, cap * rotationScale, deltaTime);
            }
            else if (_state.RotationMode != FAlsRotationMode.VelocityDirection)
            {
                float offset = FAlsMotionRules.TurnInPlaceCheck(viewYaw, bodyYaw);
                if (Mathf.Abs(offset) > 0.01f)
                {
                    float step = Mathf.Clamp(offset, -turnInPlaceSpeedCap * rotationScale * deltaTime, turnInPlaceSpeedCap * rotationScale * deltaTime);
                    newYaw = bodyYaw + step;
                    _state.TurningInPlace = true;
                    _state.TurnInPlaceYawOffset = offset;
                }
            }

            _state.RotationRate = deltaTime > 0f ? Mathf.DeltaAngle(bodyYaw, newYaw) / deltaTime : 0f;
            if (Mathf.Abs(_state.RotationRate) > 0.01f)
            {
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
        }

        private static float ExtraSmoothYaw(float current, float target, float halfLife, float maxRateDegPerSec, float deltaTime)
        {
            float delta = Mathf.DeltaAngle(current, target);
            if (Mathf.Abs(delta) < 0.01f)
            {
                return target;
            }

            float t = halfLife > 0.0001f ? 1f - Mathf.Pow(0.5f, deltaTime / halfLife) : 1f;
            float step = Mathf.Clamp(delta * t, -maxRateDegPerSec * deltaTime, maxRateDegPerSec * deltaTime);
            return current + step;
        }

        private void ApplyLean(Vector3 worldMove, float deltaTime)
        {
            float targetLean = worldMove.magnitude * leanByTurn;
            _state.Lean = Mathf.Lerp(_state.Lean, targetLean, 1f - Mathf.Exp(-speedBlendSmoothing * deltaTime));
        }
    }
}
