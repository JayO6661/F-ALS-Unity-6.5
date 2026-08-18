using UnityEngine;

namespace FGP.FALS.Procedural
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Runtime.FAlsController))]
    public class FAlsProceduralPoseDriver : MonoBehaviour
    {
        [SerializeField] private Runtime.FAlsController controller;
        [SerializeField] private Transform pelvis;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;

        [Header("Local-space tuning")]
        [SerializeField] private float smoothing = 22f;

        private Vector3 _pelvisBaseLocal;
        private Vector3 _leftFootBaseLocal;
        private Vector3 _rightFootBaseLocal;
        private bool _initialized;

        private void Awake()
        {
            CacheBasePose();
        }

        private void Reset()
        {
            controller = GetComponent<Runtime.FAlsController>();
        }

        private void LateUpdate()
        {
            if (controller == null)
            {
                return;
            }

            if (!_initialized)
            {
                CacheBasePose();
            }

            var procedural = controller.Signals.Procedural;
            var smooth = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

            if (pelvis != null)
            {
                pelvis.localPosition = Vector3.Lerp(pelvis.localPosition, _pelvisBaseLocal + procedural.PelvisOffset, smooth);
            }

            if (leftFoot != null)
            {
                leftFoot.localPosition = Vector3.Lerp(leftFoot.localPosition, _leftFootBaseLocal + procedural.LeftFootOffset, smooth);
            }

            if (rightFoot != null)
            {
                rightFoot.localPosition = Vector3.Lerp(rightFoot.localPosition, _rightFootBaseLocal + procedural.RightFootOffset, smooth);
            }
        }

        private void CacheBasePose()
        {
            _pelvisBaseLocal = pelvis != null ? pelvis.localPosition : Vector3.zero;
            _leftFootBaseLocal = leftFoot != null ? leftFoot.localPosition : Vector3.zero;
            _rightFootBaseLocal = rightFoot != null ? rightFoot.localPosition : Vector3.zero;
            _initialized = true;
        }
    }
}
