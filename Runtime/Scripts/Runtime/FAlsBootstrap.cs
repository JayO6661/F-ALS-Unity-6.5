using FGP.FALS.Action;
using UnityEngine;

namespace FGP.FALS.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FAlsController), typeof(FAlsInputDriver))]
    public class FAlsBootstrap : MonoBehaviour
    {
        [SerializeField] private FAlsController controller;
        [SerializeField] private FAlsInputDriver inputDriver;
        [SerializeField] private FAlsAnimatorBridge animatorBridge;

        [Header("Football context")]
        [SerializeField] private Transform ballTransform;
        [SerializeField] private Transform leftFootTransform;
        [SerializeField] private Transform rightFootTransform;
        [SerializeField] private float assumedBallDistance = 2.2f;

        private void Reset()
        {
            controller = GetComponent<FAlsController>();
            inputDriver = GetComponent<FAlsInputDriver>();
            animatorBridge = GetComponent<FAlsAnimatorBridge>();
        }

        private void Update()
        {
            if (controller == null || inputDriver == null)
            {
                return;
            }

            var motorInput = inputDriver.ReadMotorInput();
            var actionInput = BuildFootballActionInput();
            controller.Tick(motorInput, actionInput, Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (animatorBridge != null && controller != null)
            {
                animatorBridge.Apply(controller.Signals);
            }
        }

        private FAlsFootballActionInput BuildFootballActionInput()
        {
            float ballDistance = CalculateDistance(transform, ballTransform, assumedBallDistance);
            float leftFootBallDistance = CalculateDistance(leftFootTransform, ballTransform, -1f);
            float rightFootBallDistance = CalculateDistance(rightFootTransform, ballTransform, -1f);

            return inputDriver.ReadFootballInput(
                ballDistance,
                leftFootBallDistance,
                rightFootBallDistance);
        }

        private static float CalculateDistance(Transform from, Transform to, float fallback)
        {
            if (from == null || to == null)
            {
                return fallback;
            }

            return Vector3.Distance(from.position, to.position);
        }
    }
}
