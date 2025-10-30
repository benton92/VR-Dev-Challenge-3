using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class playerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum number of hits the player can take")]
    public int maxHealth = 3;
    [Tooltip("Time in seconds before health starts regenerating")]
    public float regenerationDelay = 5f;
    [Tooltip("Range within which enemy hits can damage the player")]
    public float hitDetectionRange = 2f; // You can adjust this value

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;
    public UnityEvent onHealthRegained;

    private int currentHealth;
    private float lastHitTime;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        lastHitTime = -regenerationDelay; // Allow immediate regeneration if needed at start
    }

    void Update()
    {
        if (isDead) return;

        // Check for health regeneration
        if (currentHealth < maxHealth && Time.time >= lastHitTime + regenerationDelay)
        {
            RegenerateHealth();
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;

        currentHealth--;
        lastHitTime = Time.time;
        
        onDamaged?.Invoke();
        
        if (currentHealth <= 0)
        {
            Die();
        }

            // Red text for damage
            Debug.LogFormat(this, "<color=red>DAMAGE TAKEN! Player Health: {0}/{1}</color>", currentHealth, maxHealth);
    }

    private void RegenerateHealth()
    {
        currentHealth = maxHealth;
        onHealthRegained?.Invoke();
        // Green text for healing
        Debug.LogFormat(this, "<color=green>HEALTH RESTORED! Player Health: {0}/{1}</color>", currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
        // Yellow text for death
        Debug.LogFormat(this, "<color=yellow>PLAYER DIED!</color>");
        // You can add death behavior here (like respawning, game over screen, etc.)
    }

    // Call this from enemyAnimation when their hit animation finishes
    public bool IsInHitRange(Vector3 enemyPosition)
    {
        float distance = Vector3.Distance(transform.position, enemyPosition);
        return distance <= hitDetectionRange;
    }
}
