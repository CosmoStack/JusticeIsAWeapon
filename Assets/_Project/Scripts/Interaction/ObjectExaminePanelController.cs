// ============================================================================
// ObjectExaminePanelController.cs
//
// WHAT THIS SCRIPT DOES:
// The "Object Examine Panel" from the GDD (§21.2) — a popup showing
// whatever was just clicked: a title, description, and image, plus an
// "Add to Journal" button. It listens for the SAME shared event every
// InteractableHotspot raises, so it works identically for every object
// in the game without needing to know about any of them individually.
//
// SETUP IN THE SCENE:
//   - Put this on the root of your Examine Panel UI (a Panel GameObject).
//   - Drag in references to its child Text/Image/Button components.
//   - Drag the SAME InspectableInfoGameEvent asset used by every
//     InteractableHotspot into this script's "On Interact Event" field.
//   - Leave the panel disabled by default in the scene — this script
//     turns it on/off itself.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JusticeIsAWeapon.Events;

namespace JusticeIsAWeapon.Interaction
{
    public class ObjectExaminePanelController : GameEventListener<InspectableInfo>
    {
        [Header("Shared Event (same asset every hotspot uses)")]
        [SerializeField] private InspectableInfoGameEvent onInteractEvent;

        [Header("UI References (drag from this panel's children)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button addToJournalButton;
        [SerializeField] private Button dismissButton;

        // Keeps track of whichever object is currently being shown, so the
        // "Add to Journal" button knows what to add.
        private InspectableInfo _currentInfo;

        private void Awake()
        {
            panelRoot.SetActive(false);

            addToJournalButton.onClick.AddListener(HandleAddToJournal);
            dismissButton.onClick.AddListener(ClosePanel);
        }

        // --------------------------------------------------------------
        // These two methods are required by GameEventListener<T> — Unity
        // calls OnEventRaised automatically whenever onInteractEvent fires.
        // --------------------------------------------------------------
        private void OnEnable()
        {
            onInteractEvent.RegisterListener(this);
        }

        private void OnDisable()
        {
            onInteractEvent.UnregisterListener(this);
        }

        public override void OnEventRaised(InspectableInfo info)
        {
            ShowPanel(info);
        }

        // --------------------------------------------------------------
        // Panel behavior
        // --------------------------------------------------------------
        private void ShowPanel(InspectableInfo info)
        {
            _currentInfo = info;

            titleText.text = info.title;
            descriptionText.text = info.description;

            // If no icon was provided, just hide the image instead of
            // showing an empty/broken sprite.
            iconImage.gameObject.SetActive(info.icon != null);
            iconImage.sprite = info.icon;

            panelRoot.SetActive(true);
        }

        private void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void HandleAddToJournal()
        {
            if (_currentInfo.linkedClue == null)
            {
                // Flavor-only object — nothing to add, just close.
                Debug.Log($"[ObjectExaminePanel] '{_currentInfo.title}' is flavor-only, nothing added to Journal.");
                ClosePanel();
                return;
            }

            // TODO: once the Clue Database (Section H) exists, replace
            // this log with something like:
            //   GameManager.Instance.Get<ClueDatabase>().Add(_currentInfo.linkedClue);
            Debug.Log($"[ObjectExaminePanel] Added clue to Journal: {_currentInfo.linkedClue.name}");

            ClosePanel();
        }
    }
}