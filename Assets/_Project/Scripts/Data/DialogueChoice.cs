using System;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// One selectable response that advances the conversation to a child node.
    /// </summary>
    [Serializable]
    public struct DialogueChoice
    {
        /// <summary>Text shown on the topic/choice button (e.g. "Ask about the contract").</summary>
        public string choiceLabel;

        /// <summary>The child node this choice leads to. Null ends the branch.</summary>
        public DialogueNodeSO nextNode;
    }
}
