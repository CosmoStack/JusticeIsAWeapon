using System;
using System.Collections.Generic;
using JusticeIsAWeapon.Data;

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// One flat "render block" of a passage: text + links that are shown
    /// together when <see cref="condition"/> passes. Produced at import time
    /// by flattening the (if:)/(else-if:)/(else:) nesting, so the stored
    /// condition is already the fully combined expression — Unity never sees
    /// a recursive class hierarchy (avoids the serialization depth limit).
    /// </summary>
    [Serializable]
    public class DialogueBlock
    {
        /// <summary>Combined Harlowe condition. Null/empty = always shown.</summary>
        public string condition;

        /// <summary>Rich-text content shown in the dialogue box.</summary>
        public string text;

        /// <summary>Choices presented together with this text, in order.</summary>
        public List<LinkData> links;
    }

    /// <summary>
    /// One clickable link inside a block. The target node reference is
    /// resolved by the importer after every node has been created.
    /// </summary>
    [Serializable]
    public class LinkData
    {
        /// <summary>The label shown on the choice button.</summary>
        public string label;

        /// <summary>The target passage name as authored in the Twee file.</summary>
        public string target;

        /// <summary>Resolved reference to the target node (filled in by the importer).</summary>
        public DialogueNodeSO node;
    }
}
