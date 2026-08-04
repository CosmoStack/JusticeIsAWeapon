using JusticeIsAWeapon.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace JusticeIsAWeapon.Editor
{
    /// <summary>
    /// Builds a minimal playable scene for the imported dialogue tree:
    /// camera, light, EventSystem (Input System aware), and a DialogueRuntime
    /// object (DialogueManager + DialogueUIController + TestDialogueDriver).
    /// The UI itself is built at runtime by DialogueUIController.
    /// </summary>
    public static class DialogueTestSceneGenerator
    {
        private const string TreeAssetPath = "Assets/_Project/Data/ImportedDialogue/The Midnight Gallery.asset";
        private const string ScenePath = "Assets/Scenes/DialogueTest.unity";

        [MenuItem("Tools/JusticeIsAWeapon/2. Generate Dialogue Test Scene")]
        public static void GenerateFromMenu()
        {
            GenerateCore();
        }

        /// <summary>Batch entry point: Unity -executeMethod JusticeIsAWeapon.Editor.DialogueTestSceneGenerator.GenerateScene</summary>
        public static void GenerateScene()
        {
            GenerateCore();
        }

        private static void GenerateCore()
        {
            var tree = AssetDatabase.LoadAssetAtPath<JusticeIsAWeapon.Data.DialogueTreeSO>(TreeAssetPath);
            if (tree == null)
            {
                Debug.LogError($"[TestScene] No imported tree at {TreeAssetPath}. Run 'Tools > JusticeIsAWeapon > 1. Import The Midnight Gallery Dialogue' first.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            cameraGO.transform.position = new Vector3(0, 1, -10);

            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGO.AddComponent<StandaloneInputModule>();
#endif

            GameObject runtimeGO = new GameObject("DialogueRuntime");
            runtimeGO.AddComponent<DialogueManager>();
            runtimeGO.AddComponent<DialogueUIController>();
            TestDialogueDriver driver = runtimeGO.AddComponent<TestDialogueDriver>();
            driver.dialogueTree = tree;
            driver.startNodeId = "Interview Elena";

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TestScene] Saved {ScenePath} — open it and press Play to test the dialogue tree.");
            EditorGUIUtility.PingObject(tree);
        }
    }
}
