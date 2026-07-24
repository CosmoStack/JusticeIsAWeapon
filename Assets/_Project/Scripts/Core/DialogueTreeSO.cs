using UnityEngine;

namespace JusticeIsAWeapon.Core
{
    /// <summary>
    /// ScriptableObject that holds a suspect's interrogration dialogue tree.
    /// One asset per suspect (e.g. "Dialogue(1)_YukiTanaka").
    /// </summary>
    [CreateAssetMenu(menuName = "JusticeIsAWeapon/Dialogue Tree", fileName = "Dialogue(X)")]
    public class DialogueTreeSO : ScriptableObject
    {
    }
}
