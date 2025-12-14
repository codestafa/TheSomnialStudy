using UnityEngine;
using UnityEngine.AI;
using AI.Base;
using AI.States;
using AI.Common.Perception;
using AI.Common.Navigation;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    public AiStateMachine StateMachine { get; private set; }
    private AiContext context;

    [Header("Behavior Settings")]
    [Tooltip("AI chase speed - should be between player walk and sprint speed")]
    public float chaseSpeed = 5.5f;
    public float patrolSpeed = 1.2f;
    public float searchSpeed = 2.0f;
    public float searchDuration = 10f;
    public float turnSmoothness = 8f;
    public float searchRadius = 10f;
    public int maxSearchPoints = 5;

    [Header("Optional Patrol Route")]
    public PatrolRoute patrolRoute;

    [Header("Spawn Context")]
    public SpawnType CurrentSpawn { get; private set; }

    private void Awake()
    {
        Instance = this;

        var agent = GetComponent<NavMeshAgent>();
        var animator = GetComponent<Animator>();
        var vision = GetComponent<VisionSensor>();
        var hearing = GetComponent<HearingSensor>();

        // Defer player lookup to Update() to avoid freeze during scene preload
        // GameObject.FindGameObjectWithTag("Player") is slow with 17K+ objects

        context = new AiContext
        {
            Self = transform,
            Agent = agent,
            Animator = animator,
            Vision = vision,
            Hearing = hearing,
            Player = null, // Will be found in first Update()
            InitialPosition = transform.position,
            SearchRadius = searchRadius,
            MaxSearchPoints = maxSearchPoints,
            chaseSpeed = chaseSpeed,
            patrolSpeed = patrolSpeed,
            searchSpeed = searchSpeed,
            searchDuration = searchDuration,
            turnSmoothness = turnSmoothness,
            PatrolRoute = patrolRoute
        };

        StateMachine = new AiStateMachine();
        StateMachine.SetState(new IdleState(), context);
    }

    private void Update()
    {
        if (context == null || StateMachine == null)
            return;

        // Lazy-load player reference on first Update
        if (context.Player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                context.Player = playerObj.transform;
            }
            else
            {
                return; // Skip this frame if player not found yet
            }
        }

        StateMachine.Update(context);

        // Smooth rotation toward movement
        if (context.Agent != null && context.Agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(context.Agent.velocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, Time.deltaTime * context.turnSmoothness);
        }

        // Sync animation with velocity
        if (context.Animator != null && context.Agent != null)
        {
            context.Animator.SetFloat("Speed", context.Agent.velocity.magnitude);
            float playbackSpeed = context.Agent.velocity.magnitude / Mathf.Max(context.Agent.speed, 0.01f);
            context.Animator.speed = Mathf.Clamp(playbackSpeed, 0.8f, 1.2f);
        }
    }

    public AiContext GetContext() => context;

    // ---------------- SPAWN CONTEXT MANAGEMENT ----------------
    public void SetSpawnContext(SpawnType spawn)
    {
        CurrentSpawn = spawn;
        Debug.Log($"{name} spawned in zone: {spawn}");

        // Assign behavior based on spawn zone
        switch (spawn)
        {
            case SpawnType.Pacing:
                // Pacing AIs move around their spawn point
                StateMachine.SetState(new PatrolState(), context);
                break;

            case SpawnType.Lab:
            case SpawnType.Office:
                // Stationary AIs stay idle unless triggered
                StateMachine.SetState(new IdleState(), context);
                break;

            default:
                // Fallback
                StateMachine.SetState(new IdleState(), context);
                break;
        }
    }
}
