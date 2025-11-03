using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dmgTest : MonoBehaviour
{
    [Tooltip("Reference to the player's playerHealth component. If not set, will try to find by tag 'Player'.")]
    public playerHealth player;

    [Header("Test Timing")]
    [Tooltip("Seconds to wait after scene starts before applying the first damage.")]
    public float initialDelay = 5f;
    [Tooltip("Seconds between subsequent damage ticks.")]
    public float tickInterval = 2f;
    [Tooltip("Automatically start applying damage on play.")]
    public bool startOnPlay = true;
    [Header("Limits")]
    [Tooltip("Number of times to apply damage before stopping.")]
    public int maxTicks = 2;

    private Coroutine _damageRoutine;

    private void Awake()
    {
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.GetComponent<playerHealth>();
            }
        }

        if (player == null)
        {
            Debug.LogWarning("dmgTest: Could not find playerHealth. Assign the Player (with playerHealth) in the Inspector or tag your player as 'Player'.");
        }
    }

    private void OnEnable()
    {
        if (startOnPlay)
        {
            StartTest();
        }
    }

    private void OnDisable()
    {
        StopTest();
    }

    public void StartTest()
    {
        if (_damageRoutine == null)
        {
            _damageRoutine = StartCoroutine(DamageLoop());
        }
    }

    public void StopTest()
    {
        if (_damageRoutine != null)
        {
            StopCoroutine(_damageRoutine);
            _damageRoutine = null;
        }
    }

    private IEnumerator DamageLoop()
    {
        if (player == null)
        {
            yield break;
        }

        Debug.Log($"dmgTest: Waiting {initialDelay:F1}s before first damage...");
        yield return new WaitForSeconds(initialDelay);

        int ticks = 0;
        while (enabled && ticks < maxTicks)
        {
            if (player != null)
            {
                player.TakeDamage();
                ticks++;
            }
            else
            {
                Debug.LogWarning("dmgTest: player reference missing; stopping test.");
                _damageRoutine = null;
                yield break;
            }

            if (ticks >= maxTicks)
            {
                break;
            }

            yield return new WaitForSeconds(tickInterval);
        }
        Debug.Log($"dmgTest: Completed {ticks}/{maxTicks} damage ticks. Stopping.");
        _damageRoutine = null;
    }
}
