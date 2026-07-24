namespace JusticeIsAWeapon.Core
{
    /// <summary>
/// Represents the difficulty level of a case.
/// Placeholder: Easy selected for Case 1 ("The Midnight Gallery").
/// Swapped later: Composite Key (case number + difficulty).
/// </summary>
public enum Difficulty
{
    /// <summary>Guided experience with fewer required clues and lenient timers.</summary>
    Easy,
    /// <summary>Balanced challenge with standard clue requirements.</summary>
    Normal,
    /// <summary>Demanding investigation with more clues needed and tighter constraints.</summary>
    Hard
}
}