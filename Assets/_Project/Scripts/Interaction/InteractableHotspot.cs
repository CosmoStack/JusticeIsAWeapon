// ============================================================================
// InteractableHotspot.cs
//
// WHAT THIS SCRIPT DOES:
// Drop this on ANY object with a Collider — a placeholder cube today, a
// finished piece of Art later — to make it tappable/clickable. When
// clicked, it announces "I was interacted with, here's my info" through
// a shared event. It does NOT know or care what happens next (opening a
// panel, playing a sound) — that's entirely up to whatever is listening.
//
// REQUIREMENTS ON THE GAMEOBJECT:
//   - Needs a Collider (BoxCollider works fine for a placeholder cube).
//
// TWO WAYS TO FILL IN THE INFO:
//   1. FLAVOR-ONLY OBJECT (e.g. Champagne Flutes): fill in "Manual Info"
//      directly in the Inspector — title, description, icon.
//   2. REAL CLUE OBJECT (e.g. Hidden Ledger): assign "Linked Clue" and
//      leave Manual Info's title blank — this script will pull the
//      display text from the clue instead once ClueDataSO's real fields
//      exist (see the TODO below).
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Data;

namespace JusticeIsAWeapon.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class InteractableHotspot : MonoBehaviour
    {
        [Header("Option A — flavor-only object (no real clue)")]
        [SerializeField] private InspectableInfo manualInfo;

        [Header("Option B — real clue object")]
        [Tooltip("If set, this takes priority over Manual Info above.")]
        [SerializeField] private ClueDataSO linkedClue;

        [Header("Shared Event (drag the same asset onto every hotspot)")]
        [SerializeField] private InspectableInfoGameEvent onInteractEvent;

        // Runs when the player clicks/taps this object.
        // OnMouseDown works for both editor mouse clicks and single-touch
        // taps on Android without any extra setup, as long as this object
        // has a Collider and there's a Camera tagged "MainCamera" in the scene.
        private void OnMouseDown()
        {
            Debug.Log("CLICK DETECTED ON: " + gameObject.name);
            InspectableInfo infoToSend = BuildInfoToSend();
            onInteractEvent.Raise(infoToSend);
        }

        // Decides which info to send: a real clue's info if one is linked,
        // otherwise the manually-filled flavor info.
        private InspectableInfo BuildInfoToSend()
        {
            if (linkedClue != null)
            {
                // TODO: once ClueDataSO's full version exists (Section B),
                // replace the two placeholder lines below with its real
                // title/description fields, e.g.:
                //   title = linkedClue.clueTitle
                //   description = linkedClue.detectiveNote
                return new InspectableInfo
                {
                    title = linkedClue.name,   // placeholder — swap for real field later
                    description = "(Clue description pending ClueDataSO Section B build)",
                    icon = manualInfo.icon,    // fine to still set an icon manually for now
                    linkedClue = linkedClue
                };
            }

            return manualInfo;
        }
    }
}