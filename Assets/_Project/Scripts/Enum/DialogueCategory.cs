namespace JusticeIsAWeapon.Enum
{
    /// <summary>
    /// Determines when a dialogue node is accessible during an interview.
    /// </summary>
    public enum DialogueCategory
    {
        /// <summary>Always available — Alibi, Timeline, Relationship baseline questions.</summary>
        Baseline,

        /// <summary>Only appears when the player has dragged a matching clue onto the Interrogation tab.</summary>
        ClueTriggered,

        /// <summary>Special sequence with unique flow (e.g. Elena's breakdown).</summary>
        Confrontation
    }
}
