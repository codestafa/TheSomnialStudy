using UnityEngine;
using AI.Base;

namespace AI.States
{
    public class ChaseState : AI.Base.IState
    {
        public void Enter(AiContext context)
        {
            context.Agent.isStopped = false;
            context.Agent.speed = context.chaseSpeed;
            context.Animator.SetBool("isPlayerDiscovered", true);
        }

        public void Update(AiContext context)
        {
            if (context.Player == null || context.Agent == null)
                return;

            // Continuously check perception
            context.CanSeePlayer = context.Vision.CanSeeTarget(context.Player);
            context.CanHearPlayer = context.Hearing.CanHearSound(context.Player.position);

            // Always update destination toward player if visible or audible
            if (context.CanSeePlayer || context.CanHearPlayer)
            {
                context.LastKnownPlayerPos = context.Player.position;
                context.Agent.SetDestination(context.Player.position);
            }
            else
            {
                // Lost both sight and sound — begin search
                context.SearchTimer = context.searchDuration;
                AIController.Instance.StateMachine.SetState(new SearchState(), context);
            }
        }

        public void Exit(AiContext context)
        {
            context.Agent.isStopped = true;
            context.Animator.SetBool("isPlayerDiscovered", false);
        }
    }
}
