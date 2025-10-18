using UnityEngine;
using AI.Base;

namespace AI.States
{
    public class IdleState : AI.Base.IState
    {
        public void Enter(AiContext context)
        {
            context.Agent.isStopped = true;
            context.Agent.speed = 0.5f;
            context.Animator.SetBool("isPlayerDiscovered", false);
        }

        public void Update(AiContext context)
        {
            context.CanSeePlayer = context.Vision.CanSeeTarget(context.Player);
            context.CanHearPlayer = context.Hearing.CanHearSound(context.Player.position);

            if (context.CanSeePlayer || context.CanHearPlayer)
            {
                context.LastKnownPlayerPos = context.Player.position;
                AIController.Instance.StateMachine.SetState(new ChaseState(), context);
            }
        }

        public void Exit(AiContext context) { }
    }
}
