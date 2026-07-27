using JusticeIsAWeapon.Enum;
using UnityEngine;
using UnityEngine.Video;

namespace JusticeIsAWeapon.Data
{
    /// <summary>
    /// A single panel in a comic-style cinematic sequence.
    /// Supports both static images and video clips.
    /// </summary>
    [System.Serializable]
    public struct ComicPanelDataSO
    {
        [Header("Type")]

        /// <summary>Whether this panel displays a static sprite or plays a video clip.</summary>
        public ComicPanelType panelType;

        [Header("Visual")]

        /// <summary>Panel illustration. Used when panelType = Image. Gray box placeholder until final art.</summary>
        public Sprite image;

        /// <summary>Video footage. Used when panelType = Video. Null otherwise.</summary>
        public VideoClip videoClip;

        [Header("Text")]

        /// <summary>Caption or dialogue text displayed on or beneath the panel.</summary>
        [TextArea(2, 5)]
        public string captionText;

        [Header("Timing")]

        /// <summary>How long this panel stays on screen before advancing (seconds).</summary>
        public float holdDuration;

        [Header("Audio")]

        /// <summary>Optional identifier for an SFX to play when this panel appears. Empty string = no sound.</summary>
        public string sfxCueId;
    }
}
