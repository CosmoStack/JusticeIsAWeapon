using System.Collections.Generic;
using UnityEngine;
using JusticeIsAWeapon.Enum;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// ScriptableObject for a single clue asset (e.g. "Hidden Ledger", "Scratched Lock", "Discarded Contract").
    /// One asset per clue. Tagged by type so the Clipboard knows which tab to file it under.
    /// Linked to suspects and optionally paired with another clue for cross-referencing.
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Clue Data", fileName = "Clue_")]
    public class ClueDataSO : ScriptableObject
    {
        [Header("Identity")]

        /// <summary>Unique identifier for save/load and cross-referencing. Used as a stable key across scenes and save files.</summary>
        [Tooltip("Unique identifier for save/load and cross-referencing")]
        public string clueID;

        /// <summary>Display title shown in the Clipboard UI and clue card headers (e.g. "The Hidden Ledger").</summary>
        [Tooltip("Display title shown in Clipboard UI")]
        public string clueTitle;

        /// <summary>Detective's note text displayed on the clue card. Written in first-person detective voice.</summary>
        [TextArea(2, 4)]
        [Tooltip("Detective's note text on the clue card")]
        public string detectiveNote;

        [Header("Categorization")]

        /// <summary>Filing category that determines which Clipboard tab the clue appears under (Physical / Vision / Witness / CrossReference).</summary>
        [Tooltip("Filing category for Clipboard tab filtering")]
        public ClueType type;

        /// <summary>Which Time Vision stage this clue is associated with. Use None for non-vision clues.</summary>
        [Tooltip("Related Time Vision stage. Use None for non-vision clues.")]
        public TimeStage timeStage;

        /// <summary>Crime scene location where this clue was discovered (e.g. "Private Office", "Exhibition Hall").</summary>
        [Tooltip("Crime scene location where this clue was found")]
        public string location;

        [Header("Gameplay")]

        /// <summary>If true, this clue counts toward the minimum clue threshold required to unlock the final accusation.</summary>
        [Tooltip("Counts toward the minimum clue threshold for accusation")]
        public bool isRequired;

        /// <summary>If true, this clue is misleading or false. Only included on Expert difficulty to obscure the deduction path.</summary>
        [Tooltip("Misleading clue — only used on Expert difficulty")]
        public bool isRedHerring;

        [Header("Links")]

        /// <summary>Suspect(s) implicated or linked by this clue. Drives the Deduction Board connection lines.</summary>
        [Tooltip("Suspect(s) implicated by this clue")]
        public List<SuspectDataSO> linkedSuspects;

        /// <summary>For CrossReference type clues, this is the other clue that was paired to produce the contradiction (e.g. autopsy vial + toxicology report).</summary>
        [Tooltip("Paired clue for CrossReference linking (autopsy discrepancy, etc.)")]
        public ClueDataSO crossReferenceSource;
    }
}
