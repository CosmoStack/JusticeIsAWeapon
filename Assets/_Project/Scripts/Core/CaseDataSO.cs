using System.Collections.Generic;
using UnityEngine;

namespace JusticeIsAWeapon.Core
{
    /// <summary>
    /// Root ScriptableObject for a case. One asset per case (e.g. "The Midnight Gallery").
    /// Everything else (suspects, clues, comics) hangs off this object via references.
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Case Data", fileName = "Case(X)")]
    public class CaseDataSO : ScriptableObject
    {
        [Header("Case Info")]

        /// <summary>Unique identifier for sorting and UI indexing. Matches case select screen order.</summary>
        public int caseNumber;

        /// <summary>Display title shown on case select screen and briefing (e.g. "The Midnight Gallery").</summary>
        public string caseTitle;

        /// <summary>Primary crime scene location name (e.g. "Whitmore Museum of Contemporary Art").</summary>
        public string locationName;

        /// <summary>Brief synopsis for the case select card UI.</summary>
        [TextArea(3, 6)]
        public string description;

        /// <summary>Case difficulty tier — affects hint availability and clue requirements.</summary>
        public Difficulty difficulty;

        [Header("Victim")]

        /// <summary>Full name of the victim (e.g. "Marcus Webb").</summary>
        public string victimName;

        /// <summary>Estimated time of death as displayed in the case file (e.g. "11:20 PM").</summary>
        public string timeOfDeath;

        /// <summary>Cause of death summary for the briefing screen (e.g. "Blunt force trauma").</summary>
        public string causeOfDeath;

        [Header("Progression")]

        /// <summary>Number of distinct clues the player must collect before unlocking the final accusation.</summary>
        public int requiredClueCount;

        [Header("Content References")]

        /// <summary>All suspects involved in this case. References are assigned in the inspector.</summary>
        public List<SuspectDataSO> suspects;

        /// <summary>All discoverable clues for this case. Tagged by type for the Clipboard sorter.</summary>
        public List<ClueDataSO> clues;

        /// <summary>Optional comic panel sequence that plays at the start of the case (e.g. Act I opening cinematic).</summary>
        public ComicPanelSequenceSO openingComicSequence;
    }
}