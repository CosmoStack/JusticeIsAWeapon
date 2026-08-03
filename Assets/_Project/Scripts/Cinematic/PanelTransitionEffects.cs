// ============================================================================
// PanelTransitionEffects.cs
//
// WHAT THIS SCRIPT DOES:
// A small, reusable fade helper. Give it a CanvasGroup and it can fade
// that CanvasGroup in or out over a set duration. ComicSequencePlayer
// calls this between panels; later, the Present-Day <-> Time-Vision
// palette swap (GDD §10.2) can reuse this exact same component too.
//
// WHY THIS IS ITS OWN SCRIPT (instead of just being part of the sequencer):
// Keeping "how to fade something" separate from "what order panels play
// in" means either piece can be reused or changed without touching the
// other. This script doesn't know or care what a "panel" is.
// ============================================================================

using System.Collections;
using UnityEngine;

namespace JusticeIsAWeapon.Cinematic
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelTransitionEffects : MonoBehaviour
    {
        [Header("Default Timing (matches GDD §10.2 — 0.6s crossfade)")]
        [SerializeField] private float defaultFadeDuration = 0.6f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Fades this CanvasGroup from fully transparent to fully visible.
        /// "instant" skips the animation entirely — used for the
        /// accessibility "Reduce Motion" setting.
        /// </summary>
        public IEnumerator FadeIn(bool instant = false)
        {
            yield return Fade(0f, 1f, instant);
        }

        /// <summary>Fades this CanvasGroup from fully visible to fully transparent.</summary>
        public IEnumerator FadeOut(bool instant = false)
        {
            yield return Fade(1f, 0f, instant);
        }

        // Shared logic for both directions — moves alpha from "from" to "to"
        // over defaultFadeDuration seconds, or jumps instantly if requested.
        private IEnumerator Fade(float from, float to, bool instant)
        {
            if (instant)
            {
                _canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < defaultFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / defaultFadeDuration;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }
    }
}