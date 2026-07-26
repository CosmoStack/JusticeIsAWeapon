namespace JusticeIsAWeapon.Core
{
    /// <summary>
    /// Represents the four observable stages of a crime through Time Vision.
    /// Each stage reveals different information about the events surrounding the case.
    /// Non-vision clues use None.
    /// </summary>
    public enum TimeStage
    {
        /// <summary>Default value for clues that are not obtained through Time Vision.</summary>
        None,
        /// <summary>Observes the victim's routine and location state prior to the crime. Establishes baseline and potential motives.</summary>
        Before,
        /// <summary>Observes the culprit's actions leading up to the crime, including tool gathering and planning.</summary>
        Preparation,
        /// <summary>Observes the crime itself. The perpetrator's identity remains obscured to preserve deduction.</summary>
        Execution,
        /// <summary>Observes reactions, cover-up attempts, and witnesses fleeing after the crime.</summary>
        Aftermath
    }
}
