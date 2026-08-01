using JusticeIsAWeapon.Data;
using UnityEngine;

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// Simple test entry point: starts a dialogue tree as soon as the scene
    /// runs. Used by the generated DialogueTest scene; game scenes later wire
    /// the same DialogueManager to their own triggers instead.
    /// </summary>
    public class TestDialogueDriver : MonoBehaviour
    {
        [Header("Tree to start on Play")]
        public DialogueTreeSO dialogueTree;

        private void Start()
        {
            if (dialogueTree == null)
            {
                Debug.LogError("[TestDialogueDriver] No dialogue tree assigned.");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[TestDialogueDriver] No DialogueManager found in the scene.");
                return;
            }

            DialogueManager.Instance.StartTree(dialogueTree);
        }
    }
}
