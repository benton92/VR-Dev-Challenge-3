using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class playerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum number of hits the player can take")]
    public int maxHealth = 3;
    [Tooltip("Time in seconds before health starts regenerating to full")]
    public float regenerationDelay = 5f;
    [Tooltip("Range within which enemy hits can damage the player")]
    public float hitDetectionRange = 2f; // You can adjust this value
    [Tooltip("Invincibility time in seconds after taking damage")]
    public float invincibilityDuration = 0.5f;

    [Header("Overlay Settings (Full-screen Red)")]
    [Tooltip("CanvasGroup on the full-screen overlay Image (head-locked)")]
    public CanvasGroup vignetteCanvasGroup;
    [Tooltip("Optional: Image to tint directly if no CanvasGroup is assigned")]
    public Image overlayImage;
    [Tooltip("Try to auto-find an overlay at runtime if none is assigned (CanvasGroup or Image)")]
    public bool autoFindOverlay = true;
    [Tooltip("Max alpha when at 1 HP (low health)")]
    [Range(0f, 1f)] public float lowHealthMaxAlpha = 0.7f;
    // [Tooltip("Alpha to apply on death")]
    // [Range(0f, 1f)] public float deathAlpha = 0.9f;
    [Tooltip("How fast the overlay fades toward its target alpha (per second)")]
    public float overlayFadeSpeed = 5f;

    [Header("Overlay Fade Tuning")]
    [Tooltip("When health regenerates to full, fade the red overlay out over a fixed duration for extra smoothness.")]
    public bool useRegenFade = true;
    [Tooltip("Duration in seconds for the overlay to fade out after regeneration starts.")]
    public float regenFadeDuration = 1.5f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealthRegained;

    private int currentHealth;
    private float lastHitTime;
    // private bool isDead = false;
    private float lastDamageTime = -999f; // Track when we last took damage for invincibility frames
    private Coroutine overlayFadeRoutine;
    private bool overlayFadeActive = false;

    void Start()
    {
        currentHealth = maxHealth;
        lastHitTime = -regenerationDelay; // Allow immediate regeneration if needed at start

        Debug.LogFormat(this, "<color=blue>playerHealth INITIALIZED on {0} - Max Health: {1}, Regen Delay: {2}s</color>",
            gameObject.name, maxHealth, regenerationDelay);

        // Initialize overlay(s)
        if (vignetteCanvasGroup == null && overlayImage == null && autoFindOverlay)
        {
            TryAutoFindOverlay();
        }

        if (vignetteCanvasGroup != null)
        {
            vignetteCanvasGroup.alpha = 0f; // start hidden
            Debug.Log("playerHealth: Using CanvasGroup overlay: " + vignetteCanvasGroup.name);
        }
        if (overlayImage != null)
        {
            // Ensure the image starts fully transparent
            var c = overlayImage.color; c.a = 0f; overlayImage.color = c;
            Debug.Log("playerHealth: Using Image overlay: " + overlayImage.name);
        }
        if (vignetteCanvasGroup == null && overlayImage == null)
        {
            Debug.LogWarning("playerHealth: No overlay assigned or found. Assign a CanvasGroup or an Image to show the red tint.");
        }
    }

    void Update()
    {
        // ...existing code...

        // Check for health regeneration
        if (currentHealth < maxHealth && Time.time >= lastHitTime + regenerationDelay)
        {
            RegenerateHealth();
        }

        // Smoothly drive overlay alpha toward the target for current health,
        // unless a dedicated fade routine is currently running (e.g., regen fade-out).
        if (!overlayFadeActive)
        {
            float target = ComputeTargetOverlayAlpha();
            if (vignetteCanvasGroup != null)
            {
                vignetteCanvasGroup.alpha = Mathf.MoveTowards(vignetteCanvasGroup.alpha, target, overlayFadeSpeed * Time.deltaTime);
            }
            else if (overlayImage != null)
            {
                var c = overlayImage.color;
                float newA = Mathf.MoveTowards(c.a, target, overlayFadeSpeed * Time.deltaTime);
                c.a = newA;
                overlayImage.color = c;
            }
        }
    }

    public void TakeDamage()
    {
        Debug.LogFormat(this, "<color=magenta>TakeDamage() called on {0}. isDead={1}, currentHealth={2}</color>",
            gameObject.name, isDead, currentHealth);

        if (isDead) return;

        // Check invincibility frames
        if (Time.time < lastDamageTime + invincibilityDuration)
        {
            Debug.LogFormat(this, "<color=yellow>INVINCIBLE! Damage blocked. Time remaining: {0:F2}s</color>",
                (lastDamageTime + invincibilityDuration) - Time.time);
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - 1);
        lastHitTime = Time.time;
        lastDamageTime = Time.time;

        onDamaged?.Invoke();

        // ...existing code...

        // Red text for damage
        Debug.LogFormat(this, "<color=red>DAMAGE TAKEN! Player Health: {0}/{1}</color>", currentHealth, maxHealth);

        // If a regen fade coroutine was active, cancel it because we're taking damage again
        CancelOverlayFade();

        // Nudge overlay immediately to at least the new target so it feels snappy
        if (vignetteCanvasGroup != null)
        {
            float target = ComputeTargetOverlayAlpha();
            vignetteCanvasGroup.alpha = Mathf.Max(vignetteCanvasGroup.alpha, target);
        }
        else if (overlayImage != null)
        {
            float target = ComputeTargetOverlayAlpha();
            var c = overlayImage.color;
            c.a = Mathf.Max(c.a, target);
            overlayImage.color = c;
        }
    }

    private void RegenerateHealth()
    {
        currentHealth = maxHealth;
        onHealthRegained?.Invoke();
        Debug.LogFormat(this, "<color=green>HEALTH RESTORED! Player Health: {0}/{1}</color>", currentHealth, maxHealth);
        // Smoothly fade the overlay out over a fixed duration if enabled
        if (useRegenFade)
        {
            StartOverlayFadeTo(0f, regenFadeDuration);
        }
        // Otherwise, Update() will fade overlay toward 0 using overlayFadeSpeed
    }

    // ...existing code...

    // Call this from enemyAnimation when their hit animation finishes
    public bool IsInHitRange(Vector3 enemyPosition)
    {
        float distance = Vector3.Distance(transform.position, enemyPosition);
        return distance <= hitDetectionRange;
    }

    // Computes desired overlay alpha based on current health (higher alpha at lower health)
    private float ComputeTargetOverlayAlpha()
    {
    // No death alpha; just fade based on health
    if (currentHealth >= maxHealth) return 0f;

        // Map missing health to alpha; with maxHealth=3:
        // 3 HP -> 0, 2 HP -> ~0.35 (if lowHealthMaxAlpha=0.7), 1 HP -> ~0.7
        int missing = Mathf.Clamp(maxHealth - currentHealth, 0, Mathf.Max(1, maxHealth - 1));
        float t = (maxHealth > 1) ? (missing / (float)(maxHealth - 1)) : 1f;
        return Mathf.Clamp01(Mathf.Lerp(0f, lowHealthMaxAlpha, t));
    }

    // Overlay fading helpers
    private void StartOverlayFadeTo(float targetAlpha, float duration)
    {
        CancelOverlayFade();
        overlayFadeRoutine = StartCoroutine(FadeOverlayRoutine(targetAlpha, duration));
    }

    private void CancelOverlayFade()
    {
        if (overlayFadeRoutine != null)
        {
            StopCoroutine(overlayFadeRoutine);
            overlayFadeRoutine = null;
        }
        overlayFadeActive = false;
    }

    private IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
    {
        overlayFadeActive = true;

        float startA = 0f;
        if (vignetteCanvasGroup != null)
            startA = vignetteCanvasGroup.alpha;
        else if (overlayImage != null)
            startA = overlayImage.color.a;

        if (Mathf.Approximately(duration, 0f))
        {
            SetOverlayAlpha(targetAlpha);
            overlayFadeActive = false;
            overlayFadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float a = Mathf.Lerp(startA, targetAlpha, t);
            SetOverlayAlpha(a);
            yield return null;
        }
        SetOverlayAlpha(targetAlpha);

        overlayFadeActive = false;
        overlayFadeRoutine = null;
    }

    private void SetOverlayAlpha(float a)
    {
        a = Mathf.Clamp01(a);
        if (vignetteCanvasGroup != null)
        {
            vignetteCanvasGroup.alpha = a;
        }
        else if (overlayImage != null)
        {
            var c = overlayImage.color; c.a = a; overlayImage.color = c;
        }
    }
    private void TryAutoFindOverlay()
    {
        // Heuristics: look for a CanvasGroup or Image that looks like a full-screen overlay
        // 1) Search children first (common when player has a head-locked canvas)
        if (vignetteCanvasGroup == null)
        {
            vignetteCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }
        if (vignetteCanvasGroup == null)
        {
            // 2) Search any active CanvasGroups in the scene with suggestive names
            var allCG = GameObject.FindObjectsOfType<CanvasGroup>(true);
            foreach (var cg in allCG)
            {
                string n = cg.name.ToLower();
                if (n.Contains("overlay") || n.Contains("vignette") || n.Contains("damage"))
                {
                    vignetteCanvasGroup = cg;
                    break;
                }
            }
        }

        if (vignetteCanvasGroup == null && overlayImage == null)
        {
            // Try to find an Image to use instead
            var allImgs = GameObject.FindObjectsOfType<Image>(true);
            foreach (var img in allImgs)
            {
                string n = img.name.ToLower();
                if (n.Contains("overlay") || n.Contains("vignette") || n.Contains("damage"))
                {
                    overlayImage = img;
                    break;
                }
            }
        }
    }
}
