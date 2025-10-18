using UnityEngine;
using AI.Base;

namespace AI.States
{
    public class PatrolState : AI.Base.IState
    {
        public void Enter(AiContext context)
        {
            if (context.PatrolRoute == null || !context.PatrolRoute.HasRoute())
            {
                Debug.LogWarning($"{context.Self.name} has no patrol route!");
                context.Agent.isStopped = true;
                return;
            }

            context.Agent.isStopped = false;
            context.Agent.speed = context.patrolSpeed;
            context.PatrolRoute.ResetRoute();

            Vector3 next = context.PatrolRoute.GetNextPoint();
            context.Agent.SetDestination(next);
        }

        public void Update(AiContext context)
        {
            // Perception override
            if (context.Vision.CanSeeTarget(context.Player))
            {
                AIController.Instance.StateMachine.SetState(new ChaseState(), context);
                return;
            }

            if (context.Hearing.CanHearSound(context.Player.position))
            {
                AIController.Instance.StateMachine.SetState(new SearchState(), context);
                return;
            }

            // Patrol progression
            if (!context.Agent.pathPending && context.Agent.remainingDistance <= context.Agent.stoppingDistance)
            {
                Vector3 next = context.PatrolRoute.GetNextPoint();
                context.Agent.SetDestination(next);
            }
        }

        public void Exit(AiContext context)
        {
            context.Agent.isStopped = true;
        }
    }
}
