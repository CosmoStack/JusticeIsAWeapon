using UnityEngine;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// Root ScriptableObject for a suspect's branching dialogue tree.
    /// One asset per suspect (e.g. "Dialogue(1)_YukiTanaka").
    /// Points to the starting node; the rest of the tree lives in referenced DialogueNodeSOs.
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Dialogue Tree", fileName = "Dialogue(X)")]
    public class DialogueTreeSO : ScriptableObject
    {
        [Header("Start Point")]

        /// <summary>First node played when the interview opens.</summary>
        public DialogueNodeSO root;

        [Header("Owner")]

        /// <summary>Back-reference to the suspect this tree belongs to.</summary>
        public SuspectDataSO suspect;
    }
}
