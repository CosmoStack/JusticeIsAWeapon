using UnityEngine;

namespace Systems.Core
{
    /// <summary>
    /// Inspector glue that lets a UnityEvent (e.g. a GameEventListener's
    /// Response) trigger CaseStateMachine.ChangeState with a chosen state.
    ///
    /// WHY THIS EXISTS:
    /// Unity's UnityEvent dropdown does not reliably list methods that take
    /// an enum parameter (ChangeState(CaseState) gets filtered out). It DOES
    /// always list zero-parameter methods. So this component holds the enum
    /// as a serialized field (renders as a normal enum dropdown in the
    /// Inspector) and exposes a no-arg Invoke() that the UnityEvent can bind.
    ///
    /// SETUP:
    /// 1. Add this to the same GameObject as the CaseStateMachine.
    /// 2. Set Next State to whichever case state the event should enter.
    /// 3. Drag the CaseStateMachine GameObject into State Machine.
    /// 4. In the GameEventListener's Response, bind to CaseStateSetter.Invoke.
    /// </summary>
    public class CaseStateSetter : MonoBehaviour
    {
        [Tooltip("The case state to enter when Invoke() is called.")]
        [SerializeField] private CaseState nextState = CaseState.Investigation;

        [Tooltip("The state machine to drive. Usually the one on this same GameObject.")]
        [SerializeField] private CaseStateMachine stateMachine;

        /// <summary>Wire this to the UnityEvent (zero parameters, always lists).</summary>
        public void Invoke()
        {
            if (stateMachine == null)
            {
                Debug.LogWarning("[CaseStateSetter] State Machine is not assigned.", this);
                return;
            }

            stateMachine.ChangeState(nextState);
        }
    }
}
