using System.Collections.Generic;
using JusticeIsAWeapon.Enum;
using UnityEngine;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// ScriptableObject for a single Time Vision playback instance (e.g. "Act II Vase Strike", "Act III Back Door Tamper").
    /// One asset per vision. Maps a TimeStage to the clue it grants, the comic sequence it plays, and the cost to view it.
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Vision Stage Data", fileName = "VisionStage_")]
    public class VisionStageDataSO : ScriptableObject
    {
        [Header("Stage Identity")]

        /// <summary>Which Time Vision stage this belongs to (Before / Preparation / Execution / Aftermath). Determines narrative placement within the timeline.</summary>
        [Tooltip("Which Time Vision stage this belongs to")]
        public TimeStage stage;

        /// <summary>Narrative description of what the detective observes during this vision. Displayed in the UI before playback.</summary>
        [TextArea(2, 4)]
        [Tooltip("Narrative description of what the detective observes")]
        public string description;

        /// <summary>Post-vision journal note written in first-person detective voice. May differ from the raw description.</summary>
        [TextArea(2, 4)]
        [Tooltip("Detective's note appended to the journal after viewing")]
        public string narrativeSummary;

        [Header("Rewards")]

        /// <summary>The clue asset granted to the player upon completing this vision playback.</summary>
        [Tooltip("Clue granted after viewing this vision")]
        public ClueDataSO grantedClue;

        /// <summary>The comic panel sequence played during this vision's playback.</summary>
        [Tooltip("Comic panel sequence played during playback")]
        public ComicPanelSequenceSO comicSequence;

        [Header("Cost & Access")]

        /// <summary>Vision charges consumed when viewing (0 for the initial free vision per stage, 1+ for revisits).</summary>
        [Tooltip("Vision charges consumed (0 = free, 1+ = costs charges)")]
        public int chargeCost;

        /// <summary>If true, this vision is available immediately. If false, the player must collect all Required Clues first.</summary>
        [Tooltip("Available immediately without prerequisites")]
        public bool isUnlockedByDefault = true;

        /// <summary>Clues that must be in the journal before this vision becomes interactable. Leave empty if no prerequisites.</summary>
        [Tooltip("Clues required before this vision unlocks")]
        public List<ClueDataSO> requiredClues;

        /// <summary>Minimum vision quality index required to view this vision (1 = Crystal Clear, 6 = Complete Blindness). Visions above the player's current quality level are blocked.</summary>
        [Tooltip("Minimum vision quality required (1=Crystal Clear … 6=Blindness)")]
        public int qualityThreshold = 1;

        [Header("Audio")]

        /// <summary>Audio cue identifier played on vision activation (e.g. pocket-watch rewind, ambient layer). Matches a key in the AudioManager cue table.</summary>
        [Tooltip("Audio cue played on activation")]
        public string sfxCueId;
    }
}
