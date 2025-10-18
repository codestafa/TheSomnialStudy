using UnityEngine;

namespace AI.Base
{
    public class AiStateMachine
    {
        private IState currentState;

        public void SetState(IState newState, AiContext context)
        {
            if (currentState == newState) return;

            Debug.Log($"[AI FSM] {context.Self.name} switching from {currentState?.GetType().Name ?? "None"} to {newState.GetType().Name}");

            currentState?.Exit(context);
            currentState = newState;
            currentState?.Enter(context);
        }

        public void Update(AiContext context)
        {
            currentState?.Update(context);
        }

        public IState GetCurrentState() => currentState;
    }
}
