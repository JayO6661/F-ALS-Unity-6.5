using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace FGP.FALS.Procedural
{
    /// <summary>
    /// Foot IK implementation using Unity Animation Rigging.
    /// 
    /// Provides:
    /// - Foot locking when foot should stay planted (e.g., during stride)
    /// - Ground adaptation (vertical offset based on terrain)
    /// - Pelvis height adjustment for uneven ground
    /// - Balance correction based on velocity/lean
    /// 
    /// Requirements:
    /// - Animation Rigging package installed
    /// - RigBuilder component on character root
    /// - TwoBoneIKConstraint for each leg
    /// - MultiAimConstraint or Override for pelvis
    /// </summary>
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
        [SerializeField] private float lockDistanceThreshold = 0.15f;
        [SerializeField] private float unlockHeightThreshold = 0.3f;

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
            
            if (rigBuilder != null)
            {
                rigBuilder.Build();
            }
        }

        private void Reset()
        {
            rigBuilder = GetComponent<RigBuilder>();
        }

        private void CacheBasePose()
        {
            if (pelvisTarget != null)
            {
                _pelvisBasePosition = pelvisTarget.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (_controller == null) return;

            var procedural = _controller.Signals.Procedural;
            var locomotion = _controller.Signals.Locomotion;
            float deltaTime = Time.deltaTime;

            // Update foot lock states
            UpdateFootLock(procedural, locomotion, deltaTime);

            // Apply ground adaptation
            ApplyGroundAdaptation(deltaTime);

            // Apply balance correction
            ApplyBalanceCorrection(locomotion, deltaTime);

            // Update rig weights
            UpdateRigWeights(procedural);
        }

        private void UpdateFootLock(FAlsProceduralSignals procedural, FAlsLocomotionState locomotion, float deltaTime)
        {
            float lockBlend = 1f - Mathf.Exp(-lockBlendSpeed * deltaTime);

            // Left foot lock
            bool shouldLockLeft = procedural.LockedFoot == FAlsLockedFoot.Left || 
                                  (procedural.FootLock > 0.5f && !locomotion.HasInput);
            
            if (shouldLockLeft && !_leftFootLocked && leftFootTarget != null)
            {
                _leftFootLockPosition = leftFootTarget.position;
                _leftFootLockRotation = leftFootTarget.rotation;
                _leftFootLocked = true;
            }
            else if (!shouldLockLeft && _leftFootLocked)
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

            // Right foot lock
            bool shouldLockRight = procedural.LockedFoot == FAlsLockedFoot.Right || 
                                   (procedural.FootLock > 0.5f && !locomotion.HasInput);
            
            if (shouldLockRight && !_rightFootLocked && rightFootTarget != null)
            {
                _rightFootLockPosition = rightFootTarget.position;
                _rightFootLockRotation = rightFootTarget.rotation;
                _rightFootLocked = true;
            }
            else if (!shouldLockRight && _rightFootLocked)
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

            // Left foot ground detection
            if (leftFootTarget != null)
            {
                Vector3 leftFootRayOrigin = leftFootTarget.position + Vector3.up * footRayOffset;
                if (Physics.Raycast(leftFootRayOrigin, Vector3.down, out RaycastHit leftHit, raycastDistance, groundLayers))
                {
                    _groundAdaptationLeft = Mathf.Lerp(_groundAdaptationLeft, leftHit.distance - footRayOffset, smooth);
                }
                else
                {
                    _groundAdaptationLeft = Mathf.Lerp(_groundAdaptationLeft, 0f, smooth);
                }
            }

            // Right foot ground detection
            if (rightFootTarget != null)
            {
                Vector3 rightFootRayOrigin = rightFootTarget.position + Vector3.up * footRayOffset;
                if (Physics.Raycast(rightFootRayOrigin, Vector3.down, out RaycastHit rightHit, raycastDistance, groundLayers))
                {
                    _groundAdaptationRight = Mathf.Lerp(_groundAdaptationRight, rightHit.distance - footRayOffset, smooth);
                }
                else
                {
                    _groundAdaptationRight = Mathf.Lerp(_groundAdaptationRight, 0f, smooth);
                }
            }

            // Apply pelvis adaptation (average of both feet)
            if (pelvisTarget != null)
            {
                float avgAdaptation = (_groundAdaptationLeft + _groundAdaptationRight) * 0.5f;
                Vector3 adaptedPelvisPos = _pelvisBasePosition + Vector3.up * avgAdaptation;
                pelvisTarget.localPosition = Vector3.Lerp(pelvisTarget.localPosition, adaptedPelvisPos, smooth);
            }
        }

        private void ApplyBalanceCorrection(FAlsLocomotionState locomotion, float deltaTime)
        {
            if (pelvisTarget == null) return;

            float smooth = 1f - Mathf.Exp(-balanceSmoothing * deltaTime);
            
            // Calculate lean-based tilt
            float velocityMagnitude = new Vector2(locomotion.Velocity.x, locomotion.Velocity.z).magnitude;
            float leanFactor = Mathf.Clamp01(velocityMagnitude / 10f);
            
            // Tilt pelvis opposite to movement direction for balance
            Vector3 moveDir = locomotion.Velocity.normalized;
            if (moveDir.magnitude > 0.01f)
            {
                float tiltAngle = maxPelvisTilt * leanFactor;
                Quaternion tiltRotation = Quaternion.Euler(0, 0, -tiltAngle * Mathf.Sign(moveDir.x));
                pelvisTarget.localRotation = Quaternion.Slerp(pelvisTarget.localRotation, tiltRotation, smooth);
            }

            // Shift pelvis toward support foot when one foot is locked
            if (_leftFootLocked && !_rightFootLocked)
            {
                Vector3 shift = Vector3.right * maxPelvisShift * 0.5f;
                pelvisTarget.localPosition = Vector3.Lerp(pelvisTarget.localPosition, _pelvisBasePosition + shift, smooth);
            }
            else if (_rightFootLocked && !_leftFootLocked)
            {
                Vector3 shift = Vector3.left * maxPelvisShift * 0.5f;
                pelvisTarget.localPosition = Vector3.Lerp(pelvisTarget.localPosition, _pelvisBasePosition + shift, smooth);
            }
        }

        private void UpdateRigWeights(FAlsProceduralSignals procedural)
        {
            float ikWeight = procedural.FootLock;
            
            if (leftFootRig.IsValid())
            {
                leftFootRig.weight = Mathf.Lerp(leftFootRig.weight, ikWeight, Time.deltaTime * 10f);
            }
            
            if (rightFootRig.IsValid())
            {
                rightFootRig.weight = Mathf.Lerp(rightFootRig.weight, ikWeight, Time.deltaTime * 10f);
            }
            
            if (pelvisRig.IsValid())
            {
                pelvisRig.weight = Mathf.Lerp(pelvisRig.weight, procedural.GroundAdaptation, Time.deltaTime * 10f);
            }
        }

        public void SetFootLock(FAlsLockedFoot foot)
        {
            // Can be called externally to force foot lock state
        }

        public bool IsLeftFootLocked => _leftFootLocked;
        public bool IsRightFootLocked => _rightFootLocked;

        private void OnDrawGizmosSelected()
        {
            if (leftFootTarget != null)
            {
                Gizmos.color = _leftFootLocked ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(leftFootTarget.position, 0.1f);
            }

            if (rightFootTarget != null)
            {
                Gizmos.color = _rightFootLocked ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(rightFootTarget.position, 0.1f);
            }

            // Draw ground rays
            Gizmos.color = Color.blue;
            if (leftFootTarget != null)
            {
                Vector3 leftRayOrigin = leftFootTarget.position + Vector3.up * footRayOffset;
                Gizmos.DrawLine(leftRayOrigin, leftRayOrigin + Vector3.down * raycastDistance);
            }
            if (rightFootTarget != null)
            {
                Vector3 rightRayOrigin = rightFootTarget.position + Vector3.up * footRayOffset;
                Gizmos.DrawLine(rightRayOrigin, rightRayOrigin + Vector3.down * raycastDistance);
            }
        }
    }
}
