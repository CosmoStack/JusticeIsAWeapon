// ============================================================================
// ClueUnlockGate.cs
//
// WHAT THIS SCRIPT DOES:
// A generic "this stays locked until a condition is met" component. Drop
// it on the Restricted Corridor door today; reuse it on every future
// locked object in the game — a safe, a case file, anything.
//
// HOW IT COUNTS PROGRESS (no dependency on unwritten scripts):
// This script doesn't listen to events directly by itself. Instead, you
// add a plain GameEventListener component (already in your Events folder)
// alongside this one, and wire its Response to call this script's
// OnProgressEventReceived() method — entirely through the Inspector, no
// code changes needed. See the setup steps below.
//
// TWO MODES:
//   1. ProgressCount — unlocks once OnProgressEventReceived() has been
//      called N times (e.g. once per completed interview).
//   2. CustomFlag — unlocks the moment SetCustomFlag(true) is called by
//      some other script, for one-off conditions that aren't a simple count.
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Events;

namespace JusticeIsAWeapon.Interaction
{
    public class ClueUnlockGate : MonoBehaviour
    {
        public enum ConditionType
        {
            ProgressCount,
            CustomFlag
        }

        [Header("Unlock Condition")]
        [SerializeField] private ConditionType conditionType = ConditionType.ProgressCount;

        [Tooltip("Only used in ProgressCount mode. E.g. 4 for 'all 4 interviews logged.'")]
        [SerializeField] private int requiredProgressCount = 4;

        [Header("What happens when unlocked")]
        [SerializeField] private GameEventSO onUnlocked;

        private int _currentProgressCount = 0;
        private bool _customFlagValue = false;

        public bool IsUnlocked { get; private set; } = false;

        /// <summary>
        /// Wire this up as the Response of a GameEventListener component on
        /// this same GameObject, listening to whatever progress event
        /// applies (e.g. "InterviewCompletedEvent"). Called once per
        /// relevant event — e.g. once per witness interview finished.
        /// </summary>
        public void OnProgressEventReceived()
        {
            if (IsUnlocked) return;

            _currentProgressCount++;
            Debug.Log($"[ClueUnlockGate] '{gameObject.name}' progress: {_currentProgressCount}/{requiredProgressCount}");

            CheckUnlockCondition();
        }

        /// <summary>
        /// For CustomFlag mode — call this from another script once a
        /// specific one-off condition is met (e.g. a particular clue found).
        /// </summary>
        public void SetCustomFlag(bool value)
        {
            if (IsUnlocked) return;

            _customFlagValue = value;
            CheckUnlockCondition();
        }

        private void CheckUnlockCondition()
        {
            bool conditionMet = conditionType == ConditionType.ProgressCount
                ? _currentProgressCount >= requiredProgressCount
                : _customFlagValue;

            if (conditionMet)
            {
                Unlock();
            }
        }

        private void Unlock()
        {
            IsUnlocked = true;
            onUnlocked?.Raise();
            Debug.Log($"[ClueUnlockGate] '{gameObject.name}' is now UNLOCKED.");
        }
    }
}