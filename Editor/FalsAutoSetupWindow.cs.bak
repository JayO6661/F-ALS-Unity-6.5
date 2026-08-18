using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using FGP.FALS.Motion;
using FGP.FALS.Procedural;
using FGP.FALS.Runtime;

namespace FGP.FALS.EditorTools
{
    public sealed class FAlsAutoSetupWindow : EditorWindow
    {
        private GameObject _target;

        [MenuItem("Tools/F-ALS/Auto Setup Selected Player")]
        public static void Open()
        {
            GetWindow<FAlsAutoSetupWindow>("F-ALS Auto Setup").Show();
        }

        private void OnGUI()
        {
            _target = (GameObject)EditorGUILayout.ObjectField("Player Root", _target, typeof(GameObject), true);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Animator Parameters");
                GUILayout.Label("- FALS_Grounded (Bool)");
                GUILayout.Label("- FALS_DesiredSpeed, FALS_Stride, FALS_MoveAlpha, FALS_Lean (Float)");
                GUILayout.Label("- FALS_Gait, FALS_RotationMode, FALS_Stance, FALS_Action, FALS_FootballAction, FALS_LockedFoot (Int)");
                GUILayout.Label("- FALS_ActionReady (Bool)");
                GUILayout.Label("- FALS_PhysicalControl, FALS_FootLock, FALS_PelvisUp, FALS_PelvisForward, FALS_LeanCorrection, FALS_GroundAdaptation, FALS_Balance, FALS_LeftFootY, FALS_RightFootY (Float)");
            }

            GUILayout.Space(8);

            if (GUILayout.Button("Apply F-ALS Core Setup") && _target != null)
            {
                ApplySetup(_target.transform);
            }

            if (_target == null)
            {
                EditorGUILayout.HelpBox("Select player root GameObject first.", MessageType.Info);
            }
        }

        private static void ApplySetup(Transform root)
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var controller = root.GetComponent<FAlsController>();
            var input = root.GetComponent<FAlsInputDriver>();
            var bootstrap = root.GetComponent<FAlsBootstrap>();
            var locomotion = root.GetComponent<FAlsLocomotionMotor>();
            var bridge = root.GetComponent<FAlsAnimatorBridge>();
            var poseDriver = root.GetComponent<FAlsProceduralPoseDriver>();

            if (root.GetComponent<CharacterController>() == null)
            {
                Undo.AddComponent<CharacterController>(root.gameObject);
            }

            if (locomotion == null)
            {
                Undo.AddComponent<FAlsLocomotionMotor>(root.gameObject);
                locomotion = root.GetComponent<FAlsLocomotionMotor>();
            }

            if (controller == null)
            {
                Undo.AddComponent<FAlsController>(root.gameObject);
                controller = root.GetComponent<FAlsController>();
            }

            if (input == null)
            {
                Undo.AddComponent<FAlsInputDriver>(root.gameObject);
                input = root.GetComponent<FAlsInputDriver>();
            }

            if (bootstrap == null)
            {
                Undo.AddComponent<FAlsBootstrap>(root.gameObject);
                bootstrap = root.GetComponent<FAlsBootstrap>();
            }

            if (bridge == null)
            {
                Undo.AddComponent<FAlsAnimatorBridge>(root.gameObject);
                bridge = root.GetComponent<FAlsAnimatorBridge>();
            }

            if (poseDriver == null)
            {
                Undo.AddComponent<FAlsProceduralPoseDriver>(root.gameObject);
                poseDriver = root.GetComponent<FAlsProceduralPoseDriver>();
            }

            var cc = root.GetComponent<CharacterController>();
            if (locomotion != null)
            {
                var ccField = locomotion.GetType().GetField("characterController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (ccField != null)
                {
                    ccField.SetValue(locomotion, cc);
                    EditorUtility.SetDirty(locomotion);
                }
            }

            if (bootstrap != null)
            {
                AssignSerializedRef(bootstrap, "controller", controller);
                AssignSerializedRef(bootstrap, "inputDriver", input);
            }

            if (bridge != null && animator != null)
            {
                AssignSerializedRef(bridge, "animator", animator);
            }

            if (poseDriver != null)
            {
                var pelvis = FindTransform(root, new[] { "Hips", "Hip", "pelvis", "Pelvis" });
                var leftFoot = FindTransform(root, new[] { "LeftFoot", "Foot_L", "foot_l" });
                var rightFoot = FindTransform(root, new[] { "RightFoot", "Foot_R", "foot_r" });

                AssignSerializedRef(poseDriver, "pelvis", pelvis);
                AssignSerializedRef(poseDriver, "leftFoot", leftFoot);
                AssignSerializedRef(poseDriver, "rightFoot", rightFoot);
            }

            if (bootstrap != null || bridge != null || poseDriver != null)
            {
                if (bootstrap != null) EditorUtility.SetDirty(bootstrap);
                if (bridge != null) EditorUtility.SetDirty(bridge);
                if (poseDriver != null) EditorUtility.SetDirty(poseDriver);

                EditorSceneManager.MarkAllScenesDirty();
            }

            Debug.Log("F-ALS setup applied.");
        }

        private static void AssignSerializedRef(Object target, string fieldName, Object value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var prop = serializedObject.FindProperty(fieldName);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                return;
            }

            prop.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static Transform FindTransform(Transform root, string[] names)
        {
            foreach (var name in names)
            {
                var result = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
