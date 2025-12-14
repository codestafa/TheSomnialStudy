using UnityEngine;
using AI.Base;

namespace AI.States
{
    public class ChaseState : AI.Base.IState
    {
        private float pathUpdateInterval = 0.1f; // Update path 10 times per second for responsive chasing
        private float lastPathUpdateTime;
        private float lostPlayerTimer;
        private const float lostPlayerGracePeriod = 2.0f; // Longer grace period - AI doesn't give up easily
        private const float catchDistance = 1.8f; // Distance at which AI catches player

        // Pursuit prediction
        private Vector3 predictedPlayerPos;
        private Vector3 lastPlayerVelocity;

        public void Enter(AiContext context)
        {
            context.Agent.isStopped = false;
            context.Agent.speed = context.chaseSpeed;
            context.Agent.acceleration = 12f; // Higher acceleration for snappy response
            context.Agent.angularSpeed = 360f; // Fast turning during chase
            context.Animator.SetBool("isPlayerDiscovered", true);
            lastPathUpdateTime = -pathUpdateInterval; // Force immediate path update
            lostPlayerTimer = 0f;
            predictedPlayerPos = context.Player.position;
            lastPlayerVelocity = Vector3.zero;

            // Enable chase mode for wider vision
            if (context.Vision != null)
            {
                context.Vision.SetChaseMode(true);
            }
        }

        public void Update(AiContext context)
        {
            if (context.Player == null || context.Agent == null)
                return;

            // Check if AI caught the player
            float distanceToPlayer = Vector3.Distance(context.Self.position, context.Player.position);
            if (distanceToPlayer <= catchDistance)
            {
                CatchPlayer(context);
                return;
            }

            // Continuously check perception - during chase, use extended range check as backup
            context.CanSeePlayer = context.Vision.CanSeeTarget(context.Player);
            context.CanHearPlayer = context.Hearing.CanHearSound(context.Player.position);

            // During active chase, also do a simple distance check as "awareness"
            // (AI knows roughly where player went even if briefly out of sight)
            bool isCloseEnough = distanceToPlayer <= context.Vision.viewRadius * 1.5f;
            bool canPerceivePlayer = context.CanSeePlayer || context.CanHearPlayer || (isCloseEnough && lostPlayerTimer < 0.5f);

            // Calculate player velocity for prediction
            Vector3 currentPlayerVelocity = Vector3.zero;
            Rigidbody playerRb = context.Player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                currentPlayerVelocity = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z);
            }

            if (canPerceivePlayer)
            {
                lostPlayerTimer = 0f;
                context.LastKnownPlayerPos = context.Player.position;
                lastPlayerVelocity = currentPlayerVelocity;

                // Predictive pursuit: aim ahead of where player is moving
                float predictionTime = Mathf.Clamp(distanceToPlayer / Mathf.Max(context.Agent.speed, 1f), 0f, 1f);
                predictedPlayerPos = context.Player.position + currentPlayerVelocity * predictionTime;

                // Throttle path updates to reduce NavMesh overhead
                if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
                {
                    // Use predicted position for smarter interception
                    context.Agent.SetDestination(predictedPlayerPos);
                    lastPathUpdateTime = Time.time;
                }
            }
            else
            {
                // Grace period before switching to search (prevents flickering)
                lostPlayerTimer += Time.deltaTime;

                if (lostPlayerTimer >= lostPlayerGracePeriod)
                {
                    context.SearchTimer = context.searchDuration;
                    AIController.Instance.StateMachine.SetState(new SearchState(), context);
                }
                else
                {
                    // Continue toward predicted position during grace period
                    // Use last known velocity to extrapolate where player might have gone
                    Vector3 extrapolatedPos = context.LastKnownPlayerPos + lastPlayerVelocity * lostPlayerTimer;

                    if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
                    {
                        context.Agent.SetDestination(extrapolatedPos);
                        lastPathUpdateTime = Time.time;
                    }
                }
            }
        }

        public void Exit(AiContext context)
        {
            context.Agent.isStopped = true;
            context.Animator.SetBool("isPlayerDiscovered", false);

            // Disable chase mode - return to normal vision
            if (context.Vision != null)
            {
                context.Vision.SetChaseMode(false);
            }
        }

        private void CatchPlayer(AiContext context)
        {
            Debug.Log($"[AI FSM] {context.Self.name} caught the player!");

            // Stop the AI
            context.Agent.isStopped = true;

            // Trigger catch animation if available
            if (context.Animator != null)
            {
                context.Animator.SetTrigger("CatchPlayer");
            }

            // Notify player health system
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.GetCaught();
            }

            // Switch to idle state after catching
            AIController.Instance.StateMachine.SetState(new IdleState(), context);
        }
    }
}
