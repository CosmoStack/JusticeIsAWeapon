namespace JusticeIsAWeapon.Core
{
    /// <summary>
    /// Categorizes a clue for Clipboard tab filtering and UI sorting.
    /// Determines which tab the clue card appears under in the Deduction Clipboard.
    /// </summary>
    public enum ClueType
    {
        /// <summary>Tangible evidence found at the crime scene (e.g. hidden ledger, scratched lock, discarded contract).</summary>
        Physical,
        /// <summary>Clue revealed through Time Vision playback (e.g. witnessing the strike, the staging, the escape).</summary>
        Vision,
        /// <summary>Statement or body-language tell obtained from witness interviews (e.g. Yuki's composure, Thomas's deflection).</summary>
        Witness,
        /// <summary>Clue derived by cross-referencing two pieces of evidence (e.g. autopsy report contradicts planted vial narrative).</summary>
        CrossReference
    }
}
