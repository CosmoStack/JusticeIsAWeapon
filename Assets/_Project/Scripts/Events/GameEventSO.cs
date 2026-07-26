// ============================================================================
// GameEventSO.cs
//
// WHAT THIS SCRIPT DOES:
// A reusable "announcement" that different systems can share WITHOUT
// needing a direct reference to each other. One script "raises" the event;
// any number of other scripts can be "listening" for it.
//
// HOW TO USE IT (no coding needed after this file exists):
// 1. Right-click in the Project window → Create → JusticeIsAWeapon → Game Event.
// 2. Name it something like "DoorUnlockedEvent" or "InterviewCompletedEvent".
// 3. Drag that asset into any script that should RAISE it, and call
//    myEvent.Raise() when that thing happens.
// 4. Drag that same asset into a GameEventListener component (see below)
//    on any GameObject that should REACT to it, and hook up what should
//    happen in the Inspector — no code required.
//
// EXAMPLE:
//   ClueUnlockGate.cs raises "DoorUnlockedEvent" when the 4th interview
//   finishes. A GameEventListener on the door's Animator reacts to that
//   same event and plays the "unlock" animation. Neither script knows
//   the other one exists.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace JusticeIsAWeapon.Events
{
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "JusticeIsAWeapon/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        // Every listener currently waiting on this event.
        private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

        /// <summary>Call this when the thing this event represents actually happens.</summary>
        public void Raise()
        {
            // Loop backwards in case a listener removes itself while we're
            // going through the list (e.g. a "listen only once" script).
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(GameEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}