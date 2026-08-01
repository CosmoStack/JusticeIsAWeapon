using System;
using System.Collections.Generic;
using System.Text;
using JusticeIsAWeapon.Data;
using UnityEngine;

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// Runtime engine that walks a DialogueTreeSO node by node, tracks which
    /// passages have been visited, evaluates the imported Harlowe conditions,
    /// and exposes the current text + available choices to the UI.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("State (read-only while playing)")]
        public DialogueTreeSO CurrentTree { get; private set; }
        public DialogueNodeSO CurrentNode { get; private set; }

        /// <summary>Fully evaluated text for the current node (rich text, ready for TMP).</summary>
        public string CurrentText { get; private set; }

        /// <summary>Choices currently visible, after evaluating every (if:) branch.</summary>
        public IReadOnlyList<DialogueChoice> AvailableChoices => _availableChoices;

        /// <summary>Passages visited so far, in play order. Drives (history:) conditions.</summary>
        public IReadOnlyList<string> VisitedPassages => _visited;

        public bool IsActive { get; private set; }

        /// <summary>Fired after the current node's text/choices have been evaluated.</summary>
        public event Action<DialogueNodeSO> OnNodeChanged;

        /// <summary>Fired whenever the available choice list is rebuilt.</summary>
        public event Action OnChoicesRefreshed;

        /// <summary>Fired when the tree hits a node with no available choices (a dead end).</summary>
        public event Action OnDialogueEnded;

        public struct DialogueChoice
        {
            public string label;
            public DialogueNodeSO target;
        }

        private readonly List<DialogueChoice> _availableChoices = new List<DialogueChoice>();
        private readonly List<string> _visited = new List<string>();
        private readonly Dictionary<string, bool> _vars = new Dictionary<string, bool>();
        private readonly StringBuilder _builder = new StringBuilder(512);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[DialogueManager] A DialogueManager already exists. Destroying the duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Starts (or restarts) a dialogue tree from its root node.</summary>
        public void StartTree(DialogueTreeSO tree)
        {
            if (tree == null)
            {
                Debug.LogError("[DialogueManager] StartTree called with no tree.");
                return;
            }
            if (tree.root == null)
            {
                Debug.LogError($"[DialogueManager] Tree '{tree.name}' has no root node.");
                return;
            }

            CurrentTree = tree;
            _visited.Clear();
            _vars.Clear();
            IsActive = true;
            EnterNode(tree.root);
        }

        /// <summary>Selects one of the currently available choices and advances to its target node.</summary>
        public void SelectChoice(int index)
        {
            if (!IsActive || index < 0 || index >= _availableChoices.Count)
            {
                return;
            }

            DialogueChoice choice = _availableChoices[index];
            if (choice.target == null)
            {
                EndTree();
                return;
            }
            EnterNode(choice.target);
        }

        /// <summary>Ends the dialogue and reports the visited path (dead-end reporting for the state machine).</summary>
        public void EndTree()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            CurrentText = null;
            _availableChoices.Clear();
            OnChoicesRefreshed?.Invoke();

            Debug.Log($"[DialogueManager] Dialogue ended. Passages visited: {string.Join(" -> ", _visited)}");
            OnDialogueEnded?.Invoke();
        }

        /// <summary>True if the given passage has been visited during this run of the tree.</summary>
        public bool HasVisited(string passageId)
        {
            return !string.IsNullOrEmpty(passageId) && _visited.Contains(passageId);
        }

        /// <summary>Looks up a computed story variable (e.g. _keyFirst). Unknown variables are false.</summary>
        public bool GetVar(string name)
        {
            return !string.IsNullOrEmpty(name) && _vars.TryGetValue(name, out bool value) && value;
        }

        private void EnterNode(DialogueNodeSO node)
        {
            if (node == null)
            {
                EndTree();
                return;
            }

            CurrentNode = node;
            _visited.Add(node.nodeId);
            RecomputeVars();
            EvaluateNode(node);

            Debug.Log($"[DialogueManager] Node: {node.nodeId} | available choices: {_availableChoices.Count}");
            OnNodeChanged?.Invoke(node);

            // No choices at all (or every branch gated off) = dead end.
            if (_availableChoices.Count == 0)
            {
                EndTree();
            }
        }

        /// <summary>
        /// Recomputes the story variables from the visited-history, mirroring the
        /// (set:) / (for: each ... (history:)) logic the author used in the tree.
        /// </summary>
        private void RecomputeVars()
        {
            int indexOf(string name) => _visited.IndexOf(name);
            bool has(string name) => _visited.Contains(name);

            _vars.Clear();
            _vars["_decided"] = has("Key Clue Unlock") || has("Elena - Contract");
            _vars["_keyFirst"] = has("Key Clue Unlock")
                && (indexOf("Elena - Contract") < 0 || indexOf("Key Clue Unlock") < indexOf("Elena - Contract"));
            _vars["_contractClue"] = has("Elena - Contract");
            _vars["_interrogationAfterClue"] = indexOf("Elena - Contract") >= 0
                && indexOf("Interrogation Room") > indexOf("Elena - Contract");
            _vars["_galleryAfterClue"] = indexOf("Elena - Contract") >= 0
                && indexOf("Gallery Exterior") > indexOf("Elena - Contract");
            _vars["_corridorSeen"] = has("The Restricted Corridor Door");
            _vars["_galleryAfterCorridor"] = indexOf("The Restricted Corridor Door") >= 0
                && indexOf("Gallery Exterior") > indexOf("The Restricted Corridor Door");
        }

        private void EvaluateNode(DialogueNodeSO node)
        {
            _builder.Clear();
            _availableChoices.Clear();

            if (node.segments != null)
            {
                WalkSegments(node.segments);
            }

            CurrentText = _builder.ToString();
        }

        private void WalkSegments(List<TweeSegment> segments)
        {
            foreach (TweeSegment segment in segments)
            {
                switch (segment.kind)
                {
                    case SegmentKind.Text:
                        _builder.Append(segment.text);
                        break;

                    case SegmentKind.Link:
                        _availableChoices.Add(new DialogueChoice
                        {
                            label = segment.text,
                            target = segment.nextNode
                        });
                        break;

                    case SegmentKind.Conditional:
                        WalkConditional(segment);
                        break;
                }
            }
        }

        private void WalkConditional(TweeSegment segment)
        {
            if (segment.branches == null)
            {
                return;
            }

            bool entered = false;
            foreach (TweeBranch branch in segment.branches)
            {
                bool active = string.IsNullOrEmpty(branch.condition)
                    ? !entered
                    : ConditionEvaluator.Evaluate(branch.condition, HasVisited, GetVar);

                if (!active)
                {
                    continue;
                }

                if (branch.body != null)
                {
                    WalkSegments(branch.body);
                }
                entered = true;
                break;
            }
        }
    }
}
