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
    
    [Tooltip("Minimum time in seconds between attacks")]
    public float attackCooldown = 2.5f;

    [Header("Death Settings")]
    [Tooltip("Play this Animator state on death (ignored if 'Use Death Trigger' is enabled and the trigger exists)")]
    public string deathState = "Death";
    [Tooltip("If enabled and the Animator has this trigger parameter, it will be set on death")] 
    public bool useDeathTrigger = false;
    [Tooltip("Animator Trigger parameter name to fire on death")] 
    public string deathTrigger = "Die";
    [Tooltip("How long to wait before despawning after triggering death (seconds)")]
    public float deathAnimationDuration = 2.0f;
    [Tooltip("Destroy the GameObject after death animation completes (otherwise SetActive(false))")] 
    public bool destroyOnDeath = true;
    [Tooltip("When true, fade the enemy's renderers out instead of popping instantly on despawn")] 
    public bool fadeOutOnDeath = true;
    [Tooltip("Seconds to fade visuals to invisible before despawn")] 
    public float fadeOutDuration = 2.0f;

    // internals
    private Vector3 lastPosition;
    private int isMovingParamHash = -1;
    private int speedParamHash = -1;
    private int isHittingParamHash = -1;
    private bool canHit = false;
    private playerHealth playerHealthScript;
    private bool isCurrentlyAttacking = false;
    private float lastAttackTime = -999f; // Start with a large negative value so first attack can happen immediately
    private bool isDead = false;
    private int deathTriggerHash = -1;
    private float spawnTime = 0f;
    private const float SPAWN_GRACE_PERIOD = 1.0f; // Ignore spell hits for this many seconds after spawn

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

                if (useDeathTrigger && p.type == AnimatorControllerParameterType.Trigger && p.name == deathTrigger)
                {
                    deathTriggerHash = Animator.StringToHash(deathTrigger);
                }
            }
        }

        lastPosition = transform.position;
        spawnTime = Time.time; // Track when this enemy spawned

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
        if (isDead)
            return; // stop driving animations once dead

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
            if (agent.remainingDistance <= stopDist && !moving && !isCurrentlyAttacking)
            {
                // Check if enough time has passed since last attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    // We've stopped near the target - check if we can hit
                    if (isHittingParamHash != -1 && animator.GetCurrentAnimatorStateInfo(0).IsName(idleState))
                    {
                        animator.SetBool(isHittingParamHash, true);
                        lastAttackTime = Time.time;
                        // Schedule turning off the hit animation
                        StartCoroutine(ResetHitAnimation());
                    }
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
        if (isDead) yield break;
        isCurrentlyAttacking = true;
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
        
        isCurrentlyAttacking = false;
    }

    // ---- Death handling on spell contact ----
    private static readonly string[] KillColliderNames = { "LightningSpellCollider", "FireSpellCollider" };

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        
        // Grace period to prevent instant death at spawn
        if (Time.time < spawnTime + SPAWN_GRACE_PERIOD)
        {
            Debug.LogFormat(this, "<color=yellow>enemyAnimation[{0}]: Ignoring trigger '{1}' during spawn grace period ({2:F2}s remaining)</color>", 
                name, other.name, (spawnTime + SPAWN_GRACE_PERIOD) - Time.time);
            return;
        }
        
        if (IsSpellCollider(other.transform))
        {
            Debug.LogFormat(this, "<color=purple>enemyAnimation[{0}]: Hit by spell trigger '{1}'. Dying...</color>", name, other.name);
            StartCoroutine(HandleDeath());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        
        // Grace period to prevent instant death at spawn
        if (Time.time < spawnTime + SPAWN_GRACE_PERIOD)
        {
            Debug.LogFormat(this, "<color=yellow>enemyAnimation[{0}]: Ignoring collision '{1}' during spawn grace period ({2:F2}s remaining)</color>", 
                name, collision.gameObject.name, (spawnTime + SPAWN_GRACE_PERIOD) - Time.time);
            return;
        }
        
        if (IsSpellCollider(collision.transform))
        {
            Debug.LogFormat(this, "<color=purple>enemyAnimation[{0}]: Hit by spell collision '{1}'. Dying...</color>", name, collision.gameObject.name);
            StartCoroutine(HandleDeath());
        }
    }

    private bool IsSpellCollider(Transform t)
    {
        // Walk up the hierarchy to catch child colliders
        Transform cur = t;
        int depth = 0;
        while (cur != null && depth < 5)
        {
            foreach (var n in KillColliderNames)
            {
                if (cur.name == n)
                    return true;
            }
            cur = cur.parent;
            depth++;
        }
        return false;
    }

    private IEnumerator HandleDeath()
    {
        if (isDead) yield break;
        isDead = true;

        // stop navigation and combat
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        isCurrentlyAttacking = false;

        // reset combat animator params
        if (animator != null)
        {
            if (isHittingParamHash != -1) animator.SetBool(isHittingParamHash, false);
            if (speedParamHash != -1) animator.SetFloat(speedParamHash, 0f);

            // trigger or play death
            bool fired = false;
            if (useDeathTrigger && deathTriggerHash != -1)
            {
                animator.ResetTrigger(deathTriggerHash);
                animator.SetTrigger(deathTriggerHash);
                fired = true;
                Debug.LogFormat(this, "enemyAnimation[{0}]: Set death trigger '{1}'", name, deathTrigger);
            }
            if (!fired && !string.IsNullOrEmpty(deathState))
            {
                animator.Play(deathState, 0, 0f);
                Debug.LogFormat(this, "enemyAnimation[{0}]: Playing death state '{1}'", name, deathState);
            }
        }

        // disable colliders so we don't get re-hit while dying
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            c.enabled = false;
        }

        // wait for animation duration
        yield return new WaitForSeconds(Mathf.Max(0f, deathAnimationDuration));

        if (fadeOutOnDeath && fadeOutDuration > 0f)
        {
            yield return StartCoroutine(FadeOutAndDespawn());
        }
        else
        {
            // instant despawn
            if (destroyOnDeath)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    // --- Visual fade-out helpers ---
    private IEnumerator FadeOutAndDespawn()
    {
        // Collect all renderers and create material instances
        var renderers = GetComponentsInChildren<Renderer>(true);
        var mats = new List<Material>();
        foreach (var r in renderers)
        {
            // renderer.materials returns instances (safe to edit per-object)
            var rms = r.materials;
            for (int i = 0; i < rms.Length; i++)
            {
                var m = rms[i];
                if (m != null && !mats.Contains(m))
                {
                    PrepareMaterialForFade(m);
                    mats.Add(m);
                }
            }
        }

        // Capture starting alpha per material
        var startA = new float[mats.Count];
        for (int i = 0; i < mats.Count; i++)
        {
            startA[i] = GetMaterialAlpha(mats[i]);
        }

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            for (int i = 0; i < mats.Count; i++)
            {
                float a = Mathf.Lerp(startA[i], 0f, k);
                SetMaterialAlpha(mats[i], a);
            }
            yield return null;
        }

        // Ensure fully invisible
        for (int i = 0; i < mats.Count; i++)
        {
            SetMaterialAlpha(mats[i], 0f);
        }

        // Final despawn
        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void PrepareMaterialForFade(Material m)
    {
        // URP Lit: _Surface = 0(Opaque), 1(Transparent); color is _BaseColor
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f); // Transparent
            // For URP we might also need to set render queue
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return;
        }

        // Built-in Standard shader fallbacks
        if (m.HasProperty("_Mode"))
        {
            // 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent
            m.SetFloat("_Mode", 2f); // Fade
        }
        // Common blending setup for fade
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private float GetMaterialAlpha(Material m)
    {
        if (m.HasProperty("_BaseColor"))
            return m.GetColor("_BaseColor").a;
        if (m.HasProperty("_Color"))
            return m.GetColor("_Color").a;
        return 1f;
    }

    private void SetMaterialAlpha(Material m, float a)
    {
        a = Mathf.Clamp01(a);
        if (m.HasProperty("_BaseColor"))
        {
            var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c);
            return;
        }
        if (m.HasProperty("_Color"))
        {
            var c = m.GetColor("_Color"); c.a = a; m.SetColor("_Color", c);
        }
    }
}
