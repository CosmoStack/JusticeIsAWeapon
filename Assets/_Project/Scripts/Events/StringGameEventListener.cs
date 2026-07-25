// ============================================================================
// StringGameEventListener.cs
//
// Drop-in listener for StringGameEventSO — same "no coding needed" idea as
// GameEventListener, but the Response can use the string that was passed in.
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace Systems.Events
{
    // A UnityEvent that carries a string has to be its own named class for
    // Unity to show it properly in the Inspector — this is that class.
    [System.Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    public class StringGameEventListener : GameEventListener<string>
    {
        [Tooltip("The event asset to listen for.")]
        [SerializeField] private StringGameEventSO Event;

        [Tooltip("What should happen when the event fires. The string it carries is available in here too.")]
        [SerializeField] private StringUnityEvent Response;

        private void OnEnable()
        {
            Event.RegisterListener(this);
        }

        private void OnDisable()
        {
            Event.UnregisterListener(this);
        }

        public override void OnEventRaised(string value)
        {
            Response.Invoke(value);
        }
    }
}