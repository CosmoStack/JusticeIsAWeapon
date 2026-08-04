// ============================================================================
// ComicSequencePlayer.cs
//
// WHAT THIS SCRIPT DOES:
// Plays back any ComicPanelSequenceSO, one panel at a time: fade in image
// + caption, hold for a while (or until the player taps), fade out, move
// to the next panel. When every panel has played, it fires
// OnSequenceComplete so the CaseStateMachine (or whatever called this)
// knows it's time to move on.
//
// THIS EXACT SAME COMPONENT drives both:
//   - Scene 1: the Act I opening gala cutscene
//   - Scene 9: the Act II Time Vision reveal
// The only difference between the two is which ComicPanelSequenceSO
// asset gets passed into Play().
//
// ACTUAL DATA SHAPE (see ComicPanelDataSO.cs / ComicPanelSequenceSO.cs):
//   public struct ComicPanelDataSO {
//       public ComicPanelType panelType;
//       public Sprite image;
//       public VideoClip videoClip;
//       public string captionText;
//       public float holdDuration;
//       public string sfxCueId;
//   }
//   public class ComicPanelSequenceSO : ScriptableObject {
//       public List<ComicPanelDataSO> panels;
//   }
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JusticeIsAWeapon.Data;
using JusticeIsAWeapon.Events;

namespace JusticeIsAWeapon.Cinematic
{
    public class ComicSequencePlayer : MonoBehaviour
    {
        [Header("UI References (drag from this player's children)")]
        [SerializeField] private Image panelImageDisplay;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private PanelTransitionEffects transitionEffects;

        [Header("Placeholder Look (used when a panel has no image yet)")]
        [SerializeField] private Color placeholderColor = Color.gray;

        [Header("Accessibility")]
        [Tooltip("If true, skips fades and lets the player tap through every panel instantly. Wire this to the 'Reduce Motion' setting later.")]
        [SerializeField] private bool reduceMotion = false;

        [Tooltip("If true, panels advance automatically after their hold duration. If false, the player must tap to advance.")]
        [SerializeField] private bool autoAdvance = true;

        [Header("Fired when the whole sequence finishes")]
        [SerializeField] private GameEventSO onSequenceComplete;

        private bool _tapReceived;

        // The player (or a UI Button covering the screen) calls this on tap/click.
        public void OnTapToAdvance()
        {
            _tapReceived = true;
        }

        /// <summary>
        /// Starts playing the given sequence from panel 1.
        /// Call this from wherever a scene decides it's time for a cutscene,
        /// e.g. CaseStateMachine.ChangeState(CaseState.Cinematic) followed by
        /// comicPlayer.Play(openingSequence).
        /// </summary>
        public void Play(ComicPanelSequenceSO sequence)
        {
            StartCoroutine(PlaySequenceRoutine(sequence));
        }

        private IEnumerator PlaySequenceRoutine(ComicPanelSequenceSO sequence)
        {
            List<ComicPanelDataSO> panels = sequence.panels;

            for (int i = 0; i < panels.Count; i++)
            {
                yield return PlaySinglePanel(panels[i]);
            }

            onSequenceComplete?.Raise();
        }

        private IEnumerator PlaySinglePanel(ComicPanelDataSO panel)
        {
            // 1. Set the panel's visuals BEFORE fading in, so we fade
            //    into the correct image/caption rather than the old one.
            ApplyPanelContent(panel);

            // 2. Fade in.
            yield return transitionEffects.FadeIn(reduceMotion);

            // 3. Hold — either for a fixed time, or until the player taps,
            //    whichever the scene is configured to use.
            _tapReceived = false;
            float elapsed = 0f;

            while (true)
            {
                if (_tapReceived)
                {
                    break; // player tapped — always allowed to skip ahead
                }

                if (autoAdvance)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= panel.holdDuration)
                    {
                        break;
                    }
                }

                yield return null;
            }

            // 4. Fade out before the next panel takes over.
            yield return transitionEffects.FadeOut(reduceMotion);
        }

        // Fills in the image and caption for one panel. If there's no
        // image yet (Art hasn't delivered it), shows a plain gray
        // rectangle instead of a broken/empty image.
        private void ApplyPanelContent(ComicPanelDataSO panel)
        {
            bool hasImage = panel.image != null;

            panelImageDisplay.sprite = panel.image;
            panelImageDisplay.color = hasImage ? Color.white : placeholderColor;

            captionText.text = panel.captionText;
        }
    }
}