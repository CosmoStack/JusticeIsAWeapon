// ============================================================================
// InspectableInfoGameEvent.cs
//
// A ready-to-use typed event that carries an InspectableInfo payload.
// Follows the exact same pattern as StringGameEventSO — this is the
// "one tiny subclass" that GameEventSOGeneric.cs's comments describe.
//
// Create ONE asset from this (Create → Systems → Game Event (Inspectable
// Info)) — every hotspot in the game raises this same shared asset, and
// the Examine Panel listens to that one asset. That's what lets any
// hotspot talk to the panel without knowing it exists.
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Events;

namespace JusticeIsAWeapon.Interaction
{
    [CreateAssetMenu(fileName = "ObjectInteractedEvent", menuName = "JustiveIsAWeapon/Game Event (Inspectable Info)")]
    public class InspectableInfoGameEvent : GameEventSO<InspectableInfo>
    {
    }
}