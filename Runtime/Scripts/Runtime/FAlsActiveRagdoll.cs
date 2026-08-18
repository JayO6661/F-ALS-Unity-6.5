using UnityEngine;

namespace FGP.FALS.Runtime
{
    /// <summary>
    /// Active Ragdoll integration for F-ALS.
    /// 
    /// Switches between animated control and physics-based ragdoll based on
    /// PhysicalControl signal from Recovery system. When PhysicalControl < 0.5,
    /// ragdoll takes over. When recovering, blends back to animated pose.
    /// 
    /// Requirements:
    /// - Rigidbody on each bone in the ragdoll hierarchy
    /// - CharacterController disabled during ragdoll mode
    /// - Animator with writeDefaults off or properly configured
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Runtime.FAlsController))]
    public class FAlsActiveRagdoll : MonoBehaviour
    {
        [Header("Ragdoll Setup")]
        [SerializeField] private Transform[] ragdollBones;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;

        [Header("Blending")]
        [SerializeField] private float blendSpeed = 10f;
        [SerializeField] private float ragdollThreshold = 0.3f;
        [SerializeField] private float recoveryThreshold = 0.7f;

        [Header("Physics")]
        [SerializeField] private float ragdollMass = 1f;
        [SerializeField] private float ragdollDrag = 0.5f;
        [SerializeField] private float ragdollAngularDrag = 0.8f;

        private Rigidbody[] _boneRigidbodies;
        private Collider[] _boneColliders;
        private bool _isRagdollActive;
        private float _currentPhysicalControl = 1f;
        private Runtime.FAlsController _controller;
        private Vector3 _lastAnimatedVelocity;
        private Quaternion[] _initialBoneRotations;

        private void Awake()
        {
            _controller = GetComponent<Runtime.FAlsController>();
            CacheComponents();
            StoreInitialPose();
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
        }

        private void CacheComponents()
        {
            if (ragdollBones == null || ragdollBones.Length == 0)
            {
                Debug.LogWarning("[FAlsActiveRagdoll] No ragdoll bones assigned. Auto-detecting...");
                AutoDetectRagdollBones();
            }

            _boneRigidbodies = new Rigidbody[ragdollBones.Length];
            _boneColliders = new Collider[ragdollBones.Length];

            for (int i = 0; i < ragdollBones.Length; i++)
            {
                if (ragdollBones[i] != null)
                {
                    _boneRigidbodies[i] = ragdollBones[i].GetComponent<Rigidbody>();
                    _boneColliders[i] = ragdollBones[i].GetComponent<Collider>();

                    if (_boneRigidbodies[i] == null)
                    {
                        _boneRigidbodies[i] = ragdollBones[i].gameObject.AddComponent<Rigidbody>();
                    }

                    _boneRigidbodies[i].mass = ragdollMass;
                    _boneRigidbodies[i].drag = ragdollDrag;
                    _boneRigidbodies[i].angularDrag = ragdollAngularDrag;
                    _boneRigidbodies[i].interpolation = RigidbodyInterpolation.Interpolate;
                    _boneRigidbodies[i].collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
            }
        }

        private void AutoDetectRagdollBones()
        {
            // Try to find common ragdoll bone names
            string[] boneNames = { "Hips", "Spine", "Chest", "Neck", "Head", 
                                   "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
                                   "RightShoulder", "RightArm", "RightForeArm", "RightHand",
                                   "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
                                   "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase" };

            var foundBones = new Transform[boneNames.Length];
            int count = 0;

            foreach (var boneName in boneNames)
            {
                var bone = FindChildRecursive(transform, boneName);
                if (bone != null)
                {
                    foundBones[count++] = bone;
                }
            }

            if (count > 0)
            {
                var finalBones = new Transform[count];
                System.Array.Copy(foundBones, finalBones, count);
                ragdollBones = finalBones;
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;

            foreach (Transform child in parent)
            {
                var result = FindChildRecursive(child, name);
                if (result != null) return result;
            }

            return null;
        }

        private void StoreInitialPose()
        {
            if (ragdollBones != null && ragdollBones.Length > 0)
            {
                _initialBoneRotations = new Quaternion[ragdollBones.Length];
                for (int i = 0; i < ragdollBones.Length; i++)
                {
                    if (ragdollBones[i] != null)
                    {
                        _initialBoneRotations[i] = ragdollBones[i].localRotation;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (_controller == null) return;

            var recovery = _controller.Signals.Recovery;
            _currentPhysicalControl = recovery.PhysicalControl;

            bool shouldBeRagdoll = _currentPhysicalControl < ragdollThreshold;
            bool shouldBeAnimated = _currentPhysicalControl > recoveryThreshold;

            if (shouldBeRagdoll && !_isRagdollActive)
            {
                ActivateRagdoll();
            }
            else if (shouldBeAnimated && _isRagdollActive)
            {
                DeactivateRagdoll();
            }

            if (_isRagdollActive)
            {
                UpdateRagdollPhysics(recovery);
            }
        }

        private void ActivateRagdoll()
        {
            _isRagdollActive = true;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            foreach (var rb in _boneRigidbodies)
            {
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = _lastAnimatedVelocity;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            foreach (var collider in _boneColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            Debug.Log("[FAlsActiveRagdoll] Ragdoll activated");
        }

        private void DeactivateRagdoll()
        {
            _isRagdollActive = false;

            foreach (var rb in _boneRigidbodies)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            foreach (var collider in _boneColliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            if (animator != null)
            {
                animator.enabled = true;
            }

            // Snap bones back to animated pose
            if (ragdollBones != null && _initialBoneRotations != null)
            {
                for (int i = 0; i < ragdollBones.Length && i < _initialBoneRotations.Length; i++)
                {
                    if (ragdollBones[i] != null)
                    {
                        ragdollBones[i].localRotation = _initialBoneRotations[i];
                    }
                }
            }

            Debug.Log("[FAlsActiveRagdoll] Animated control restored");
        }

        private void UpdateRagdollPhysics(FAlsRecoveryOutput recovery)
        {
            // Apply forces based on recovery state
            if (recovery.State == FAlsRecoveryState.Falling)
            {
                // Add slight air resistance
                foreach (var rb in _boneRigidbodies)
                {
                    if (rb != null && !rb.isKinematic)
                    {
                        rb.AddForce(-rb.velocity * 0.1f, ForceMode.VelocityChange);
                    }
                }
            }
            else if (recovery.State == FAlsRecoveryState.GroundedRecovery)
            {
                // Dampen motion during recovery
                foreach (var rb in _boneRigidbodies)
                {
                    if (rb != null && !rb.isKinematic)
                    {
                        rb.drag = Mathf.Lerp(rb.drag, 2f, Time.fixedDeltaTime * 5f);
                    }
                }
            }

            // Store velocity for transition
            Vector3 totalVelocity = Vector3.zero;
            int activeCount = 0;
            foreach (var rb in _boneRigidbodies)
            {
                if (rb != null && !rb.isKinematic)
                {
                    totalVelocity += rb.velocity;
                    activeCount++;
                }
            }
            if (activeCount > 0)
            {
                _lastAnimatedVelocity = totalVelocity / activeCount;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (ragdollBones == null) return;

            Gizmos.color = _isRagdollActive ? Color.red : Color.green;
            foreach (var bone in ragdollBones)
            {
                if (bone != null)
                {
                    Gizmos.DrawWireSphere(bone.position, 0.1f);
                }
            }
        }

        public bool IsRagdollActive => _isRagdollActive;
        public float CurrentPhysicalControl => _currentPhysicalControl;
    }
}
