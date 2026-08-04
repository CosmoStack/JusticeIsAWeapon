// ============================================================================
// CinematicTestTrigger.cs
//
// WHAT THIS SCRIPT DOES:
// A temporary test hook that plays a ComicPanelSequenceSO the moment the
// scene starts. Drag a Comic Sequence asset into "Sequence", hit Play, and
// the cutscene runs immediately.
//
// THIS IS A TEMPORARY SCRIPT. When the real flow exists (e.g. the
// CaseStateMachine entering CaseState.Cinematic), replace the Start() call
// with the proper trigger point and delete this file.
// ============================================================================

using UnityEngine;
using JusticeIsAWeapon.Data;

namespace JusticeIsAWeapon.Cinematic
{
    public class CinematicTestTrigger : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private ComicSequencePlayer comicSequencePlayer;

        [Tooltip("The sequence to play on scene start.")]
        [SerializeField] private ComicPanelSequenceSO sequence;

        private void Start()
        {
            if (comicSequencePlayer == null)
            {
                comicSequencePlayer = GetComponent<ComicSequencePlayer>();
            }

            if (sequence == null)
            {
                Debug.LogWarning("[CinematicTestTrigger] No sequence assigned. Add the OpeningCutscene_ActI asset.", this);
                return;
            }

            comicSequencePlayer.Play(sequence);
        }
    }
}
