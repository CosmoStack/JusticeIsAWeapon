// ============================================================================
// GameManager.cs
//
// WHAT THIS SCRIPT DOES:
// This is the one object that lives for the entire time the game is running.
// Every other manager (Save, Vision Charges, Clue Database, etc.) will
// register itself here so any script in the game can find it easily,
// without needing to know which scene it lives in.
//
// WHERE IT LIVES:
// The very first scene that loads when the game starts).
//
// KEY UNITY CONCEPT USED HERE: "Singleton"
// A Singleton means "there is only ever one of these in the whole game."
// We store a private reference to that one instance (_instance) and expose
// it through a public property (Instance) so any script can call:
//     GameManager.Instance.DoSomething();
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using JusticeIsAWeapon.Data;
using JusticeIsAWeapon.Enum;

namespace JusticeIsAWeapon.Core
{
    public class GameManager : MonoBehaviour
    {
        // --------------------------------------------------------------
        // SECTION 1: SINGLETON SETUP
        // --------------------------------------------------------------
        // This is the single, shared instance every other script talks to.
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            // Guard clause: if a GameManager already exists (e.g. we
            // accidentally left one in a scene we're loading into),
            // destroy this new duplicate and stop here.
            if (Instance != null)
            {
                Debug.LogWarning("[GameManager] A GameManager already exists. Destroying the duplicate.");
                Destroy(gameObject);
                return;
            }

            // This is now THE GameManager for the rest of the game's session.
            Instance = this;

            // Keeps this GameObject alive when we load a new scene, instead
            // of getting destroyed like everything else in the old scene.
            DontDestroyOnLoad(gameObject);


            GoToMainMenu();
        }

        // --------------------------------------------------------------
        // SECTION 2: CURRENT CASE
        // --------------------------------------------------------------
        // Whatever case (e.g. "The Midnight Gallery") is currently being
        // played. Other scripts read this through CurrentCase.
        [Header("Current Case (read-only while playing)")]
        [SerializeField] private CaseDataSO currentCase;
        public CaseDataSO CurrentCase => currentCase;

        // Other scripts can subscribe to this to know the moment a new
        // case starts, e.g. the Clue Database clearing itself out.
        // Example of subscribing from another script:
        //     GameManager.Instance.OnCaseLoaded += HandleNewCase;
        public event Action<CaseDataSO> OnCaseLoaded;

        /// <summary>
        /// Call this to start a case: stores which case it is, tells
        /// anyone listening, then loads that case's scene.
        /// </summary>
        public void LoadCase(CaseDataSO caseToLoad, string sceneName)
        {
            if (caseToLoad == null)
            {
                Debug.LogError("[GameManager] LoadCase was called with no case data. Nothing happened.");
                return;
            }

            currentCase = caseToLoad;
            OnCaseLoaded?.Invoke(currentCase);

            SceneManager.LoadScene(sceneName);
        }

        // --------------------------------------------------------------
        // SECTION 3: SCENE SHORTCUTS
        // --------------------------------------------------------------
        [Header("Scene Names (must match Build Settings exactly)")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        /// <summary>Loads the Main Menu scene.</summary>
        public void GoToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // --------------------------------------------------------------
        // SECTION 4: FINDING OTHER MANAGERS (SERVICE LOCATOR)
        // --------------------------------------------------------------
        // PROBLEM THIS SOLVES:
        // Later, we'll build SaveManager, VisionChargeManager, etc.
        // Instead of GameManager needing to know about every single one
        // of them in advance, each manager simply "checks in" with
        // GameManager when it wakes up. Any script can then ask
        // GameManager for it by type.
        //
        // EXAMPLE — once SaveManager.cs exists:
        //     // Inside SaveManager's own Awake():
        //     GameManager.Instance.Register(this);
        //
        //     // From anywhere else in the game:
        //     SaveManager save = GameManager.Instance.Get<SaveManager>();
        // --------------------------------------------------------------
        private readonly Dictionary<Type, object> _registeredManagers = new Dictionary<Type, object>();

        /// <summary>Call this from a manager's own Awake() to make it findable.</summary>
        public void Register<T>(T manager) where T : class
        {
            _registeredManagers[typeof(T)] = manager;
        }

        /// <summary>Fetches a previously registered manager by type. Returns null if not found yet.</summary>
        public T Get<T>() where T : class
        {
            _registeredManagers.TryGetValue(typeof(T), out object found);

            if (found == null)
            {
                Debug.LogWarning($"[GameManager] No manager of type {typeof(T).Name} has registered yet.");
            }

            return found as T;
        }
    }
}