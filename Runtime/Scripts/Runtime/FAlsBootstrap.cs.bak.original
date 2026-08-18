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
        [SerializeField] private Transform ballTransform;
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
            var actionInput = inputDriver.ReadFootballInput(CalculateBallDistance());
            controller.Tick(motorInput, actionInput, Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (animatorBridge == null)
            {
                return;
            }

            animatorBridge.Apply(controller.Signals);
        }

        private float CalculateBallDistance()
        {
            if (ballTransform == null)
            {
                return assumedBallDistance;
            }

            return Vector3.Distance(transform.position, ballTransform.position);
        }
    }
}
