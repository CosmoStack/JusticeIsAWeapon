// ============================================================================
// VisionChargeManager.cs
//
// WHAT THIS SCRIPT DOES:
// The single source of truth for "how many Time Vision charges does the
// player have left in this case." Everything else — the confirm prompt,
// the degradation visuals, the eventual Blind Phase trigger — reads from
// this one place instead of tracking its own copy of the number.
//
// WHY THIS IS "STATIC/SINGLETON" LIKE GameManager:
// Only one case is ever being played at a time, so there only ever needs
// to be one of these. Unlike CaseStateMachine (one per scene), this
// persists conceptually for the whole case — in practice it's simplest
// to just recreate/reset it each time a new case loads, which
// ResetForNewCase() below handles.
//
// GAMEMANAGER INTEGRATION:
// On Awake this registers itself with GameManager's service locator so any
// script can reach it via either path:
//   VisionChargeManager.Instance            // direct singleton
//   GameManager.Instance.Get<VisionChargeManager>()  // service locator
// Both point to the same object. The direct Instance is kept for scripts
// that need a quick grab without going through GameManager.
//
// TODO (once difficulty config exists): replace the hardcoded
// startingCharges default below with a lookup from the active
// Difficulty setting (Easy=3, Normal=4, Hard/Expert=5 per GDD §4.3).
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Events;

namespace JusticeIsAWeapon.Core
{
    public class VisionChargeManager : MonoBehaviour
    {
        public static VisionChargeManager Instance { get; private set; }

        [Header("Starting Charges (TODO: pull from Difficulty config later)")]
        [SerializeField] private int startingCharges = 4; // Normal difficulty default, GDD §4.3

        [Header("Events — assign GameEventSO assets from the Project window")]
        [Tooltip("Fires every time a charge is used, after the count updates.")]
        [SerializeField] private GameEventSO onChargeUsed;

        [Tooltip("Fires once, the moment charges reach zero.")]
        [SerializeField] private GameEventSO onChargesDepleted;

        public int TotalCharges { get; private set; }
        public int ChargesUsed { get; private set; }
        public int ChargesRemaining => TotalCharges - ChargesUsed;

        /// <summary>0 = fully blind, 1 = fully clear. VisionDegradationController reads this each frame.</summary>
        public float RemainingPercent => TotalCharges <= 0 ? 0f : (float)ChargesRemaining / TotalCharges;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Register with the service locator so any script can also
            // reach this via GameManager.Instance.Get<VisionChargeManager>().
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Register(this);
            }
            else
            {
                Debug.LogWarning("[VisionChargeManager] GameManager.Instance not found during Awake. " +
                                 "Service locator registration skipped — direct Instance access still works.");
            }

            ResetForNewCase(startingCharges);
        }

        /// <summary>
        /// Call this whenever a new case starts (e.g. from a GameManager.OnCaseLoaded
        /// subscriber) to wipe the charge count back to that case's starting amount.
        /// </summary>
        public void ResetForNewCase(int charges)
        {
            TotalCharges = charges;
            ChargesUsed = 0;
            Debug.Log($"[VisionChargeManager] Reset for new case. Starting charges: {charges}");
        }

        /// <summary>
        /// Returns true if there is at least one charge left to spend.
        /// Use HasEnoughCharges(n) when a vision costs more than one.
        /// </summary>
        public bool HasChargesRemaining()
        {
            return ChargesRemaining > 0;
        }

        /// <summary>
        /// Returns true if the player has at least <paramref name="count"/> charges left.
        /// TimeVisionController uses this when chargeCost > 1.
        /// </summary>
        public bool HasEnoughCharges(int count)
        {
            return ChargesRemaining >= count;
        }

        /// <summary>
        /// Spends one charge. Call this only after the player has confirmed
        /// the prompt. For visions that cost multiple charges, the caller
        /// (TimeVisionController) loops this — each call fires onChargeUsed
        /// and the final call to zero fires onChargesDepleted.
        /// </summary>
        public void UseCharge()
        {
            if (!HasChargesRemaining())
            {
                Debug.LogWarning("[VisionChargeManager] Tried to use a charge with none remaining.");
                return;
            }

            ChargesUsed++;
            Debug.Log($"[VisionChargeManager] Charge used. Remaining: {ChargesRemaining}/{TotalCharges}");

            onChargeUsed?.Raise();

            if (ChargesRemaining <= 0)
            {
                onChargesDepleted?.Raise();
            }
        }
    }
}
