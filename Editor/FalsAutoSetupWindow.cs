using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using FGP.FALS.Motion;
using FGP.FALS.Runtime;

namespace FGP.FALS.EditorTools
{
    public sealed class FAlsAutoSetupWindow : EditorWindow
    {
        private GameObject _target;

        [MenuItem("Tools/F-ALS/Setup Selected Player")]
        public static void Open()
        {
            var window = GetWindow<FAlsAutoSetupWindow>("F-ALS Setup");
            window._target = Selection.activeGameObject;
            window.Show();
        }

        [MenuItem("Tools/F-ALS/Validate Selected Player")]
        public static void ValidateSelected()
        {
            Validate(Selection.activeGameObject, true);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("F-ALS Production Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Adds only the production locomotion foundation. Input, skills, stamina, ball gameplay and networking remain game-owned.",
                MessageType.Info);

            _target = (GameObject)EditorGUILayout.ObjectField("Player Root", _target, typeof(GameObject), true);

            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("Apply Core Setup", GUILayout.Height(28f)))
                {
                    ApplySetup(_target);
                }

                if (GUILayout.Button("Validate"))
                {
                    Validate(_target, true);
                }
            }

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Core components", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• CharacterController");
            EditorGUILayout.LabelField("• FAlsLocomotionMotor");
            EditorGUILayout.LabelField("• FAlsController");
            EditorGUILayout.LabelField("• FAlsAnimatorBridge (when Animator exists)");
            EditorGUILayout.HelpBox(
                "Foot IK is optional and requires Animation Rigging targets/constraints. Add FAlsFootIK only after the rig is prepared.",
                MessageType.None);
        }

        private static void ApplySetup(GameObject target)
        {
            if (target == null) return;

            Undo.RegisterFullObjectHierarchyUndo(target, "Apply F-ALS Core Setup");

            var cc = GetOrAdd<CharacterController>(target);
            var motor = GetOrAdd<FAlsLocomotionMotor>(target);
            var controller = GetOrAdd<FAlsController>(target);
            var animator = target.GetComponentInChildren<Animator>(true);
            var bridge = animator != null ? GetOrAdd<FAlsAnimatorBridge>(target) : null;

            AssignObjectReference(motor, "characterController", cc);
            AssignObjectReference(controller, "locomotionMotor", motor);
            if (bridge != null) AssignObjectReference(bridge, "animator", animator);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.scene);

            Validate(target, false);
            Debug.Log($"[F-ALS] Production core setup applied to '{target.name}'.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void AssignObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) return;

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool Validate(GameObject target, bool logSuccess)
        {
            if (target == null)
            {
                Debug.LogError("[F-ALS] Select the player root GameObject first.");
                return false;
            }

            bool valid = true;
            if (target.GetComponent<CharacterController>() == null)
            {
                Debug.LogError("[F-ALS] Missing CharacterController on player root.", target);
                valid = false;
            }

            if (target.GetComponent<FAlsLocomotionMotor>() == null)
            {
                Debug.LogError("[F-ALS] Missing FAlsLocomotionMotor on player root.", target);
                valid = false;
            }

            if (target.GetComponent<FAlsController>() == null)
            {
                Debug.LogError("[F-ALS] Missing FAlsController on player root.", target);
                valid = false;
            }

            if (target.GetComponent<FAlsInputDriver>() != null || target.GetComponent<FAlsBootstrap>() != null)
            {
                Debug.LogWarning("[F-ALS] Legacy standalone input/bootstrap components detected. Remove them for production game integration.", target);
            }

            if (valid && logSuccess)
            {
                Debug.Log($"[F-ALS] '{target.name}' production core is valid.", target);
            }

            return valid;
        }
    }
}
