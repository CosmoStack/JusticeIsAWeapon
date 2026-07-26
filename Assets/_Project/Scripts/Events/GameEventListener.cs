// ============================================================================
// GameEventListener.cs
//
// WHAT THIS SCRIPT DOES:
// Drop this on any GameObject, drag a GameEventSO into "Event", then use
// the Inspector to say what should happen when that event fires — play a
// sound, trigger an animation, show a UI popup, whatever. No coding needed
// by whoever sets this up (Audio/Art/UI can all do this themselves).
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace JusticeIsAWeapon.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("The event asset to listen for.")]
        [SerializeField] private GameEventSO Event;

        [Tooltip("What should happen when the event above is raised. Set this up in the Inspector.")]
        [SerializeField] private UnityEvent Response;

        // Start listening as soon as this object is active...
        private void OnEnable()
        {
            Event.RegisterListener(this);
        }

        // ...and stop listening the moment it's disabled or destroyed,
        // so we don't react to events after this object is gone.
        private void OnDisable()
        {
            Event.UnregisterListener(this);
        }

        // Called by GameEventSO.Raise(). Not meant to be called directly
        // by other scripts — that's what Event.Raise() is for.
        public void OnEventRaised()
        {
            Response.Invoke();
        }
    }
}