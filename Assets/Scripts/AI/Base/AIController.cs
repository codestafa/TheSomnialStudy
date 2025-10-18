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
    public float chaseSpeed = 1.5f;
    public float patrolSpeed = 0.52f;
    public float searchSpeed = 0.52f;
    public float searchDuration = 8f;
    public float turnSmoothness = 6f;
    public float searchRadius = 6f;
    public int maxSearchPoints = 4;

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
        var playerObj = GameObject.FindGameObjectWithTag("Player");

        context = new AiContext
        {
            Self = transform,
            Agent = agent,
            Animator = animator,
            Vision = vision,
            Hearing = hearing,
            Player = playerObj != null ? playerObj.transform : null,
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
        if (context == null || context.Player == null || StateMachine == null)
            return;

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
