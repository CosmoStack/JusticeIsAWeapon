// ============================================================================
// InspectableInfo.cs
//
// WHAT THIS SCRIPT DOES:
// A simple, plain data container — NOT a ScriptableObject, NOT a
// MonoBehaviour — just a small bundle of fields describing "something the
// player can look at": a title, some descriptive text, and an icon/image.
//
// WHY THIS EXISTS:
// Some hotspots represent a real clue (the Hidden Ledger) and some are just
// flavor with no gameplay weight (the Champagne Flutes). Both need to show
// the same kind of Examine Panel. Rather than forcing every flavor object
// to have a full ClueDataSO asset, InteractableHotspot can fill one of
// these in directly in the Inspector for flavor-only objects, OR pull one
// from a real ClueDataSO when there's an actual clue attached.
//
// NOTE: The optional "linkedClue" field is there so the Examine Panel's
// "Add to Journal" button has something to hand to the Clue Database
// later (Section H) — for flavor-only objects, leave it empty.
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Data;

namespace JusticeIsAWeapon.Interaction
{
    [System.Serializable]
    public class InspectableInfo
    {
        public string title;

        [TextArea]
        public string description;

        public Sprite icon;

        [Tooltip("Leave empty for flavor-only objects with no real clue attached.")]
        public ClueDataSO linkedClue;
    }
}