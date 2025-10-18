using UnityEngine;
using UnityEngine.AI;
using AI.Base;

namespace AI.States
{
    public class SearchState : IState
    {
        public void Enter(AiContext context)
        {
            context.Agent.isStopped = false;
            context.Agent.speed = context.searchSpeed;
            context.SearchTimer = context.searchDuration;

            // Generate points around last known player position
            context.SearchPoints = GenerateSearchPoints(context.LastKnownPlayerPos, context);
            context.SearchIndex = 0;

            if (context.SearchPoints.Length > 0)
            {
                context.Agent.SetDestination(context.SearchPoints[0]);
            }

            Debug.Log($"[AI FSM] {context.Self.name} entered SearchState - searching {context.SearchPoints.Length} points.");
        }

        public void Update(AiContext context)
        {
            context.SearchTimer -= Time.deltaTime;

            // --- 1. Detect Player Again ---
            if (context.Vision.CanSeeTarget(context.Player) || context.Hearing.CanHearSound(context.Player.position))
            {
                AIController.Instance.StateMachine.SetState(new ChaseState(), context);
                return;
            }

            // --- 2. Timeout: switch to Patrol if available, else Return ---
            if (context.SearchTimer <= 0f)
            {
                if (context.PatrolRoute != null && context.PatrolRoute.HasRoute())
                {
                    Debug.Log($"[AI FSM] {context.Self.name} switching from SearchState → PatrolState");
                    AIController.Instance.StateMachine.SetState(new PatrolState(), context);
                }
                else
                {
                    Debug.Log($"[AI FSM] {context.Self.name} switching from SearchState → ReturnState");
                    AIController.Instance.StateMachine.SetState(new ReturnState(), context);
                }
                return;
            }

            // --- 3. Move Between Search Points ---
            if (!context.Agent.pathPending && context.Agent.remainingDistance <= context.Agent.stoppingDistance)
            {
                context.SearchIndex++;
                if (context.SearchIndex < context.SearchPoints.Length)
                {
                    context.Agent.SetDestination(context.SearchPoints[context.SearchIndex]);
                }
                else
                {
                    // Re-roll random points around self once all are visited
                    context.SearchPoints = GenerateSearchPoints(context.Self.position, context);
                    context.SearchIndex = 0;

                    if (context.SearchPoints.Length > 0)
                        context.Agent.SetDestination(context.SearchPoints[0]);
                }
            }
        }

        public void Exit(AiContext context)
        {
            context.Agent.isStopped = true;
        }

        // --- Helper: Generate random NavMesh positions around a center ---
        private Vector3[] GenerateSearchPoints(Vector3 center, AiContext context)
        {
            Vector3[] points = new Vector3[context.MaxSearchPoints];
            int valid = 0;

            for (int i = 0; i < context.MaxSearchPoints * 3 && valid < context.MaxSearchPoints; i++)
            {
                Vector3 random = center + Random.insideUnitSphere * context.SearchRadius;
                random.y = center.y;

                if (NavMesh.SamplePosition(random, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    points[valid++] = hit.position;
                }
            }

            System.Array.Resize(ref points, valid);
            return points;
        }
    }
}
