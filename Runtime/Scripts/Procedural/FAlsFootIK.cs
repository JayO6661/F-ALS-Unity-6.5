using FGP.FALS.Core;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace FGP.FALS.Procedural
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Runtime.FAlsController))]
    public class FAlsFootIK : MonoBehaviour
    {
        [Header("Rig Setup")]
        [SerializeField] private RigBuilder rigBuilder;
        [SerializeField] private Rig leftFootRig;
        [SerializeField] private Rig rightFootRig;
        [SerializeField] private Rig pelvisRig;

        [Header("IK Targets")]
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform rightFootTarget;
        [SerializeField] private Transform pelvisTarget;

        [Header("Foot Lock Settings")]
        [SerializeField] private float lockBlendSpeed = 15f;

        [Header("Ground Adaptation")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float raycastDistance = 1.5f;
        [SerializeField] private float footRayOffset = 0.1f;
        [SerializeField] private float smoothAdaptation = 10f;

        [Header("Balance")]
        [SerializeField] private float balanceSmoothing = 8f;
        [SerializeField] private float maxPelvisTilt = 15f;
        [SerializeField] private float maxPelvisShift = 0.2f;

        private Runtime.FAlsController _controller;
        private Vector3 _leftFootLockPosition;
        private Vector3 _rightFootLockPosition;
        private float _leftFootLockWeight;
        private float _rightFootLockWeight;
        private Quaternion _leftFootLockRotation;
        private Quaternion _rightFootLockRotation;
        private Vector3 _pelvisBasePosition;
        private float _groundAdaptationLeft;
        private float _groundAdaptationRight;
        private bool _leftFootLocked;
        private bool _rightFootLocked;

        private void Awake()
        {
            _controller = GetComponent<Runtime.FAlsController>();
            CacheBasePose();
            if (rigBuilder != null) rigBuilder.Build();
        }

        private void Reset()
        {
            rigBuilder = GetComponent<RigBuilder>();
        }

        private void CacheBasePose()
        {
            if (pelvisTarget != null) _pelvisBasePosition = pelvisTarget.localPosition;
        }

        private void LateUpdate()
        {
            if (_controller == null) return;

            var procedural = _controller.Signals.Procedural;
            var locomotion = _controller.Signals.Locomotion;
            float deltaTime = Time.deltaTime;

            UpdateFootLock(procedural, locomotion, deltaTime);
            ApplyGroundAdaptation(deltaTime);
            ApplyBalanceCorrection(locomotion, deltaTime);
            UpdateRigWeights(procedural);
        }

        private void UpdateFootLock(FAlsProceduralSignals procedural, FAlsLocomotionState locomotion, float deltaTime)
        {
            float lockBlend = 1f - Mathf.Exp(-lockBlendSpeed * deltaTime);

            bool shouldLockLeft = procedural.LockedFoot == FAlsLockedFoot.Left || (procedural.FootLock > 0.5f && !locomotion.HasInput);
            if (shouldLockLeft && !_leftFootLocked && leftFootTarget != null)
            {
                _leftFootLockPosition = leftFootTarget.position;
                _leftFootLockRotation = leftFootTarget.rotation;
                _leftFootLocked = true;
            }
            else if (!shouldLockLeft)
            {
                _leftFootLocked = false;
            }

            if (_leftFootLocked && leftFootTarget != null)
            {
                _leftFootLockWeight = Mathf.Lerp(_leftFootLockWeight, 1f, lockBlend);
                leftFootTarget.position = Vector3.Lerp(leftFootTarget.position, _leftFootLockPosition, _leftFootLockWeight);
                leftFootTarget.rotation = Quaternion.Slerp(leftFootTarget.rotation, _leftFootLockRotation, _leftFootLockWeight);
            }
            else
            {
                _leftFootLockWeight = Mathf.Lerp(_leftFootLockWeight, 0f, lockBlend);
            }

            bool shouldLockRight = procedural.LockedFoot == FAlsLockedFoot.Right || (procedural.FootLock > 0.5f && !locomotion.HasInput);
            if (shouldLockRight && !_rightFootLocked && rightFootTarget != null)
            {
                _rightFootLockPosition = rightFootTarget.position;
                _rightFootLockRotation = rightFootTarget.rotation;
                _rightFootLocked = true;
            }
            else if (!shouldLockRight)
            {
                _rightFootLocked = false;
            }

            if (_rightFootLocked && rightFootTarget != null)
            {
                _rightFootLockWeight = Mathf.Lerp(_rightFootLockWeight, 1f, lockBlend);
                rightFootTarget.position = Vector3.Lerp(rightFootTarget.position, _rightFootLockPosition, _rightFootLockWeight);
                rightFootTarget.rotation = Quaternion.Slerp(rightFootTarget.rotation, _rightFootLockRotation, _rightFootLockWeight);
            }
            else
            {
                _rightFootLockWeight = Mathf.Lerp(_rightFootLockWeight, 0f, lockBlend);
            }
        }

        private void ApplyGroundAdaptation(float deltaTime)
        {
            float smooth = 1f - Mathf.Exp(-smoothAdaptation * deltaTime);

            if (leftFootTarget != null)
            {
                Vector3 origin = leftFootTarget.position + Vector3.up * footRayOffset;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
                    _groundAdaptationLeft = Mathf.Lerp(_groundAdaptationLeft, hit.point.y - leftFootTarget.position.y, smooth);
                else
                    _groundAdaptationLeft = Mathf.Lerp(_groundAdaptationLeft, 0f, smooth);
            }

            if (rightFootTarget != null)
            {
                Vector3 origin = rightFootTarget.position + Vector3.up * footRayOffset;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
                    _groundAdaptationRight = Mathf.Lerp(_groundAdaptationRight, hit.point.y - rightFootTarget.position.y, smooth);
                else
                    _groundAdaptationRight = Mathf.Lerp(_groundAdaptationRight, 0f, smooth);
            }

            if (pelvisTarget != null)
            {
                float lowerFoot = Mathf.Min(_groundAdaptationLeft, _groundAdaptationRight);
                Vector3 target = _pelvisBasePosition + Vector3.up * lowerFoot;
                pelvisTarget.localPosition = Vector3.Lerp(pelvisTarget.localPosition, target, smooth);
            }
        }

        private void ApplyBalanceCorrection(FAlsLocomotionState locomotion, float deltaTime)
        {
            if (pelvisTarget == null) return;
            float smooth = 1f - Mathf.Exp(-balanceSmoothing * deltaTime);
            float velocityMagnitude = new Vector2(locomotion.Velocity.x, locomotion.Velocity.z).magnitude;
            float leanFactor = Mathf.Clamp01(velocityMagnitude / 10f);
            Vector3 moveDir = locomotion.Velocity.normalized;

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                float tiltAngle = maxPelvisTilt * leanFactor;
                Quaternion tiltRotation = Quaternion.Euler(0f, 0f, -tiltAngle * Mathf.Sign(moveDir.x));
                pelvisTarget.localRotation = Quaternion.Slerp(pelvisTarget.localRotation, tiltRotation, smooth);
            }

            Vector3 supportShift = Vector3.zero;
            if (_leftFootLocked && !_rightFootLocked) supportShift = Vector3.right * maxPelvisShift * 0.5f;
            else if (_rightFootLocked && !_leftFootLocked) supportShift = Vector3.left * maxPelvisShift * 0.5f;

            pelvisTarget.localPosition = Vector3.Lerp(pelvisTarget.localPosition, _pelvisBasePosition + supportShift + Vector3.up * Mathf.Min(_groundAdaptationLeft, _groundAdaptationRight), smooth);
        }

        private void UpdateRigWeights(FAlsProceduralSignals procedural)
        {
            float t = Time.deltaTime * 10f;
            if (leftFootRig != null) leftFootRig.weight = Mathf.Lerp(leftFootRig.weight, procedural.FootLock, t);
            if (rightFootRig != null) rightFootRig.weight = Mathf.Lerp(rightFootRig.weight, procedural.FootLock, t);
            if (pelvisRig != null) pelvisRig.weight = Mathf.Lerp(pelvisRig.weight, procedural.GroundAdaptation, t);
        }

        public bool IsLeftFootLocked => _leftFootLocked;
        public bool IsRightFootLocked => _rightFootLocked;
    }
}
