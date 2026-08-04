// ============================================================================
// VisionDegradationStage.cs
//
// The five visual clarity stages from GDD §4.2/§10.3. VisionDegradationController
// maps "how many charges are left" onto one of these.
// ============================================================================

namespace JusticeIsAWeapon.Enum
{
    public enum VisionDegradationStage
    {
        /// <summary>All charges remaining. Full visual fidelity, no overlay.</summary>
        Clear,

        /// <summary>One charge spent. Subtle gray tint begins — barely noticeable.</summary>
        MinorBlur,

        /// <summary>Roughly half the charges gone. Gray overlay clearly visible.</summary>
        Moderate,

        /// <summary>One charge left. Heavy overlay — vision noticeably compromised.</summary>
        Severe,

        /// <summary>No charges remaining. Full gray overlay. Blind Phase triggered.</summary>
        Blind
    }
}
