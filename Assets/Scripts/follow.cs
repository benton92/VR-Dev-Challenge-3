using UnityEngine;
using UnityEngine.AI;

/// Simple follower that uses NavMesh pathfinding to walk towards the player.
/// Requires:
/// - NavMeshAgent component on this GameObject
/// - NavMesh baked for the level
/// - GameObject with "Player" tag in the scene
public class Follow : MonoBehaviour
{
    // Cached reference to our NavMeshAgent
    private NavMeshAgent agent;
    
    // Cached reference to player transform (found by tag)
    private Transform player;

    // Track last observed path status to avoid spamming logs
    private NavMeshPathStatus lastPathStatus = NavMeshPathStatus.PathInvalid;
    private bool lastHasPath = false;
    
    [Header("Pathfinding")]
    [Tooltip("Seconds between path recalculations. Reduce to update more frequently, increase to lower CPU and avoid thrash.")]
    public float pathUpdateInterval = 0.25f;

    private float nextPathTime = 0f;
    private NavMeshPath cachedPath;

    private void Start()
    {
        // Get the NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Follow script requires a NavMeshAgent component", this);
            enabled = false;
            return;
        }

        // Find the player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Cannot find GameObject with 'Player' tag", this);
            enabled = false;
            return;
        }
        
        // Ensure agent will update position/rotation
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;

    // Try to reduce group blocking by giving each agent a random avoidance priority
    // and ensuring auto-repath is enabled so agents recover from blocked paths.
    agent.avoidancePriority = Random.Range(0, 100);
    agent.autoRepath = true;
    }

    private void Update()
    {
        // Update destination every frame to follow moving target
        if (agent != null && player != null)
        {
                // Only recalculate path at the configured interval to avoid thrashing
                if (Time.time >= nextPathTime)
                {
                    nextPathTime = Time.time + Mathf.Max(0.01f, pathUpdateInterval);

                    // If agent is off the NavMesh, try a single recovery warp; don't repeatedly warp every frame.
                    if (!agent.isOnNavMesh)
                    {
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            // fall through and attempt to calculate a path now that we're on the NavMesh
                        }
                        else
                        {
                            // can't sample a navmesh nearby; skip path calculation this interval
                            Debug.LogWarningFormat(this, "Follow: agent '{0}' not on NavMesh and no nearby sample found", name);
                            return;
                        }
                    }

                    // Calculate a path first (so we can inspect status) then assign it to the agent.
                    if (cachedPath == null)
                        cachedPath = new NavMeshPath();

                    bool calc = NavMesh.CalculatePath(agent.transform.position, player.position, NavMesh.AllAreas, cachedPath);
                    if (cachedPath.status == NavMeshPathStatus.PathInvalid)
                    {
                        // skip assigning an invalid path — log once when status changes
                        if (lastPathStatus != cachedPath.status)
                            Debug.LogWarningFormat(this, "Follow: calculated invalid path for '{0}' to player", name);
                    }
                    else
                    {
                        agent.SetPath(cachedPath);
                    }
                }

            // Minimal state-change logging to observe path issues when multiple agents exist
            if (agent.pathStatus != lastPathStatus || agent.hasPath != lastHasPath)
            {
                lastPathStatus = agent.pathStatus;
                lastHasPath = agent.hasPath;
                Debug.LogFormat(this, "Follow[{0}]: hasPath={1} pathStatus={2} remaining={3:F2}", name, agent.hasPath, agent.pathStatus, agent.remainingDistance);
            }
        }
    }
}
