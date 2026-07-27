using System.Collections.Generic;
using UnityEngine;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// An ordered list of panels for a comic-style cinematic sequence.
    /// Reused for the Act I opening (5 panels) and the Act II Time Vision reveal (5 panels).
    /// A sequencer MonoBehaviour plays any ComicPanelSequenceSO regardless of content.
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Comic Sequence", fileName = "ComicSeq_(X)")]
    public class ComicPanelSequenceSO : ScriptableObject
    {
        /// <summary>Panels play in list order from index 0 onward.</summary>
        public List<ComicPanelDataSO> panels;
    }
}
