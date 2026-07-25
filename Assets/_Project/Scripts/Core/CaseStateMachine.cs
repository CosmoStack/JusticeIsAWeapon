// ============================================================================
// CaseStateMachine.cs
//
// WHAT THIS SCRIPT DOES:
// Tracks which "phase" the current case is in — is the player watching a
// cutscene, exploring a scene, talking to a witness, etc. — and tells the
// rest of the game whenever that phase changes.
//
// WHY THIS MATTERS:
// Instead of every script individually checking "am I allowed to move the
// player right now?", they all just ask this one script: IsInputAllowed.
// And instead of the UI needing to know exactly when a cutscene starts,
// it just subscribes to OnStateChanged and reacts on its own.
//
// WHERE IT LIVES:
// One CaseStateMachine per gameplay scene (Exhibition Hall, Private
// Office, etc.) — NOT persistent across scenes like GameManager.
// ============================================================================

using System;
using UnityEngine;

namespace Systems.Core
{
    // Every phase a case can be in, in the order the GDD describes them.
    // Not every case will use every state, but all 7 are available.
    public enum CaseState
    {
        Cinematic,          // Watching a cutscene — no player input
        Investigation,      // Exploring a scene, clicking on objects
        DialogueInterview,  // Talking to a witness/suspect
        DeductionClipboard, // Reviewing clues in the Journal UI
        TimeVisionPlayback, // Watching a Time Vision comic sequence
        BlindPhase,         // Vision exhausted — audio-only navigation
        Verdict             // Making the final accusation
    }

    public class CaseStateMachine : MonoBehaviour
    {
        [Header("Starting state when this scene loads")]
        [SerializeField] private CaseState startingState = CaseState.Cinematic;

        public CaseState CurrentState { get; private set; }

        // True when the player is allowed to click/move around.
        // Other scripts (input handlers, hotspots) check this before
        // reacting to a tap, instead of each needing their own rules.
        public bool IsInputAllowed { get; private set; }

        // Anyone can subscribe to this to react when the state changes.
        // Example, from a UI script:
        //     stateMachine.OnStateChanged += HandleStateChanged;
        public event Action<CaseState> OnStateChanged;

        private void Start()
        {
            // Run "Enter" logic for whatever state we're starting in,
            // without treating it as a "change" (there's no previous state).
            EnterState(startingState);
            CurrentState = startingState;
        }

        /// <summary>
        /// Moves the case to a new state. Call this from wherever a scene's
        /// flow decides it's time to move on — e.g. after the last witness
        /// interview finishes, call ChangeState(CaseState.DeductionClipboard).
        /// </summary>
        public void ChangeState(CaseState newState)
        {
            if (newState == CurrentState)
            {
                // Already in this state — nothing to do.
                return;
            }

            ExitState(CurrentState);
            CurrentState = newState;
            EnterState(newState);

            OnStateChanged?.Invoke(CurrentState);
        }

        // Runs once, the moment we arrive in a state.
        private void EnterState(CaseState state)
        {
            // This is the one place that decides "can the player click
            // things right now?" for every state in the game.
            switch (state)
            {
                case CaseState.Cinematic:
                    IsInputAllowed = false;
                    break;

                case CaseState.Investigation:
                    IsInputAllowed = true;
                    break;

                case CaseState.DialogueInterview:
                    IsInputAllowed = true; // still need to tap dialogue choices
                    break;

                case CaseState.DeductionClipboard:
                    IsInputAllowed = true;
                    break;

                case CaseState.TimeVisionPlayback:
                    IsInputAllowed = false;
                    break;

                case CaseState.BlindPhase:
                    IsInputAllowed = true; // audio-only navigation still needs taps
                    break;

                case CaseState.Verdict:
                    IsInputAllowed = false;
                    break;
            }

            Debug.Log($"[CaseStateMachine] Entered: {state}");
        }

        // Runs once, the moment we leave a state. Currently just logs —
        // add cleanup here later if a specific state needs it
        // (e.g. stopping a cinematic's audio when skipped early).
        private void ExitState(CaseState state)
        {
            Debug.Log($"[CaseStateMachine] Exited: {state}");
        }
    }
}