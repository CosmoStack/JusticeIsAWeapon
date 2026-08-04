// ============================================================================
// TimeVisionController.cs
//
// WHAT THIS SCRIPT DOES:
// Runs the full Time Vision activation flow:
//   1. Player triggers vision on some object/hotspot.
//   2. Show a confirm/cancel prompt ("This will cost N Vision Charges").
//   3. If confirmed: spend charges (per VisionStageDataSO.chargeCost),
//      trigger the screen effect, play that stage's comic sequence, then
//      grant its clue.
//   4. If cancelled: nothing happens, no charges spent.
//
// CHARGE COST BEHAVIOUR:
//   chargeCost == 0  →  free vision, no prompt shown, activates immediately.
//   chargeCost == 1  →  standard single-charge cost, prompt shown.
//   chargeCost >= 2  →  multi-charge cost, prompt shown, UseCharge() loops.
// RequestActivation() blocks early if the player cannot afford the full cost.
//
// SETUP IN THE SCENE:
//   Put this on an empty GameObject, e.g. "TimeVisionSystem".
//   Wire up a confirm prompt UI (two buttons) and drag in a
//   ComicSequencePlayer reference (built in Section E).
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Data;
using JusticeIsAWeapon.Cinematic;

namespace JusticeIsAWeapon.Core
{
    public class TimeVisionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisionChargeManager chargeManager;
        [SerializeField] private ComicSequencePlayer comicSequencePlayer;

        [Header("Confirm Prompt UI")]
        [SerializeField] private GameObject confirmPromptRoot;

        // Remembers which stage was requested while the confirm prompt
        // is showing, so ConfirmActivation() knows what to actually play.
        private VisionStageDataSO _pendingStage;

        /// <summary>
        /// Call this from whatever the player interacts with to start a
        /// Time Vision (e.g. a hotspot on the display pedestal in Scene 9).
        /// Free visions (chargeCost == 0) skip the prompt and activate
        /// immediately. Paid visions show the confirm prompt instead.
        /// </summary>
        public void RequestActivation(VisionStageDataSO stage)
        {
            if (stage == null)
            {
                Debug.LogWarning("[TimeVisionController] RequestActivation called with a null stage.");
                return;
            }

            // Free vision (e.g. the first mandatory reveal per stage) — no
            // charge check needed, no prompt needed, activate straight away.
            if (stage.chargeCost == 0)
            {
                ActivateVision(stage);
                return;
            }

            // Paid vision — make sure the player can actually afford it.
            if (!chargeManager.HasEnoughCharges(stage.chargeCost))
            {
                Debug.Log($"[TimeVisionController] Not enough charges for this vision " +
                          $"(needs {stage.chargeCost}, has {chargeManager.ChargesRemaining}).");
                // TODO: once Blind Phase exists, trigger it here if ChargesRemaining == 0.
                return;
            }

            _pendingStage = stage;
            confirmPromptRoot.SetActive(true);
        }

        /// <summary>Wire this to the confirm prompt's "Confirm" button.</summary>
        public void ConfirmActivation()
        {
            confirmPromptRoot.SetActive(false);

            if (_pendingStage == null)
            {
                Debug.LogWarning("[TimeVisionController] ConfirmActivation called with no pending stage.");
                return;
            }

            VisionStageDataSO stage = _pendingStage;
            _pendingStage = null;

            ActivateVision(stage);
        }

        /// <summary>Wire this to the confirm prompt's "Cancel" button.</summary>
        public void CancelActivation()
        {
            confirmPromptRoot.SetActive(false);
            _pendingStage = null;
        }

        // Shared activation path used by both free (no prompt) and
        // confirmed paid visions. Spends charges, plays the sequence,
        // grants the clue.
        private void ActivateVision(VisionStageDataSO stage)
        {
            // Spend charges — loop so each UseCharge() fires its own
            // onChargeUsed event. chargeCost == 0 means this loop
            // does nothing (free vision).
            for (int i = 0; i < stage.chargeCost; i++)
            {
                chargeManager.UseCharge();
            }

            TriggerScreenEffect();

            comicSequencePlayer.Play(stage.comicSequence);

            GrantStageClue(stage);
        }

        // Placeholder for the sepia/color-drain screen effect described in
        // the GDD's Time Vision activation process. Art/Shader work hooks
        // in here later — this method's signature doesn't need to change.
        private void TriggerScreenEffect()
        {
            Debug.Log("[TimeVisionController] TODO: trigger sepia/color-drain screen effect here.");
        }

        // TODO: once the Clue Database (Section H) exists, replace this
        // log with something like:
        //   GameManager.Instance.Get<ClueDatabase>().Add(stage.grantedClue);
        private void GrantStageClue(VisionStageDataSO stage)
        {
            if (stage.grantedClue == null)
            {
                Debug.LogWarning($"[TimeVisionController] Stage '{stage.name}' has no grantedClue assigned.");
                return;
            }

            Debug.Log($"[TimeVisionController] Granted clue: {stage.grantedClue.name}");
        }
    }
}
