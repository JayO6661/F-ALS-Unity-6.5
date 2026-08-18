namespace FGP.FALS.Action
{
    public enum FAlsFootballActionType
    {
        None,
        PreparedKick,
        QuickKick,
        ReachKick,
        ToePoke,
        StretchTouch,
        LungeTouch,
        Miss
    }

    public struct FAlsFootballActionInput
    {
        public bool ShotPressed;
        public float BallDistance;
        public float PreparedDistance;
        public float QuickDistance;
        public float ReachDistance;

        // Optional live distances from the ball to each foot. Negative means unavailable.
        public float LeftFootBallDistance;
        public float RightFootBallDistance;
        public float EmergencyFootReachDistance;
    }

    public struct FAlsFootballActionOutput
    {
        public FAlsFootballActionType ActionType;
        public bool IsActionReady;
        public bool BallContactExpected;
        public bool UseLeftFoot;
        public string DebugHint;
    }

    public static class FAlsFootballActionResolver
    {
        public static FAlsFootballActionOutput Resolve(
            FAlsFootballActionInput input,
            bool allowQuickFallback = true,
            bool allowEmergencyFallback = true)
        {
            if (!input.ShotPressed)
            {
                return None();
            }

            if (input.BallDistance >= 0f && input.BallDistance <= input.PreparedDistance)
            {
                return Ready(FAlsFootballActionType.PreparedKick, false, "prepared kick");
            }

            if (allowQuickFallback && input.BallDistance >= 0f && input.BallDistance <= input.QuickDistance)
            {
                return Ready(FAlsFootballActionType.QuickKick, false, "quick kick");
            }

            if (allowQuickFallback && input.BallDistance >= 0f && input.BallDistance <= input.ReachDistance)
            {
                return Ready(FAlsFootballActionType.ReachKick, false, "extended reach");
            }

            if (allowEmergencyFallback && input.EmergencyFootReachDistance > 0f)
            {
                bool leftValid = input.LeftFootBallDistance >= 0f &&
                                 input.LeftFootBallDistance <= input.EmergencyFootReachDistance;
                bool rightValid = input.RightFootBallDistance >= 0f &&
                                  input.RightFootBallDistance <= input.EmergencyFootReachDistance;

                if (leftValid || rightValid)
                {
                    bool useLeft = leftValid && (!rightValid || input.LeftFootBallDistance <= input.RightFootBallDistance);
                    float footDistance = useLeft ? input.LeftFootBallDistance : input.RightFootBallDistance;
                    float normalizedReach = footDistance / input.EmergencyFootReachDistance;
                    var action = normalizedReach <= 0.55f
                        ? FAlsFootballActionType.ToePoke
                        : FAlsFootballActionType.StretchTouch;

                    return Ready(action, useLeft, "emergency foot reach");
                }
            }

            return new FAlsFootballActionOutput
            {
                ActionType = FAlsFootballActionType.Miss,
                IsActionReady = false,
                BallContactExpected = false,
                UseLeftFoot = false,
                DebugHint = "no viable contact path"
            };
        }

        private static FAlsFootballActionOutput None()
        {
            return new FAlsFootballActionOutput
            {
                ActionType = FAlsFootballActionType.None,
                IsActionReady = false,
                BallContactExpected = false,
                UseLeftFoot = false,
                DebugHint = "no action requested"
            };
        }

        private static FAlsFootballActionOutput Ready(
            FAlsFootballActionType action,
            bool useLeftFoot,
            string hint)
        {
            return new FAlsFootballActionOutput
            {
                ActionType = action,
                IsActionReady = true,
                BallContactExpected = true,
                UseLeftFoot = useLeftFoot,
                DebugHint = hint
            };
        }
    }
}
