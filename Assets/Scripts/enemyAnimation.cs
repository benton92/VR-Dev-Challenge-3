using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// Controls the skeleton's Animator to play walk vs idle.
/// Behavior:
/// - If a NavMeshAgent exists, use its velocity magnitude to determine movement.
/// - Otherwise fall back to transform positional delta for movement detection.
/// - If the Animator has a boolean parameter named "isMoving" it will be set.
/// - Otherwise the script will attempt to play the states named by `walkState` / `idleState`.
public class enemyAnimation : MonoBehaviour
{
    [Tooltip("Animator on the enemy (optional). If null, one will be fetched from this GameObject.)")]
    public Animator animator;

    [Tooltip("Optional NavMeshAgent on the enemy. If present, its velocity will be used to detect movement.")]
    public NavMeshAgent agent;

    [Header("Animation state names")]
    [Tooltip("Name of the idle state in the Animator (used if no 'isMoving' parameter exists).")]
    public string idleState = "Idle";

    [Tooltip("Name of the walk state in the Animator (used if no 'isMoving' parameter exists).")]
    public string walkState = "Walk";

    [Tooltip("Velocity magnitude threshold to consider the skeleton as moving.")]
    public float movementThreshold = 0.1f;
    [Tooltip("Extra distance buffer added to NavMeshAgent.stoppingDistance when deciding arrival.")]
    public float arrivalBuffer = 0.15f;

    [Tooltip("Damp time (seconds) used when setting the Animator 'Speed' float parameter to smooth values. Lower = snappier.")]
    public float speedDampTime = 0.05f;

    [Header("Combat Settings")]
    [Tooltip("Reference to the player's GameObject (will attempt to find by tag 'Player' if not set)")]
    public GameObject player;
    
    [Tooltip("Range within which the hit can damage the player")]
    public float hitRange = 2f;
    
    [Tooltip("Time in seconds before the hit connects in the animation (when to check for damage)")]
    public float hitTiming = 0.8f;

