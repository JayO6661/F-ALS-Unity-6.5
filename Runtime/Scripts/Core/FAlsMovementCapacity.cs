using UnityEngine;

namespace FGP.FALS.Core
{
    /// <summary>
    /// Runtime locomotion capability envelope applied on top of the base F-ALS profile.
    /// Higher-level gameplay systems (skills, stamina, injury, possession, surface)
    /// may compose this value without owning input, motor state or animation.
    /// </summary>
    [System.Serializable]
    public struct FAlsMovementCapacity
    {
        [Min(0f)] public float Speed;
        [Min(0f)] public float Acceleration;
        [Min(0f)] public float Deceleration;
        [Min(0f)] public float TurnRate;
        [Range(0f, 1f)] public float AirControl;

        public static FAlsMovementCapacity Identity => new FAlsMovementCapacity
        {
            Speed = 1f,
            Acceleration = 1f,
            Deceleration = 1f,
            TurnRate = 1f,
            AirControl = 1f
        };

        public FAlsMovementCapacity Sanitized()
        {
            return new FAlsMovementCapacity
            {
                Speed = Mathf.Max(0f, Speed),
                Acceleration = Mathf.Max(0f, Acceleration),
                Deceleration = Mathf.Max(0f, Deceleration),
                TurnRate = Mathf.Max(0f, TurnRate),
                AirControl = Mathf.Clamp01(AirControl)
            };
        }

        public static FAlsMovementCapacity Combine(FAlsMovementCapacity a, FAlsMovementCapacity b)
        {
            a = a.Sanitized();
            b = b.Sanitized();
            return new FAlsMovementCapacity
            {
                Speed = a.Speed * b.Speed,
                Acceleration = a.Acceleration * b.Acceleration,
                Deceleration = a.Deceleration * b.Deceleration,
                TurnRate = a.TurnRate * b.TurnRate,
                AirControl = a.AirControl * b.AirControl
            };
        }
    }
}
