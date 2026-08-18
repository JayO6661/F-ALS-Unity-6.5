using FGP.FALS.Core;

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
        public float LeftFootReachDistance;
        public float RightFootReachDistance;
    }

    public struct FAlsFootballActionOutput
    {
        public FAlsFootballActionType ActionType;
        public bool IsActionReady;
        public bool BallContactExpected;
        public string DebugHint;
    }

    public static class FAlsFootballActionResolver
    {
        public static FAlsFootballActionOutput Resolve(FAlsFootballActionInput input, bool allowQuickFallback = true, bool allowEmergencyFallback = true)
        {
            if (!input.ShotPressed)
            {
                return new FAlsFootballActionOutput
                {
                    ActionType = FAlsFootballActionType.None,
                    IsActionReady = false
                };
            }

            if (input.BallDistance <= input.PreparedDistance)
            {
                return new FAlsFootballActionOutput
                {
                    ActionType = FAlsFootballActionType.PreparedKick,
                    IsActionReady = true,
                    BallContactExpected = true,
                    DebugHint = "prepared kick"
                };
            }

            if (allowQuickFallback && input.BallDistance <= input.QuickDistance)
            {
                return new FAlsFootballActionOutput
                {
                    ActionType = FAlsFootballActionType.QuickKick,
                    IsActionReady = true,
                    BallContactExpected = true,
                    DebugHint = "quick kick"
                };
            }

            if (allowQuickFallback && input.BallDistance <= input.ReachDistance)
            {
                return new FAlsFootballActionOutput
                {
                    ActionType = FAlsFootballActionType.ReachKick,
                    IsActionReady = true,
                    BallContactExpected = true,
                    DebugHint = "extended reach"
                };
            }

            if (allowEmergencyFallback && (input.LeftFootReachDistance > 0f || input.RightFootReachDistance > 0f))
            {
                var actionType = input.LeftFootReachDistance >= input.RightFootReachDistance
                    ? FAlsFootballActionType.ToePoke
                    : FAlsFootballActionType.StretchTouch;

                return new FAlsFootballActionOutput
                {
                    ActionType = actionType,
                    IsActionReady = true,
                    BallContactExpected = true,
                    DebugHint = "emergency fallback"
                };
            }

            return new FAlsFootballActionOutput
            {
                ActionType = FAlsFootballActionType.Miss,
                IsActionReady = false,
                BallContactExpected = false,
                DebugHint = "no viable shot path"
            };
        }
    }
}
