using UnityEngine;

namespace FGP.FALS.Core
{
    public enum FAlsLocomotionMode
    {
        Grounded,
        InAir
    }

    public enum FAlsRotationMode
    {
        VelocityDirection,
        ViewDirection,
        Aiming
    }

    public enum FAlsStance
    {
        Standing,
        Crouching
    }

    public enum FAlsGait
    {
        Walking,
        Running,
        Sprinting
    }

    public enum FAlsLocomotionAction
    {
        None,
        Strafe,
        Roll,
        Jump,
        Mantle,
        Ragdoll,
        GetUp
    }

    public struct FAlsMotorInput
    {
        public Vector2 MoveInput;
        public bool Sprint;
        public bool Crouch;
        public bool JumpRequested;
        public bool SprintPressed;
        public Vector3 ViewDirection;
        public Vector3 VelocityInput;
        public Vector3 GroundNormal;

        // ALS rotation contract (additive)
        public FAlsRotationMode DesiredRotationMode;
        public bool AimHeld;
        /// <summary>0 disables body rotation this tick (action locks), 1 = full rate. Must be set explicitly.</summary>
        public float RotationScale;

        public static FAlsMotorInput None => new FAlsMotorInput
        {
            MoveInput = Vector2.zero,
            Sprint = false,
            Crouch = false,
            JumpRequested = false,
            SprintPressed = false,
            ViewDirection = Vector3.forward,
            VelocityInput = Vector3.zero,
            GroundNormal = Vector3.up,
            DesiredRotationMode = FAlsRotationMode.VelocityDirection,
            AimHeld = false,
            RotationScale = 1f
        };
    }

    public struct FAlsMotionParams
    {
        public float WalkSpeed;
        public float RunSpeed;
        public float SprintSpeed;
        public float Acceleration;
        public float Deceleration;
        public float TurnRate;
        public float AirControl;
        public float JumpImpulse;
    }

    public struct FAlsLocomotionState
    {
        public FAlsLocomotionMode Mode;
        public FAlsRotationMode RotationMode;
        public FAlsStance Stance;
        public FAlsGait Gait;
        public FAlsLocomotionAction Action;
        public Vector3 Velocity;
        public float DesiredSpeed;
        public Vector3 DesiredVelocity;
        public float MoveAlpha;
        public float StrideBlend;
        public float Lean;
        public float FootLockAlpha;
        public float PhysicalControl;
        public bool IsGrounded;
        public float GroundedTime;
        public float AirTime;

        // ALS movement mechanics (additive)
        /// <summary>Continuous gait: 0 standing, 1 walking, 2 running, 3 sprinting.</summary>
        public float GaitAmount;
        public bool IsMoving;
        public bool HasInput;
        public float VelocityYawAngle;
        public float DesiredVelocityYawAngle;
        public bool TurningInPlace;
        public float TurnInPlaceYawOffset;
        public bool Pivoting;
        /// <summary>Actual body yaw rate this tick, deg/s (for animation curves).</summary>
        public float RotationRate;
    }
}
