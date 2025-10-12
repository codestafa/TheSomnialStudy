using UnityEngine;
using UnityEngine.AI;
using AI.Common.Perception;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AIController : MonoBehaviour
{
    private VisionSensor visionSensor;
    private HearingSensor hearingSensor;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    public JobType CurrentJob { get; private set; }

    private enum AIState { Idle, Chase, Search, Return }
    private AIState state = AIState.Idle;

    private Vector3 lastKnownPlayerPos;
    private Vector3 initialPosition;

    private float searchTimer;
    private float lookAngle = 60f;
    private float lookSpeed = 120f;
    private float rotateDir = 1f;
    private Quaternion targetRot;

    [Header("Behavior Settings")]
    public float chaseSpeed = 1.5f;
    public float patrolSpeed = 0.52f;
    public float searchSpeed = 0.52f;
    public float searchDuration = 8f; // slightly longer for exploration
    public float turnSmoothness = 6f;

    // Navigation control
    private float repathInterval = 0.5f;
    private float repathTimer = 0f;
    private float stuckTimer = 0f;

    // Debug timer
    private float debugInterval = 5f;
    private float debugTimer = 0f;

    // Hearing memory
    private bool hasRecentSound = false;
    private float lastHeardTime = 0f;
    [SerializeField] private float hearingMemoryDuration = 2.5f;

    // Smart search
    private Vector3[] searchPoints;
    private int currentSearchIndex = 0;
    private int maxSearchPoints = 4;
    private float searchRadius = 6f;

    private void Awake()
    {
        visionSensor = GetComponent<VisionSensor>();
        hearingSensor = GetComponent<HearingSensor>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        initialPosition = transform.position;
        state = AIState.Idle;
    }

    private void Update()
    {
        if (player == null) return;

        // --- Sensor Fusion ---
        bool canSee = visionSensor && visionSensor.CanSeeTarget(player);
        bool canHear = hearingSensor && hearingSensor.CanHearSound(player.position);

        // Memory / fusion logic
        if (canSee)
        {
            lastHeardTime = Time.time;
            hasRecentSound = false;
        }
        else if (canHear)
        {
            lastHeardTime = Time.time;
            hasRecentSound = true;
        }
        else if (hasRecentSound && Time.time - lastHeardTime < hearingMemoryDuration)
        {
            canHear = true; // short-term memory
        }
        else
        {
            hasRecentSound = false;
        }

        // --- Debug (every 5s) ---
        debugTimer -= Time.deltaTime;
        if (debugTimer <= 0f)
        {
            debugTimer = debugInterval;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            Debug.Log($"[AI DEBUG] Enemy: {transform.position:F2} | Player: {player.position:F2} | Distance: {distanceToPlayer:F2} | State: {state}");
        }

        // --- State Machine ---
        switch (state)
        {
            case AIState.Idle: HandleIdle(canSee, canHear); break;
            case AIState.Chase: HandleChase(canSee, canHear); break;
            case AIState.Search: HandleSearch(canSee, canHear); break;
            case AIState.Return: HandleReturn(canSee, canHear); break;
        }

        // Smooth rotation
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, Time.deltaTime * turnSmoothness);
        }

        // Animation sync
        animator.SetFloat("Speed", agent.velocity.magnitude);
        float playbackSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
        animator.speed = Mathf.Clamp(playbackSpeed, 0.8f, 1.2f);

        CheckStuck();
    }

    // ---------------- STATE LOGIC ----------------
    private void HandleIdle(bool canSee, bool canHear)
    {
        animator.SetBool("isPlayerDiscovered", false);
        agent.speed = patrolSpeed;
        agent.isStopped = true;

        if (canSee || canHear)
        {
            lastKnownPlayerPos = player.position;
            state = AIState.Chase;
            animator.SetBool("isPlayerDiscovered", true);
            agent.speed = chaseSpeed;
            Debug.Log($"{name} spotted or heard the player — chasing!");
        }
    }

    private void HandleChase(bool canSee, bool canHear)
    {
        animator.SetBool("isPlayerDiscovered", true);
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        if (canSee)
        {
            lastKnownPlayerPos = player.position;
        }
        else if (!canSee && !canHear)
        {
            Debug.Log($"{name} lost sight — entering search mode.");
            state = AIState.Search;
            searchTimer = searchDuration;
            agent.isStopped = false;
            agent.speed = searchSpeed;
            searchPoints = GenerateSearchPoints(lastKnownPlayerPos, maxSearchPoints, searchRadius);
            currentSearchIndex = 0;
            if (searchPoints.Length > 0)
                agent.SetDestination(searchPoints[currentSearchIndex]);
            animator.SetBool("isPlayerDiscovered", false);
        }
    }

    private void HandleSearch(bool canSee, bool canHear)
    {
        if (canSee || canHear)
        {
            state = AIState.Chase;
            animator.SetBool("isPlayerDiscovered", true);
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            searchPoints = null;
            return;
        }

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            Debug.Log($"{name} finished searching — returning to post.");
            state = AIState.Return;
            agent.isStopped = false;
            agent.speed = searchSpeed;
            searchPoints = null;
            return;
        }

        if (searchPoints == null || searchPoints.Length == 0)
        {
            searchPoints = GenerateSearchPoints(transform.position, maxSearchPoints, searchRadius);
            currentSearchIndex = 0;
            if (searchPoints.Length > 0)
                agent.SetDestination(searchPoints[currentSearchIndex]);
        }

        // Move to next point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentSearchIndex++;
            if (currentSearchIndex < searchPoints.Length)
            {
                agent.SetDestination(searchPoints[currentSearchIndex]);
            }
            else
            {
                // regenerate new points mid-search
                searchPoints = GenerateSearchPoints(transform.position, maxSearchPoints, searchRadius);
                currentSearchIndex = 0;
                if (searchPoints.Length > 0)
                    agent.SetDestination(searchPoints[currentSearchIndex]);
            }
        }
    }

    private void HandleReturn(bool canSee, bool canHear)
    {
        animator.SetBool("isPlayerDiscovered", false);

        if (canSee || canHear)
        {
            state = AIState.Chase;
            animator.SetBool("isPlayerDiscovered", true);
            agent.speed = chaseSpeed;
            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.SetDestination(initialPosition);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            state = AIState.Idle;
            agent.isStopped = true;
        }
    }

    // ---------------- HELPERS ----------------
    private void CheckStuck()
    {
        if (!agent.hasPath) return;

        if (agent.remainingDistance > 1.0f)
        {
            if (agent.velocity.magnitude < 0.05f)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = 0f;

            if (stuckTimer > 2f)
            {
                Debug.Log($"{name} seems stuck — resetting path.");
                agent.ResetPath();
                stuckTimer = 0f;
                state = AIState.Search;
                searchTimer = searchDuration * 0.5f;
                agent.isStopped = false;
                searchPoints = GenerateSearchPoints(transform.position, maxSearchPoints, searchRadius);
                currentSearchIndex = 0;
                if (searchPoints.Length > 0)
                    agent.SetDestination(searchPoints[currentSearchIndex]);
            }
        }
    }

    private Vector3[] GenerateSearchPoints(Vector3 center, int count, float radius)
    {
        var points = new Vector3[count];
        int validPoints = 0;

        for (int i = 0; i < count * 3 && validPoints < count; i++)
        {
            Vector3 randomPos = center + Random.insideUnitSphere * radius;
            randomPos.y = center.y;
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                points[validPoints] = hit.position;
                validPoints++;
            }
        }

        if (validPoints == 0)
        {
            Debug.LogWarning($"{name}: No valid search points found near {center}");
            return new Vector3[0];
        }

        Debug.Log($"{name} generated {validPoints} search points around {center}");
        return points;
    }

    public void SetJob(JobType job)
    {
        CurrentJob = job;
        Debug.Log("AI job assigned: " + job);
    }
}
