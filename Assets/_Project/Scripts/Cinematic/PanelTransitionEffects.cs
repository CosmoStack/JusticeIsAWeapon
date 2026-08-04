// ============================================================================
// PanelTransitionEffects.cs
//
// WHAT THIS SCRIPT DOES:
// A small, reusable transition helper. Give it a CanvasGroup and it can
// fade that CanvasGroup in or out over a set duration, optionally sliding
// it into place while it fades. ComicSequencePlayer calls this between
// panels; later, the Present-Day <-> Time-Vision palette swap (GDD §10.2)
// can reuse this exact same component too.
//
// EASING:
// Instead of moving at a constant (linear) speed, the animation follows the
// serialized "easingCurve" (an AnimationCurve). The default is an
// ease-in-out curve — it starts slow, speeds up in the middle, then slows
// down near the end — which reads as far more natural than a constant-speed
// fade. Reshape the curve live in the Inspector to get any feel you want
// (ease-in, ease-out, bounce, etc.).
//
// SLIDE-IN:
// While fading in, the panel can also glide from an offset into its resting
// position. "slideInOffset" is how far (in local UI units) the panel starts
// from where it ends up. Slide in from the right = (300, 0); slide up from
// below = (0, -300). Leave it at (0, 0) for a plain fade with no movement.
//
// WHY THIS IS ITS OWN SCRIPT (instead of just being part of the sequencer):
// Keeping "how to transition" separate from "what order panels play in"
// means either piece can be reused or changed without touching the other.
// This script doesn't know or care what a "panel" is.
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

        [Header("Easing (reshape the curve in the Inspector to change the feel)")]
        [Tooltip("0 = start of the transition, 1 = end. The default ease-in-out curve starts slow, speeds up, then slows down again.")]
        [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Slide-in (optional)")]
        [Tooltip("How far the panel starts from its final position while fading in. (300, 0) slides in from the right; (0, -300) slides up from below. Set to (0, 0) to disable sliding.")]
        [SerializeField] private Vector2 slideInOffset = Vector2.zero;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // Guard against a null/empty easing curve (e.g. if the Inspector
            // never serialized the default). Without this, Evaluate() throws
            // on the first frame and the fade freezes at alpha 0.
            if (easingCurve == null || easingCurve.length == 0)
            {
                easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            _rectTransform = transform as RectTransform;
        }

        /// <summary>
        /// Fades this CanvasGroup from fully transparent to fully visible,
        /// optionally sliding it from slideInOffset into place while it fades.
        /// "instant" skips the animation entirely — used for the
        /// accessibility "Reduce Motion" setting.
        /// </summary>
        public IEnumerator FadeIn(bool instant = false)
        {
            yield return Fade(0f, 1f, slideInOffset, instant);
        }

        /// <summary>Fades this CanvasGroup from fully visible to fully transparent.</summary>
        public IEnumerator FadeOut(bool instant = false)
        {
            yield return Fade(1f, 0f, Vector2.zero, instant);
        }

        // Shared logic for both directions — moves alpha from "from" to "to"
        // over defaultFadeDuration seconds while optionally sliding from
        // "slideFrom" to the resting position, or jumps instantly if requested.
        private IEnumerator Fade(float from, float to, Vector2 slideFrom, bool instant)
        {
            // Always land on the resting position, even if we skip the animation.
            _canvasGroup.alpha = to;
            SetLocalPosition(Vector2.zero);

            if (instant)
            {
                yield break;
            }

            _canvasGroup.alpha = from;
            SetLocalPosition(slideFrom);

            float elapsed = 0f;

            while (elapsed < defaultFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / defaultFadeDuration);
                float eased = easingCurve.Evaluate(t);

                _canvasGroup.alpha = Mathf.Lerp(from, to, eased);
                SetLocalPosition(Vector2.Lerp(slideFrom, Vector2.zero, eased));

                yield return null;
            }

            _canvasGroup.alpha = to;
            SetLocalPosition(Vector2.zero);
        }

        // Slides the attached UI element to the given local position.
        // No-ops if this component isn't sitting on a RectTransform (non-UI object).
        private void SetLocalPosition(Vector2 localPos)
        {
            if (_rectTransform == null)
            {
                return;
            }

            _rectTransform.anchoredPosition = localPos;
        }
    }
}
