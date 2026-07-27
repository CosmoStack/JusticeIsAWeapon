using System.Collections.Generic;
using JusticeIsAWeapon.Enum;
using UnityEngine;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// A single beat in a branching dialogue tree.
    /// One asset per beat (e.g. "DN(1)_Yuki_Baseline_Alibi").
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Dialogue Node", fileName = "DN(X)_CharacterName")]
    public class DialogueNodeSO : ScriptableObject
    {
        [Header("Identity")]

        /// <summary>Unique identifier for Twine export mapping. Must match the source Twine node ID.</summary>
        public string nodeId;

        [Header("Content")]

        /// <summary>Who is speaking (e.g. "Yuki", "Miller", "Narrator").</summary>
        public string speakerName;

        /// <summary>The dialogue line displayed in the speech bubble.</summary>
        [TextArea(2, 6)]
        public string lineText;

        [Header("Access Control")]

        /// <summary>Controls when this node is visible in the interview flow.</summary>
        public DialogueCategory category;

        /// <summary>
        /// If set, this node only appears when the player has this clue
        /// and drags it onto Miller's Interrogation tab.
        /// Only used when category = ClueTriggered.
        /// </summary>
        public ClueDataSO requiredClue;

        [Header("Rewards")]

        /// <summary>
        /// If set, this dialogue grants this clue to the player's inventory
        /// when the node plays (e.g. Elena's confession becomes a Witness clue).
        /// </summary>
        public ClueDataSO clueRevealed;

        [Header("Interrogation Cues")]

        /// <summary>
        /// Per-node body language tell displayed on the interview screen.
        /// Overrides the suspect's static bodyLanguage when set.
        /// </summary>
        [TextArea(1, 3)]
        public string bodyLanguageOverride;

        [Header("Branching")]

        /// <summary>
        /// If true, no further choices are shown — the interview returns to
        /// the investigation phase after this line.
        /// </summary>
        public bool isDeadEnd;

        /// <summary>Player/Miller response options leading to child nodes. Empty + isDeadEnd = conversation ends.</summary>
        public List<DialogueChoice> choices;
    }
}
