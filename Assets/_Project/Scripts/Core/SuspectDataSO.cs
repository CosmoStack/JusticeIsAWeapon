using UnityEngine;

namespace JusticeIsAWeapon.Core
{
    /// <summary>
    /// ScriptableObject representing a single suspect in a case.
    /// One asset per suspect (e.g. "Suspect(1)_YukiTanaka").
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Suspect Data", fileName = "Suspect(X)")]
    public class SuspectDataSO : ScriptableObject
    {
        [Header("Identity")]

        /// <summary>Full display name shown in the case file and interrogation UI.</summary>
        public string suspectName;

        /// <summary>Occupation or social role (e.g. "Assistant Curator", "Security Chief").</summary>
        public string roleTitle;

        /// <summary>How this person relates to the victim (e.g. "Brother", "Ex-wife", "Employee").</summary>
        public string relationshipToVictim;

        [Header("Investigation")]

        /// <summary>Short summary of why this person might have wanted the victim dead.</summary>
        [TextArea(3, 5)]
        public string motive;

        /// <summary>The suspect's stated whereabouts during the time of death. Cross-referenced during deduction.</summary>
        [TextArea(3, 5)]
        public string alibi;

        /// <summary>Behavioural tell the investigator notices during interrogation. Feeds the Deduction Clipboard.</summary>
        [TextArea(2, 4)]
        public string bodyLanguage;

        [Header("Accusation")]

        /// <summary>One-line reaction played when the player accuses this suspect. Varies by guilt in the case data.</summary>
        [TextArea(2, 4)]
        public string accusationResponse;

        [Header("Dialogue")]

        /// <summary>Reference to the dialogue tree used during witness interviews for this suspect.</summary>
        public DialogueTreeSO dialogueTree;

        [Header("Visual")]

        /// <summary>Character portrait sprite. Placeholder cube icon until final art is delivered.</summary>
        public Sprite portrait;
    }
}
