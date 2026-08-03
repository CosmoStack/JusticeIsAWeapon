using System.Collections.Generic;
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

        [Tooltip("Jump straight to this node (by nodeId). Leave empty to start at the tree root.")]
        public string startNodeId = "Interview Elena";

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

            DialogueManager manager = DialogueManager.Instance;
            if (string.IsNullOrEmpty(startNodeId))
            {
                manager.StartTree(dialogueTree);
            }
            else
            {
                DialogueNodeSO start = FindNode(dialogueTree.root, startNodeId, new HashSet<string>());
                if (start != null)
                {
                    manager.StartTreeAt(dialogueTree, start);
                }
                else
                {
                    Debug.LogWarning($"[TestDialogueDriver] Node '{startNodeId}' not found — starting at the tree root.");
                    manager.StartTree(dialogueTree);
                }
            }

            SeedDemoClues();
        }

        /// <summary>Depth-first search over imported block links, looking up a node by its passage id.</summary>
        private static DialogueNodeSO FindNode(DialogueNodeSO node, string id, HashSet<string> visited)
        {
            if (node == null)
            {
                return null;
            }
            if (!visited.Add(node.nodeId))
            {
                return null;
            }
            if (node.nodeId == id)
            {
                return node;
            }
            if (node.blocks != null)
            {
                foreach (DialogueBlock block in node.blocks)
                {
                    if (block.links == null)
                    {
                        continue;
                    }
                    foreach (LinkData link in block.links)
                    {
                        DialogueNodeSO found = FindNode(link.node, id, visited);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Puts a few demo clue tiles in the clue bar so the conversation
        /// layout is immediately visible in the test scene.
        /// </summary>
        private void SeedDemoClues()
        {
            DialogueUIController ui = GetComponent<DialogueUIController>();
            if (ui == null)
            {
                return;
            }
            ui.AddClue(CreateDemoClue("The Restricted Corridor Door", "A fire door to the staff-only corridor, locked from the inside."));
            ui.AddClue(CreateDemoClue("Asset Liquidation Contract", "A Vance Wealth Management liquidation contract, dated yesterday."));
            ui.AddClue(CreateDemoClue("Master Key Fingerprints", "Fresh gouges on the keyhole cylinder — someone was in a hurry."));
        }

        private static ClueDataSO CreateDemoClue(string title, string note)
        {
            ClueDataSO clue = ScriptableObject.CreateInstance<ClueDataSO>();
            clue.clueID = title;
            clue.clueTitle = title;
            clue.detectiveNote = note;
            return clue;
        }
    }
}
