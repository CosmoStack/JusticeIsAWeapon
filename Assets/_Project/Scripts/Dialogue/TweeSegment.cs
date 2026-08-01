using System;
using System.Collections.Generic;
using JusticeIsAWeapon.Data;

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// One node in a parsed Twine/Harlowe passage body.
    /// The importer turns each passage into a list of these segments;
    /// DialogueManager evaluates them at runtime (conditions, text, links).
    /// </summary>
    [Serializable]
    public class TweeSegment
    {
        public SegmentKind kind;

        /// <summary>Text: rich-text content. Link: the choice label shown on the button.</summary>
        public string text;

        /// <summary>Link only: the target passage name this link points to.</summary>
        public string linkTarget;

        /// <summary>Link only: resolved reference to the target node (filled in by the importer).</summary>
        public DialogueNodeSO nextNode;

        /// <summary>Conditional only: the if/else-if/else chain.</summary>
        public List<TweeBranch> branches;
    }

    /// <summary>
    /// One branch of a (if:)/(else-if:)/(else:) chain.
    /// </summary>
    [Serializable]
    public class TweeBranch
    {
        /// <summary>Raw Harlowe condition. Empty/null means "else" (only runs when no earlier branch matched).</summary>
        public string condition;

        /// <summary>Body segments evaluated when this branch is active.</summary>
        public List<TweeSegment> body;
    }

    public enum SegmentKind
    {
        Text,
        Link,
        Conditional
    }
}
