using UnityEngine;
using UnityEngine.AI;
using AI.Common.Perception;
using AI.Common.Navigation;
namespace AI.Base
{
    [System.Serializable]
    public class AiContext
    {
        // Core references
        public Transform Self;
        public Transform Player;
        public NavMeshAgent Agent;
        public Animator Animator;
        public VisionSensor Vision;
        public HearingSensor Hearing;

        // Movement & position memory
        public Vector3 InitialPosition;
        public Vector3 LastKnownPlayerPos;

        // Perception status
        public bool CanSeePlayer;
        public bool CanHearPlayer;

        // Hearing memory
        public float LastHeardTime;
        public bool HasRecentSound;

        // Search control
        public Vector3[] SearchPoints;
        public int SearchIndex;
        public float SearchTimer;
        public float SearchRadius = 6f;
        public int MaxSearchPoints = 4;

        // Behavior settings (injected by controller)
        public float chaseSpeed;
        public float patrolSpeed;
        public float searchSpeed;
        public float searchDuration;
        public float turnSmoothness;

        // Optional patrol
        public PatrolRoute PatrolRoute;
    }
}
