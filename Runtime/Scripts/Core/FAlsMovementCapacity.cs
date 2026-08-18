using UnityEngine;

namespace FGP.FALS.Core
{
    /// <summary>
    /// Runtime locomotion capability envelope supplied by higher-level gameplay systems.
    /// Values are multipliers over the base F-ALS tuning.
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
    }
}