    // internals
    private Vector3 lastPosition;
    private int isMovingParamHash = -1;
    private int speedParamHash = -1;
    private int isHittingParamHash = -1;
    private bool canHit = false;
    private playerHealth playerHealthScript;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("enemyAnimation: No player found! Make sure the player has the 'Player' tag or assign it manually.");
            }
        }

        // Get player health script
        if (player != null)
        {
            playerHealthScript = player.GetComponent<playerHealth>();
            if (playerHealthScript == null)
            {
                Debug.LogWarning("enemyAnimation: Player doesn't have a playerHealth component!");
            }
        }

        // cache param hash if exists (support common parameter names)
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == "isMoving")
                {
                    isMovingParamHash = Animator.StringToHash("isMoving");
                }

                // common float parameter used for blend trees / transitions
                if (p.type == AnimatorControllerParameterType.Float && (p.name == "Speed" || p.name == "speed"))
                {
                    speedParamHash = Animator.StringToHash(p.name);
                }

                if (p.type == AnimatorControllerParameterType.Bool && p.name == "isHitting")
                {
                    isHittingParamHash = Animator.StringToHash("isHitting");
                }
            }
        }

        lastPosition = transform.position;

        // Debug: report which animator parameter we'll use so you can verify in Console
        if (animator != null)
        {
            if (speedParamHash != -1)
                Debug.LogFormat(this, "enemyAnimation: detected Speed parameter on '{0}' (hash {1})", name, speedParamHash);
            else if (isMovingParamHash != -1)
                Debug.LogFormat(this, "enemyAnimation: detected isMoving parameter on '{0}'", name);
            else
                Debug.LogFormat(this, "enemyAnimation: no Speed/isMoving parameter found on '{0}', falling back to state names", name);
        }
    }

    private void Update()
    {
        float vel = 0f;

        if (agent != null)
        {
            // Use agent velocity when available (handles NavMesh movement)
            vel = agent.velocity.magnitude;
            // If agent has a path and is still further than stoppingDistance (+ buffer), consider it moving
            if (agent.hasPath)
            {
                float stopDist = agent.stoppingDistance + arrivalBuffer;
                if (agent.remainingDistance > stopDist)
                {
                    vel = Mathf.Max(vel, 0.01f); // ensure we treat it as moving even if velocity is small during acceleration
                    // Reset hit state when moving
                    if (isHittingParamHash != -1)
                    {
                        animator.SetBool(isHittingParamHash, false);
                    }
                }
            }
        }
        else
        {
            // fallback: approximate velocity from transform delta
            Vector3 delta = transform.position - lastPosition;
            vel = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            lastPosition = transform.position;
        }

        bool moving = vel > movementThreshold;

        // Check for hit state when stopped near target
        if (agent != null && agent.hasPath)
        {
            float stopDist = agent.stoppingDistance + arrivalBuffer;
            if (agent.remainingDistance <= stopDist && !moving)
            {
                // We've stopped near the target - check if we can hit
                if (isHittingParamHash != -1 && animator.GetCurrentAnimatorStateInfo(0).IsName(idleState))
                {
                    animator.SetBool(isHittingParamHash, true);
                    // Schedule turning off the hit animation
                    StartCoroutine(ResetHitAnimation());
                }
            }
        }

        // If the Animator uses the isMoving boolean, prefer a path-based decision when a NavMeshAgent exists.
        // This avoids waiting for a non-zero velocity (which can lag during acceleration/braking).
        if (isMovingParamHash != -1 && agent != null)
        {
            float stopDist = agent.stoppingDistance + arrivalBuffer;
            bool pathSaysMoving = agent.hasPath && !agent.pathPending && agent.remainingDistance > stopDist;
            moving = pathSaysMoving || (vel > movementThreshold);
        }

        if (animator == null)
            return; // nothing to drive

        // Prefer setting a Speed float parameter (blend trees) if available
        if (speedParamHash != -1)
        {
            // Use damped SetFloat so the parameter ramps smoothly; low damp time = snappy
            animator.SetFloat(speedParamHash, vel, speedDampTime, Time.deltaTime);
            // also set bool if present for legacy transitions
            if (isMovingParamHash != -1)
                animator.SetBool(isMovingParamHash, moving);
            // Log state change when moving flag changes
            HandleMovingDebug(moving);
            return;
        }

        if (isMovingParamHash != -1)
        {
            // animator has 'isMoving' bool parameter: set it
            animator.SetBool(isMovingParamHash, moving);
            HandleMovingDebug(moving);
            return;
        }

        // Fallback: directly play state names. Use Play() which is simpler and more robust
        if (moving)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(walkState))
                animator.Play(walkState);
            HandleMovingDebug(true);
        }
        else
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(idleState))
                animator.Play(idleState);
            HandleMovingDebug(false);
        }
    }

    // debugging helpers
    private bool lastMoving = false;
    private void HandleMovingDebug(bool moving)
    {
        if (moving != lastMoving)
        {
            lastMoving = moving;
            Debug.LogFormat(this, "enemyAnimation[{0}]: moving={1} velocityThreshold={2:F3} currentThreshold={3:F3}", name, moving, movementThreshold, (moving ? 1f : 0f));
        }
    }

    private IEnumerator ResetHitAnimation()
    {
        Debug.LogFormat(this, "<color=orange>Skeleton [{0}] started hit animation</color>", name);
        
        // Wait for a reasonable time for the hit animation to play (adjust based on your animation length)
        yield return new WaitForSeconds(hitTiming); // Timing when the actual hit connects in the animation
        
        // Check if player is in range and deal damage
        if (player != null && playerHealthScript != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.LogFormat(this, "<color=cyan>Skeleton [{0}] hit check - Distance to player: {1:F2} (hitRange: {2:F2})</color>", 
                name, distance, hitRange);
            
            if (distance <= hitRange)
            {
                Debug.LogFormat(this, "<color=orange>Skeleton [{0}] HIT CONNECTED! Damaging player.</color>", name);
                playerHealthScript.TakeDamage();
            }
            else
            {
                Debug.LogFormat(this, "<color=gray>Skeleton [{0}] missed - player out of range</color>", name);
            }
        }
        else
        {
            Debug.LogWarning("Player or playerHealth script not found!");
        }
        
        // Wait a bit more for animation to finish
        yield return new WaitForSeconds(0.2f);
        
        if (isHittingParamHash != -1)
        {
            animator.SetBool(isHittingParamHash, false);
            Debug.LogFormat(this, "<color=orange>Skeleton [{0}] finished hit animation</color>", name);
        }
    }
}
