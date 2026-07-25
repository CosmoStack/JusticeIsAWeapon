// ============================================================================
// StringGameEventSO.cs
//
// A working, ready-to-use example of a TYPED event — this one carries a
// piece of text. Useful right now for things like "InterviewCompletedEvent"
// (carries the suspect's name) before SuspectDataSO exists.
//
// COPY THIS PATTERN when you need a new data type later (e.g. once
// ClueDataSO or SuspectDataSO exist in Section B) — swap every `string`
// below for the new type and rename the file/class.
// ============================================================================

using UnityEngine;

namespace Systems.Events
{
    [CreateAssetMenu(fileName = "NewStringGameEvent", menuName = "Systems/Game Event (with string)")]
    public class StringGameEventSO : GameEventSO<string>
    {
    }
}