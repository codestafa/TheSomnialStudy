using AI.Base;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace AI.States
{
    public class ReturnState : AI.Base.IState
    {
        public void Enter(AiContext context)
        {
            context.Agent.isStopped = false;
            context.Agent.speed = 0.52f;
            context.Agent.SetDestination(context.InitialPosition);
        }

        public void Update(AiContext context)
        {
            if (context.Vision.CanSeeTarget(context.Player) || context.Hearing.CanHearSound(context.Player.position))
            {
                AIController.Instance.StateMachine.SetState(new ChaseState(), context);
                return;
            }

            if (!context.Agent.pathPending && context.Agent.remainingDistance <= context.Agent.stoppingDistance)
            {
                AIController.Instance.StateMachine.SetState(new IdleState(), context);
            }
        }

        public void Exit(AiContext context) { }
    }
}
