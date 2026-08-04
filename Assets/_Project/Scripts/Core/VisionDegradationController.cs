// ============================================================================
// VisionDegradationController.cs
//
// WHAT THIS SCRIPT DOES:
// Watches VisionChargeManager and applies a visual effect matching the
// current degradation stage (Clear -> MinorBlur -> Moderate -> Severe ->
// Blind, per GDD §10.3). Harmless to have running from Week 1 onward —
// it does nothing visible until charges actually start depleting.
//
// PLACEHOLDER VISUAL (today): a full-screen gray overlay whose transparency
// increases as charges deplete — a cheap stand-in for "desaturation."
//
// SWAP-IN LATER: once Art/Shader work delivers a real post-processing
// profile (Gaussian blur + chromatic aberration per §10.3's table), replace
// only the body of ApplyStage() below — everything else (the stage
// lookup, the event listening) stays the same.
//
// SETUP IN THE SCENE:
//   Put this on a full-screen UI Image (a plain gray/white rectangle)
//   sitting near the top of the Canvas hierarchy (rendered last = on top).
//   Drag the VisionChargeManager in.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using JusticeIsAWeapon.Enum;

namespace JusticeIsAWeapon.Core
{
    [RequireComponent(typeof(Image))]
    public class VisionDegradationController : MonoBehaviour
    {
        [Header("Dependency")]
        [SerializeField] private VisionChargeManager chargeManager;

        [Header("Placeholder Overlay (swap for real shader/post-processing later)")]
        [Tooltip("How opaque the gray overlay gets at each stage. Index matches VisionDegradationStage order: " +
                 "0=Clear, 1=MinorBlur, 2=Moderate, 3=Severe, 4=Blind.")]
        [SerializeField]
        private float[] placeholderOverlayAlpha =
        {
            0f,     // Clear
            0.15f,  // MinorBlur
            0.35f,  // Moderate
            0.65f,  // Severe
            1f      // Blind
        };

        private Image _overlayImage;

        public VisionDegradationStage CurrentStage { get; private set; }

        private void Awake()
        {
            _overlayImage = GetComponent<Image>();

            // Start fully transparent so the overlay doesn't flash on the
            // first frame before the first Update() call.
            Color c = _overlayImage.color;
            c.a = 0f;
            _overlayImage.color = c;
        }

        private void Update()
        {
            if (chargeManager == null) return;
            RefreshStage();
        }

        private void RefreshStage()
        {
            VisionDegradationStage newStage = CalculateStage();

            // Only call ApplyStage when the stage actually changes —
            // avoids setting Image.color every single frame.
            if (newStage != CurrentStage)
            {
                CurrentStage = newStage;
                ApplyStage(newStage);
            }
        }

        // Maps "charges remaining as a percentage" onto one of the five
        // stages. Thresholds are tunable by playtesting feel — the
        // breakpoints are at even 25% intervals for now.
        private VisionDegradationStage CalculateStage()
        {
            float remaining = chargeManager.RemainingPercent;

            if (remaining <= 0f)    return VisionDegradationStage.Blind;
            if (remaining <= 0.25f) return VisionDegradationStage.Severe;
            if (remaining <= 0.50f) return VisionDegradationStage.Moderate;
            if (remaining <= 0.75f) return VisionDegradationStage.MinorBlur;
            return VisionDegradationStage.Clear;
        }

        // THIS is the method to replace once real shader/post-processing
        // work exists. Everything above this line can stay untouched.
        private void ApplyStage(VisionDegradationStage stage)
        {
            float alpha = placeholderOverlayAlpha[(int)stage];

            Color c = _overlayImage.color;
            c.a = alpha;
            _overlayImage.color = c;

            Debug.Log($"[VisionDegradationController] Stage → {stage} (overlay alpha {alpha:F2})");
        }
    }
}
