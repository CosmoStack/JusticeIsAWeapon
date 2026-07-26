// ============================================================================
// GameEventSOGeneric.cs
//
// WHAT THIS SCRIPT DOES:
// Same idea as GameEventSO, but carries a piece of data along with the
// announcement — e.g. "a clue was discovered, and here's WHICH clue."
//
// THIS FILE ON ITS OWN DOES NOTHING — it's a base class. To actually use
// it, make a small concrete version for whatever data type you need, the
// same way ClueGameEventSO does below for a `string` clue ID.
//
// WHEN TO ADD A NEW ONE OF THESE:
// Once ClueDataSO exists (Section B), duplicate ClueGameEventSO.cs below,
// swap `string` for `ClueDataSO`, rename it, and you have a fully working
// typed event for clues — no changes needed here.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace JusticeIsAWeapon.Events
{
    // T is a placeholder for "whatever type of data this event carries" —
    // it gets filled in by whichever concrete class inherits from this.
    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<GameEventListener<T>> _listeners = new List<GameEventListener<T>>();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(GameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(GameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }
    }

    // The listener counterpart — also a base class, same reasoning as above.
    public abstract class GameEventListener<T> : MonoBehaviour
    {
        public abstract void OnEventRaised(T value);
    }
}