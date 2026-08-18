using UnityEngine;

namespace FGP.FALS.Core
{
    public static class FAlsMath
    {
        public static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }

        public static float LerpAngle(float from, float to, float alpha)
        {
            return Mathf.LerpAngle(from, to, alpha);
        }

        public static float DampAngle(float current, float target, float halfLife, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return current;
            }

            if (halfLife <= 0f)
            {
                return target;
            }

            var k = Mathf.Exp(-Mathf.Log(2f) * deltaTime / halfLife);
            return target + (current - target) * k;
        }

        public static float Damp(float current, float target, float halfLife, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return current;
            }

            if (halfLife <= 0f)
            {
                return target;
            }

            var k = Mathf.Exp(-Mathf.Log(2f) * deltaTime / halfLife);
            return target + (current - target) * k;
        }

        public static float ToYaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
